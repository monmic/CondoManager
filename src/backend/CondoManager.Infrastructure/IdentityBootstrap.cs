using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CondoManager.Infrastructure;

public sealed class IdentityBootstrap(AppDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles, IConfiguration config, TimeProvider clock)
{
    public async Task Run(CancellationToken ct = default)
    {
        string Required(string key) => !string.IsNullOrWhiteSpace(config["Bootstrap:" + key]) ? config["Bootstrap:" + key]! :
            throw new InvalidOperationException("Variabile Bootstrap__" + key + " obbligatoria.");
        var email = Guard.Text(Required("Email"), 256);
        var password = Required("Password");
        var tenantId = Guard.Id(Guid.Parse(Required("TenantId")));
        var code = Guard.Text(Required("TenantCode")); var name = Guard.Text(Required("TenantName"));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(31003)", ct);
        var existing = await db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == tenantId, ct);
        if (existing != null && existing.Code != code) throw new InvalidOperationException("Il TenantId esiste con un codice differente.");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tenants" ("Id","Code","Name","CreatedAt","UpdatedAt")
            VALUES ({tenantId},{code},{name},{clock.GetUtcNow()},{clock.GetUtcNow()})
            ON CONFLICT ("Id") DO NOTHING
            """, ct);
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            IdentityService.Ensure(await users.CreateAsync(user, password));
        }
        if (!user.IsActive) throw new InvalidOperationException("Account esistente disabilitato: il bootstrap non lo riattiva.");
        if (!await roles.RoleExistsAsync(IdentityService.PlatformRole))
            IdentityService.Ensure(await roles.CreateAsync(new IdentityRole<Guid>(IdentityService.PlatformRole)));
        if (!await users.IsInRoleAsync(user, IdentityService.PlatformRole))
            IdentityService.Ensure(await users.AddToRoleAsync(user, IdentityService.PlatformRole));
        var membership = await db.Memberships.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == user.Id, ct);
        if (membership == null) db.Memberships.Add(new(tenantId, user.Id, StudioRole.StudioAdmin));
        else if (!membership.IsActive || membership.Role != StudioRole.StudioAdmin)
            throw new InvalidOperationException("Membership esistente differente: non viene sovrascritta dal bootstrap.");
        db.SecurityAudits.Add(new() { Event = "Bootstrap.PlatformAndStudioAdmin", TenantId = tenantId, ActorUserId = user.Id, SubjectId = user.Id, Timestamp = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        // No password, token, cookie or session identifier is written to stdout/logs.
    }
}