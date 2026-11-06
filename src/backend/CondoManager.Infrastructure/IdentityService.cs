using Microsoft.Extensions.Hosting;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.Infrastructure;

// Identity and PostgreSQL adapter; domain/application own role and membership invariants.
public sealed class IdentityService(AppDbContext db, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
    ITenantContext tenant, IHttpContextAccessor http, ISecurityMessageSender messages, TimeProvider clock, IHostEnvironment environment)
{
    public const string PlatformRole = "PlatformAdmin";
    public const string SessionClaim = "cm_session";
    private ICurrentTenant Current => tenant as ICurrentTenant ?? throw new AccessDeniedException();
    private Guid UserId => Current.UserId ?? throw new AccessDeniedException();
    private DateTimeOffset Now => clock.GetUtcNow();
    public static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new ArgumentException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
    private static string Email(string? email)
    {
        var value = Guard.Text(email, 256);
        if (!MailAddress.TryCreate(value, out var address) || address.Address != value) throw new ArgumentException("Email non valida.");
        return value;
    }
    public static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128) throw new ArgumentException("Token non valido o scaduto.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
    private static string NewToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public async Task Audit(string name, Guid? tenantId = null, Guid? subject = null, Guid? actor = null, CancellationToken ct = default)
    {
        db.SecurityAudits.Add(new SecurityAudit { Event = name, TenantId = tenantId, SubjectId = subject, ActorUserId = actor ?? Current.UserId, Timestamp = Now });
        await db.SaveChangesAsync(ct);
    }
    public async Task<bool> Login(LoginInput input, CancellationToken ct)
    {
        var email = Email(input.Email);
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length > 128) throw new ArgumentException("Credenziali non valide.");
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            // Comparable password work even for an unknown account.
            DummyPassword.Verify(input.Password);
            await Audit("Login.Failed", ct: ct);
            return false;
        }
        if (!user.IsActive) { await Audit("Login.Failed", subject: user.Id, ct: ct); return false; }
        var result = await signIn.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await Audit(result.IsLockedOut ? "Login.LockedOut" : "Login.Failed", subject: user.Id, ct: ct);
            return false;
        }
        var memberships = await db.Memberships.Where(x => x.UserId == user.Id && x.IsActive).Select(x => x.TenantId).ToListAsync(ct);
        var session = new UserSession { UserId = user.Id, SecurityStamp = user.SecurityStamp!,
            ActiveTenantId = memberships.Count == 1 ? memberships[0] : null, CreatedAt = Now, ExpiresAt = Now.AddHours(8) };
        db.Sessions.Add(session);
        await Audit("Login.Succeeded", session.ActiveTenantId, user.Id, user.Id, ct);
        await signIn.SignInWithClaimsAsync(user, new AuthenticationProperties { IsPersistent = false, ExpiresUtc = session.ExpiresAt, AllowRefresh = false },
            new[] { new Claim(SessionClaim, session.Id.ToString()) });
        return true;
    }
    public async Task Logout(CancellationToken ct)
    {
        var session = await Session(ct);
        session.RevokedAt = Now;
        await Audit("Logout", session.ActiveTenantId, UserId, ct: ct);
        await signIn.SignOutAsync();
    }
    private async Task<UserSession> Session(CancellationToken ct)
    {
        if (!Guid.TryParse(http.HttpContext?.User.FindFirstValue(SessionClaim), out var id)) throw new AccessDeniedException();
        return await db.Sessions.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId && x.RevokedAt == null && x.ExpiresAt > Now, ct)
            ?? throw new AccessDeniedException();
    }
    public async Task<CurrentUserDto> Me(CancellationToken ct)
    {
        var user = await users.FindByIdAsync(UserId.ToString()) ?? throw new AccessDeniedException();
        var memberships = await (from member in db.Memberships.AsNoTracking()
            join studio in db.Tenants.IgnoreQueryFilters() on member.TenantId equals studio.Id
            where member.UserId == UserId && member.IsActive
            orderby studio.Name
            select new TenantAccessDto(studio.Id, studio.Name, member.Role)).ToListAsync(ct);
        var active = memberships.SingleOrDefault(x => x.TenantId == Current.TenantId);
        return new(user.Id, user.Email!, await users.IsInRoleAsync(user, PlatformRole), memberships, active?.TenantId,
            active?.Name, active?.Role, active?.Role is StudioRole.StudioAdmin or StudioRole.Collaborator, environment.IsDevelopment());
    }
    public async Task SelectTenant(Guid target, CancellationToken ct)
    {
        Guard.Id(target);
        if (!await db.Memberships.AnyAsync(x => x.TenantId == target && x.UserId == UserId && x.IsActive, ct)) throw new AccessDeniedException();
        var session = await Session(ct);
        session.ActiveTenantId = target;
        await Audit("Tenant.Selected", target, UserId, ct: ct);
    }
    public async Task ChangePassword(ChangePasswordInput input, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(UserId.ToString()) ?? throw new AccessDeniedException();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Ensure(await users.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword));
        await RevokeSessions(user.Id, ct);
        await Audit("Password.Changed", subject: user.Id, ct: ct);
        await transaction.CommitAsync(ct);
        await signIn.SignOutAsync();
    }
    public async Task ForgotPassword(string address, CancellationToken ct)
    {
        if (!messages.IsAvailable) throw new SecurityDeliveryUnavailableException();
        var email = Email(address);
        var user = await users.FindByEmailAsync(email);
        if (user == null || !user.IsActive) { await Audit("PasswordReset.Requested", ct: ct); return; }
        var token = NewToken();
        var grant = new SecurityGrant { Purpose = GrantPurpose.PasswordReset, UserId = user.Id,
            Email = user.Email!, NormalizedEmail = user.NormalizedEmail!, TokenHash = HashToken(token),
            SecurityStamp = user.SecurityStamp, CreatedAt = Now, ExpiresAt = Now.AddMinutes(30) };
        db.SecurityGrants.Add(grant);
        await Audit("PasswordReset.Requested", subject: user.Id, ct: ct);
        try { await messages.Send(new(user.Email!, "reset", token, Current.UserId, null), ct); }
        catch { grant.RevokedAt = Now; await db.SaveChangesAsync(ct); throw new SecurityDeliveryUnavailableException(); }
    }
    private async Task<SecurityGrant> LockedGrant(string token, GrantPurpose purpose, CancellationToken ct)
    {
        var hash = HashToken(token);
        var grant = await db.SecurityGrants.FromSqlInterpolated(
            $"SELECT * FROM \"SecurityGrants\" WHERE \"TokenHash\"={hash} AND \"Purpose\"={(int)purpose} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (grant == null || grant.UsedAt != null || grant.RevokedAt != null || grant.ExpiresAt <= Now)
            throw new ArgumentException("Token non valido o scaduto.");
        return grant;
    }
    public async Task ResetPassword(ResetPasswordInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var grant = await LockedGrant(input.Token, GrantPurpose.PasswordReset, ct);
        var user = await users.FindByIdAsync(grant.UserId.ToString()!) ?? throw new ArgumentException("Token non valido o scaduto.");
        if (!user.IsActive || user.SecurityStamp != grant.SecurityStamp) throw new ArgumentException("Token non valido o scaduto.");
        var identityToken = await users.GeneratePasswordResetTokenAsync(user);
        Ensure(await users.ResetPasswordAsync(user, identityToken, input.NewPassword));
        grant.UsedAt = Now;
        await RevokeSessions(user.Id, ct);
        await Audit("Password.Reset", subject: user.Id, ct: ct);
        await transaction.CommitAsync(ct);
    }
    private async Task RevokeSessions(Guid userId, CancellationToken ct)
    {
        foreach (var session in await db.Sessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct)) session.RevokedAt = Now;
    }
    private async Task LockTenant(Guid tenantId, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tenants\" WHERE \"Id\"={tenantId} FOR UPDATE", ct);
    private async Task<Guid> Admin(CancellationToken ct)
    {
        Current.RequireAdmin();
        var id = Current.RequireRead();
        if (!await db.Memberships.AnyAsync(x => x.TenantId == id && x.UserId == UserId && x.IsActive && x.Role == StudioRole.StudioAdmin, ct))
            throw new AccessDeniedException();
        return id;
    }
    public async Task<IReadOnlyList<MembershipDto>> Members(CancellationToken ct)
    {
        var id = await Admin(ct);
        return await (from member in db.Memberships join user in db.Users on member.UserId equals user.Id
            where member.TenantId == id orderby user.Email
            select new MembershipDto(member.Id, user.Id, user.Email!, member.Role, member.IsActive, user.IsActive)).ToListAsync(ct);
    }
    private async Task<int> OtherAdmins(Guid tenantId, Guid userId, CancellationToken ct) =>
        await (from member in db.Memberships join user in db.Users on member.UserId equals user.Id
            where member.TenantId == tenantId && member.UserId != userId && member.IsActive && member.Role == StudioRole.StudioAdmin && user.IsActive
            select member.Id).CountAsync(ct);
    public async Task ChangeMembership(Guid id, MembershipInput input, CancellationToken ct)
    {
        var tenantId = await Admin(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockTenant(tenantId, ct); await Admin(ct);
        var member = await db.Memberships.SingleOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct) ?? throw new NotFoundException();
        var userActive = await db.Users.AnyAsync(x => x.Id == member.UserId && x.IsActive, ct);
        MembershipRules.EnsureAdminRemains(member.IsActive && member.Role == StudioRole.StudioAdmin && userActive &&
            (!input.IsActive || input.Role != StudioRole.StudioAdmin), await OtherAdmins(tenantId, member.UserId, ct));
        var oldRole = member.Role;
        var oldActive = member.IsActive;
        member.SetAccess(input.Role, input.IsActive);
        if (oldRole != input.Role) await Audit($"Membership.Role.{oldRole}.To.{input.Role}", tenantId, member.UserId, ct: ct);
        if (oldActive != input.IsActive) await Audit(input.IsActive ? "Membership.Enabled" : "Membership.Disabled", tenantId, member.UserId, ct: ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<InvitationDto>> Invitations(CancellationToken ct)
    {
        var id = await Admin(ct);
        return await db.SecurityGrants.Where(x => x.TenantId == id && x.Purpose == GrantPurpose.Invitation)
            .OrderByDescending(x => x.CreatedAt).Select(x => new InvitationDto(x.Id, x.Email, x.Role!.Value, x.ExpiresAt, x.UsedAt, x.RevokedAt)).ToListAsync(ct);
    }
    public async Task<InvitationDto> Invite(InviteInput input, CancellationToken ct)
    {
        if (!messages.IsAvailable) throw new SecurityDeliveryUnavailableException();
        var tenantId = await Admin(ct); var email = Email(input.Email); Guard.EnumValue(input.Role);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockTenant(tenantId, ct); await Admin(ct);
        var normalized = users.NormalizeEmail(email);
        foreach (var old in await db.SecurityGrants.Where(x => x.TenantId == tenantId && x.Purpose == GrantPurpose.Invitation && x.NormalizedEmail == normalized && x.UsedAt == null && x.RevokedAt == null).ToListAsync(ct))
        { old.RevokedAt = Now; await Audit("Invitation.Revoked", tenantId, old.Id, ct: ct); }
        var token = NewToken();
        var grant = new SecurityGrant { Purpose = GrantPurpose.Invitation, TenantId = tenantId, IssuedByUserId = UserId,
            Email = email, NormalizedEmail = normalized, Role = input.Role, TokenHash = HashToken(token), CreatedAt = Now, ExpiresAt = Now.AddHours(48) };
        db.SecurityGrants.Add(grant);
        await Audit("Invitation.Created", tenantId, grant.Id, ct: ct);
        try { await messages.Send(new(email, "invite", token, UserId, tenantId), ct); }
        catch { throw new SecurityDeliveryUnavailableException(); }
        await transaction.CommitAsync(ct);
        return new(grant.Id, email, input.Role, grant.ExpiresAt, null, null);
    }
    public async Task RevokeInvitation(Guid id, CancellationToken ct)
    {
        var tenantId = await Admin(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockTenant(tenantId, ct); await Admin(ct);
        var row = await db.SecurityGrants.SingleOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.Purpose == GrantPurpose.Invitation, ct) ?? throw new NotFoundException();
        if (row.UsedAt != null) throw new InvalidOperationException("Invito gia utilizzato.");
        row.RevokedAt = Now;
        await Audit("Invitation.Revoked", tenantId, row.Id, ct: ct);
        await transaction.CommitAsync(ct);
    }
    public async Task AcceptInvitation(AcceptInvitationInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // First discover the tenant by an unguessable grant, then lock in the same order as revocations.
        var hash = HashToken(input.Token);
        var target = await db.SecurityGrants.AsNoTracking().Where(x => x.TokenHash == hash && x.Purpose == GrantPurpose.Invitation).Select(x => x.TenantId).SingleOrDefaultAsync(ct);
        if (target is null) throw new ArgumentException("Token non valido o scaduto.");
        await LockTenant(target.Value, ct);
        var grant = await LockedGrant(input.Token, GrantPurpose.Invitation, ct);
        var issuerValid = await (from member in db.Memberships join user in db.Users on member.UserId equals user.Id
            where member.TenantId == target && member.UserId == grant.IssuedByUserId && member.IsActive && member.Role == StudioRole.StudioAdmin && user.IsActive
            select member.Id).AnyAsync(ct);
        if (!issuerValid) throw new ArgumentException("Invito non piu valido.");
        var account = await users.FindByEmailAsync(grant.Email);
        if (account != null)
        {
            // Existing accounts must prove identity; an invitation is never a password-reset mechanism.
            if (Current.UserId != account.Id || !account.IsActive) throw new AccessDeniedException();
        }
        else
        {
            if (Current.UserId != null) throw new AccessDeniedException();
            account = new ApplicationUser { UserName = grant.Email, Email = grant.Email, EmailConfirmed = true };
            Ensure(await users.CreateAsync(account, input.Password ?? ""));
        }
        var memberRow = await db.Memberships.SingleOrDefaultAsync(x => x.TenantId == target && x.UserId == account.Id, ct);
        if (memberRow == null) db.Memberships.Add(new(target.Value, account.Id, grant.Role!.Value));
        else
        {
            if (memberRow.IsActive) throw new InvalidOperationException("L'utente appartiene gia allo studio.");
            memberRow.SetAccess(grant.Role!.Value, true);
        }
        grant.UsedAt = Now;
        await Audit("Invitation.Accepted", target, account.Id, account.Id, ct);
        await transaction.CommitAsync(ct);
    }
    private async Task Platform()
    {
        var user = await users.FindByIdAsync(UserId.ToString()) ?? throw new AccessDeniedException();
        if (!await users.IsInRoleAsync(user, PlatformRole)) throw new AccessDeniedException();
    }
    public async Task<IReadOnlyList<PlatformUserDto>> PlatformUsers(CancellationToken ct)
    {
        await Platform();
        return await db.Users.OrderBy(x => x.Email).Select(x => new PlatformUserDto(x.Id, x.Email!, x.IsActive)).ToListAsync(ct);
    }
    public async Task SetUserStatus(Guid id, bool active, CancellationToken ct)
    {
        await Platform();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A global lock orders account disabling against all other account disables.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(31003)", ct);
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new NotFoundException();
        if (!active && user.IsActive)
        {
            var tenantIds = await db.Memberships.Where(x => x.UserId == id && x.IsActive && x.Role == StudioRole.StudioAdmin).OrderBy(x => x.TenantId).Select(x => x.TenantId).ToListAsync(ct);
            foreach (var tenantId in tenantIds)
            {
                await LockTenant(tenantId, ct);
                MembershipRules.EnsureAdminRemains(true, await OtherAdmins(tenantId, id, ct));
            }
            if (await users.IsInRoleAsync(user, PlatformRole) &&
                !await (from link in db.UserRoles join role in db.Roles on link.RoleId equals role.Id join other in db.Users on link.UserId equals other.Id
                    where role.Name == PlatformRole && other.IsActive && other.Id != id select other.Id).AnyAsync(ct))
                throw new InvalidOperationException("Non e possibile disabilitare l'ultimo PlatformAdmin attivo.");
        }
        user.IsActive = active;
        Ensure(await users.UpdateAsync(user)); Ensure(await users.UpdateSecurityStampAsync(user));
        await RevokeSessions(id, ct);
        await Audit(active ? "User.Enabled" : "User.Disabled", subject: id, ct: ct);
        await transaction.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<AuditDto>> StudioAudit(CancellationToken ct)
    {
        var id = await Admin(ct);
        return await db.SecurityAudits.Where(x => x.TenantId == id).OrderByDescending(x => x.Timestamp).Take(200)
            .Select(x => new AuditDto(x.Id, x.Timestamp, x.Event, x.ActorUserId, x.SubjectId)).ToListAsync(ct);
    }
}
internal static class DummyPassword
{
    private static readonly PasswordHasher<ApplicationUser> Hasher = new();
    private static readonly ApplicationUser User = new();
    private static readonly string Hash = Hasher.HashPassword(User, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    public static void Verify(string password) => Hasher.VerifyHashedPassword(User, Hash, password);
}
public sealed class BoundedPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password) =>
        Task.FromResult(password is { Length: <= 128 } ? IdentityResult.Success :
            IdentityResult.Failed(new IdentityError { Code = "PasswordTooLong", Description = "Password: massimo 128 caratteri." }));
}