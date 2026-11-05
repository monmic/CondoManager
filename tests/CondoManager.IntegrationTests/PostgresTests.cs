using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.IntegrationTests;
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")))
            Skip = "Eseguire scripts/Verify-Postgres.ps1 per verificare su PostgreSQL isolato.";
    }
}
public class PostgresTests
{
    private sealed record Context(Guid? TenantId) : ITenantContext;
    [PostgresFact]
    public async Task MigrationPersistenceAndCrossTenantForeignKeys()
    {
        var connection = Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")!;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        await using var db = new AppDbContext(options, new Context(a));
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tenants" ("Id","Code","Name","CreatedAt","UpdatedAt")
            VALUES ({a},{"TEST-A"},{"Synthetic A"},now(),now()), ({b},{"TEST-B"},{"Synthetic B"},now(),now())
            """);
        var condominium = new Condominium(a, "C", "Synthetic");
        var person = new Person(a, "P", "Synthetic");
        db.AddRange(condominium, person);
        await db.SaveChangesAsync();
        var unit = new PropertyUnit(a, condominium.Id, "U");
        db.Add(unit);
        await db.SaveChangesAsync();
        db.Add(new OwnershipPeriod(a, unit.Id, person.Id, new DateOnly(2026, 1, 1), null, 0.3333333333m));
        var table = new MillesimalTable(a, condominium.Id, "T");
        db.Add(table);
        await db.SaveChangesAsync();
        db.Add(new MillesimalShare(a, condominium.Id, table.Id, unit.Id, 42.123456m, new DateOnly(2026, 1, 1), null));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(0.3333333333m, (await db.OwnershipPeriods.SingleAsync()).Fraction);
        Assert.Equal(42.123456m, (await db.MillesimalShares.SingleAsync()).Value);
        Assert.Single(await db.Tenants.ToListAsync());
        var foreignPersonId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "People" ("Id","TenantId","Code","DisplayName","CreatedAt","UpdatedAt")
            VALUES ({foreignPersonId},{b},{"FOREIGN"},{"Synthetic"},now(),now())
            """);
        var forged = new Person(a, "FORGED", "Synthetic");
        db.Entry(forged).Property(x => x.Id).CurrentValue = foreignPersonId;
        db.Attach(forged);
        db.Remove(forged);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        // Even bypassing EF filters cannot create a cross-tenant relationship.
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Buildings" ("Id","TenantId","CondominiumId","Code","CreatedAt","UpdatedAt")
            VALUES ({Guid.NewGuid()},{b},{condominium.Id},{"INVALID"},now(),now())
            """));
        await transaction.RollbackAsync();
    }
}