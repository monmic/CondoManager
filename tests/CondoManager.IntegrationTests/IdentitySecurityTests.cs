using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CondoManager.IntegrationTests;

[Collection("PostgreSQL")]
public class IdentitySecurityTests
{
    private static async Task<Guid> Tenant(SecurityTestHost host)
    {
        var id = Guid.NewGuid();
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tenants" ("Id","Code","Name","CreatedAt","UpdatedAt")
            VALUES ({id},{id.ToString()},{"Synthetic security studio"},now(),now())
            """);
        return id;
    }
    private static async Task<HttpClient> Login(SecurityTestHost host, TestAccount user, Guid? tenant = null)
    {
        var client = host.Browser(); await SecurityTestHost.Login(client, user, tenant); return client;
    }
    private static async Task<T> Body<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(SecurityTestHost.Json))!;
    }
    private static async Task<CurrentUserDto> Me(HttpClient client) => await Body<CurrentUserDto>(await client.GetAsync("/api/auth/me"));
    private static string MailToken(SecurityTestHost host, TestAccount recipient, string kind, Guid? tenant, TestAccount? issuer = null)
    {
        var mailbox = host.Services.GetRequiredService<DevelopmentMailbox>();
        var message = mailbox.ForUser(issuer?.Id ?? recipient.Id, issuer?.Email ?? recipient.Email, tenant, issuer != null).Last(x => x.Kind == kind);
        return Uri.UnescapeDataString(new Uri(message.Link).Fragment.Split('=')[1]);
    }
    private static HttpContent Payload(object body) => JsonContent.Create(body, options: SecurityTestHost.Json);
    private static async Task<CondominiumDto> Condo(HttpClient client, string code = "C") =>
        await Body<CondominiumDto>(await client.PostAsJsonAsync("/api/condominiums",
            new CondominiumInput(code, "Synthetic", null, "Synthetic address", "00000", "Synthetic city", "XX", "Test")));
    [PostgresFact]
    public async Task LoginPasswordPolicyLockoutAndAnonymousAccess()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var user = await host.Account(tenant);
        using var client = host.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/condominiums")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await SecurityTestHost.Csrf(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginInput(user.Email, "Invalid-password-1!"))).StatusCode);
        await SecurityTestHost.Login(client, user);
        Assert.Equal(user.Id, (await Me(client)).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordInput(user.Password, "weak"))).StatusCode);
        using var attempts = host.Browser(); await SecurityTestHost.Csrf(attempts);
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await attempts.PostAsJsonAsync("/api/auth/login", new LoginInput(user.Email, "Invalid-password-1!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attempts.PostAsJsonAsync("/api/auth/login", new LoginInput(user.Email, user.Password))).StatusCode);
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True((await db.Users.SingleAsync(x => x.Id == user.Id)).LockoutEnd > DateTimeOffset.UtcNow);
        Assert.Contains(await db.SecurityAudits.Select(x => x.Event).ToListAsync(), x => x == "Login.Succeeded");
        Assert.Contains(await db.SecurityAudits.Select(x => x.Event).ToListAsync(), x => x == "Login.LockedOut");
    }
    [PostgresFact]
    public async Task MultiStudioRolesAndCrossTenantIdsAreEnforced()
    {
        await using var host = new SecurityTestHost();
        var a = await Tenant(host); var b = await Tenant(host); var outsider = await Tenant(host);
        var user = await host.Account(a, StudioRole.Viewer);
        await using (var scope = host.Services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Memberships.Add(new(b, user.Id, StudioRole.Collaborator)); await db.SaveChangesAsync();
        }
        using var client = await Login(host, user);
        Assert.Null((await Me(client)).ActiveTenantId);
        Assert.Equal(2, (await Me(client)).Memberships.Count);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/condominiums")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/auth/tenant", new SelectTenantInput(outsider))).StatusCode);
        (await client.PostAsJsonAsync("/api/auth/tenant", new SelectTenantInput(a))).EnsureSuccessStatusCode();
        Assert.Equal(StudioRole.Viewer, (await Me(client)).Role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/condominiums", new CondominiumInput("DENIED", "Synthetic", null, "X", "0", "X", "X", "X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/studio/members")).StatusCode);
        var admin = await host.Account(a); using var adminClient = await Login(host, admin);
        var condoA = await Condo(adminClient);
        (await client.PostAsJsonAsync("/api/auth/tenant", new SelectTenantInput(b))).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("X-Expected-Tenant", a.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync("/api/condominiums")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Expected-Tenant");
        var condoB = await Condo(client);
        client.DefaultRequestHeaders.Add("TenantId", a.ToString());
        client.DefaultRequestHeaders.Add("X-Tenant-Id", a.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/condominiums/" + condoA.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/condominiums/" + condoA.Id, new CondominiumInput("C", "X", null, "X", "0", "X", "X", "X"))).StatusCode);
        var visible = await Body<CondominiumDto[]>(await client.GetAsync("/api/condominiums"));
        Assert.Equal(condoB.Id, Assert.Single(visible).Id);
        var personA = await Body<PersonDto>(await adminClient.PostAsync("/api/people", Payload(new PersonInput("P", PersonKind.LegalEntity, null, "Synthetic", "SYNTH", null, null, null, null, null))));
        var unitB = await Body<PropertyUnitDto>(await client.PostAsync($"/api/condominiums/{condoB.Id}/units", Payload(new PropertyUnitInput("U", null, null, null, null, PropertyUnitKind.Other, null, null))));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/condominiums/{condoB.Id}/units/{unitB.Id}/associations",
            Payload(new AssociationInput(personA.Id, UnitPersonRole.Owner, new(2026, 1, 1), null, 1)))).StatusCode);
    }
    [PostgresFact]
    public async Task MembershipChangesAreImmediateAndLastAdminIsProtected()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host);
        var admin = await host.Account(tenant); var viewer = await host.Account(tenant, StudioRole.Viewer);
        using var adminClient = await Login(host, admin); using var viewerClient = await Login(host, viewer);
        var members = await Body<MembershipDto[]>(await adminClient.GetAsync("/api/studio/members"));
        Assert.Equal(HttpStatusCode.BadRequest, (await adminClient.PostAsJsonAsync("/api/studio/invitations", new { email = "missing-role@test.invalid" })).StatusCode);
        var own = members.Single(x => x.UserId == admin.Id); var other = members.Single(x => x.UserId == viewer.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await adminClient.PutAsync("/api/studio/members/" + own.Id, Payload(new MembershipInput(StudioRole.Viewer, false)))).StatusCode);
        (await adminClient.PutAsync("/api/studio/members/" + other.Id, Payload(new MembershipInput(StudioRole.Viewer, false)))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await viewerClient.GetAsync("/api/condominiums")).StatusCode);
        Assert.Empty((await Me(viewerClient)).Memberships);
        (await adminClient.PutAsync("/api/studio/members/" + other.Id, Payload(new MembershipInput(StudioRole.Collaborator, true)))).EnsureSuccessStatusCode();
        Assert.Equal(StudioRole.Collaborator, (await Me(viewerClient)).Role);
        Assert.True((await Me(viewerClient)).CanWrite);
        await Condo(viewerClient);
        var otherTenant = await Tenant(host); var otherAdmin = await host.Account(otherTenant);
        using var otherClient = await Login(host, otherAdmin);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PutAsync("/api/studio/members/" + own.Id, Payload(new MembershipInput(StudioRole.Viewer, false)))).StatusCode);
    }
    [PostgresFact]
    public async Task CsrfAndLogoutPreventUnsafeRequestsAndCookieReplay()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var user = await host.Account(tenant);
        using var client = host.Browser();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new LoginInput(user.Email, user.Password))).StatusCode);
        await SecurityTestHost.Csrf(client);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new LoginInput(user.Email, user.Password));
        login.EnsureSuccessStatusCode();
        var cookieHeader = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-CondoManager=", StringComparison.Ordinal));
        Assert.Contains("secure", cookieHeader.ToLowerInvariant()); Assert.Contains("httponly", cookieHeader.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookieHeader.ToLowerInvariant());
        var cookie = cookieHeader.Split(';')[0];
        await SecurityTestHost.Csrf(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        await SecurityTestHost.Csrf(client);
        (await client.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var replay = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/auth/me")).StatusCode);
    }
    [PostgresFact]
    public async Task InvitationsExpireAreSingleUseAndDoNotDuplicateExistingUsers()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var admin = await host.Account(tenant);
        using var client = await Login(host, admin);
        var email = "invited-" + Guid.NewGuid().ToString("N") + "@test.invalid";
        var invitation = await Body<InvitationDto>(await client.PostAsync("/api/studio/invitations", Payload(new InviteInput(email, StudioRole.Viewer))));
        var token = MailToken(host, admin, "invite", tenant, admin);
        using var recipient = host.Browser(); await SecurityTestHost.Csrf(recipient);
        var accepted = new AcceptInvitationInput(token, SecurityTestHost.Password());
        (await recipient.PostAsJsonAsync("/api/auth/accept-invitation", accepted)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/auth/accept-invitation", accepted)).StatusCode);
        var expiredEmail = "expired-" + Guid.NewGuid().ToString("N") + "@test.invalid";
        await Body<InvitationDto>(await client.PostAsync("/api/studio/invitations", Payload(new InviteInput(expiredEmail, StudioRole.Collaborator))));
        var expired = MailToken(host, admin, "invite", tenant, admin);
        host.Clock.Advance(TimeSpan.FromHours(49));
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationInput(expired, SecurityTestHost.Password()))).StatusCode);
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull((await db.SecurityGrants.SingleAsync(x => x.Id == invitation.Id)).UsedAt);
        Assert.DoesNotContain(await db.SecurityGrants.Select(x => x.TokenHash).ToListAsync(), x => x == token);
    }
    [PostgresFact]
    public async Task ExistingAccountMustAuthenticateBeforeAcceptingAnotherStudioInvitation()
    {
        await using var host = new SecurityTestHost();
        var a = await Tenant(host); var b = await Tenant(host);
        var invited = await host.Account(a, StudioRole.Viewer); var admin = await host.Account(b);
        using var adminClient = await Login(host, admin);
        await Body<InvitationDto>(await adminClient.PostAsync("/api/studio/invitations", Payload(new InviteInput(invited.Email, StudioRole.Collaborator))));
        var token = MailToken(host, invited, "invite", b, admin);
        using var anon = host.Browser(); await SecurityTestHost.Csrf(anon);
        Assert.Equal(HttpStatusCode.Forbidden, (await anon.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationInput(token, SecurityTestHost.Password()))).StatusCode);
        using var existing = await Login(host, invited);
        (await existing.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationInput(token, null))).EnsureSuccessStatusCode();
        Assert.Equal(2, (await Me(existing)).Memberships.Count);
        (await existing.PostAsJsonAsync("/api/auth/tenant", new SelectTenantInput(b))).EnsureSuccessStatusCode();
        Assert.Equal(StudioRole.Collaborator, (await Me(existing)).Role);
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await db.Users.Where(x => x.Email == invited.Email).ToListAsync());
    }
    [PostgresFact]
    public async Task RevokedInvitationsAndInvitationsOfRevokedAdministratorsAreDenied()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var admin = await host.Account(tenant); var second = await host.Account(tenant);
        using var client = await Login(host, admin); using var secondClient = await Login(host, second);
        var email = "revoked-" + Guid.NewGuid().ToString("N") + "@test.invalid";
        var row = await Body<InvitationDto>(await client.PostAsync("/api/studio/invitations", Payload(new InviteInput(email, StudioRole.Viewer))));
        var token = MailToken(host, admin, "invite", tenant, admin);
        (await client.PostAsync("/api/studio/invitations/" + row.Id + "/revoke", null)).EnsureSuccessStatusCode();
        using var recipient = host.Browser(); await SecurityTestHost.Csrf(recipient);
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationInput(token, SecurityTestHost.Password()))).StatusCode);
        await Body<InvitationDto>(await client.PostAsync("/api/studio/invitations", Payload(new InviteInput(email, StudioRole.Viewer))));
        var pending = MailToken(host, admin, "invite", tenant, admin);
        var members = await Body<MembershipDto[]>(await secondClient.GetAsync("/api/studio/members"));
        (await secondClient.PutAsync("/api/studio/members/" + members.Single(x => x.UserId == admin.Id).Id, Payload(new MembershipInput(StudioRole.Collaborator, true)))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationInput(pending, SecurityTestHost.Password()))).StatusCode);
    }
    [PostgresFact]
    public async Task ResetAndChangePasswordInvalidateSessionsAndResetTokens()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var user = await host.Account(tenant);
        using var original = await Login(host, user);
        using var anonymous = host.Browser(); await SecurityTestHost.Csrf(anonymous);
        var request = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordInput(user.Email));
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordInput("unknown@test.invalid"))).StatusCode);
        var token = MailToken(host, user, "reset", null);
        var newPassword = SecurityTestHost.Password();
        (await anonymous.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordInput(token, newPassword))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordInput(token, newPassword))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await original.GetAsync("/api/auth/me")).StatusCode);
        using var fresh = await Login(host, user with { Password = newPassword });
        (await fresh.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordInput(newPassword, SecurityTestHost.Password()))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.GetAsync("/api/auth/me")).StatusCode);
    }
    [PostgresFact]
    public async Task ResetTokensExpireAndSessionsHaveAbsoluteExpiration()
    {
        await using var host = new SecurityTestHost();
        var user = await host.Account(null);
        using var client = await Login(host, user);
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordInput(user.Email))).EnsureSuccessStatusCode();
        var token = MailToken(host, user, "reset", null);
        host.Clock.Advance(TimeSpan.FromMinutes(31));
        using var anonymous = host.Browser(); await SecurityTestHost.Csrf(anonymous);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordInput(token, SecurityTestHost.Password()))).StatusCode);
        host.Clock.Advance(TimeSpan.FromHours(8));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [PostgresFact]
    public async Task PlatformRoleDoesNotGrantTenantDataAndDisabledAccountsLoseAllSessions()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var owner = await host.Account(tenant);
        var platform = await host.Account(null, platform: true); var viewer = await host.Account(tenant, StudioRole.Viewer);
        using var root = await Login(host, platform); using var client = await Login(host, viewer);
        Assert.True((await Me(root)).IsPlatformAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, (await root.GetAsync("/api/condominiums")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await root.GetAsync("/api/studio/members")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await root.GetAsync("/api/platform/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/platform/users")).StatusCode);
        (await root.PutAsJsonAsync("/api/platform/users/" + viewer.Id + "/status", new UserStatusInput(false))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await root.PutAsJsonAsync("/api/platform/users/" + owner.Id + "/status", new UserStatusInput(false))).StatusCode);
    }
    [PostgresFact]
    public async Task ProductionDoesNotExposeDevelopmentMessagesAndRequiresConfiguredDelivery()
    {
        await using var host = new SecurityTestHost(environment: "Production");
        var user = await host.Account(null); using var client = await Login(host, user);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/development/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordInput(user.Email))).StatusCode);
    }
    [PostgresFact]
    public async Task AuthenticationRateLimitingReturns429()
    {
        await using var host = new SecurityTestHost(); using var client = host.Browser();
        await SecurityTestHost.Csrf(client);
        for (var i = 0; i < 20; i++) await client.PostAsJsonAsync("/api/auth/login", new LoginInput("unknown@test.invalid", "Invalid-password-1!"));
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new LoginInput("unknown@test.invalid", "Invalid-password-1!"))).StatusCode);
    }
    [PostgresFact]
    public async Task ConcurrentDemotionsCannotRemoveEveryStudioAdmin()
    {
        await using var host = new SecurityTestHost();
        var tenant = await Tenant(host); var a = await host.Account(tenant); var b = await host.Account(tenant);
        using var ca = await Login(host, a); using var cb = await Login(host, b);
        var rows = await Body<MembershipDto[]>(await ca.GetAsync("/api/studio/members"));
        var responses = await Task.WhenAll(
            ca.PutAsync("/api/studio/members/" + rows.Single(x => x.UserId == a.Id).Id, Payload(new MembershipInput(StudioRole.Viewer, true))),
            cb.PutAsync("/api/studio/members/" + rows.Single(x => x.UserId == b.Id).Id, Payload(new MembershipInput(StudioRole.Viewer, true))));
        Assert.Single(responses.Where(x => x.IsSuccessStatusCode));
        Assert.Single(responses.Where(x => x.StatusCode == HttpStatusCode.Conflict));
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Memberships.CountAsync(x => x.TenantId == tenant && x.IsActive && x.Role == StudioRole.StudioAdmin));
    }
}