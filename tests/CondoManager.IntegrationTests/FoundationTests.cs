using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.IntegrationTests;
public class FoundationTests
{
    private sealed record Context(Guid? TenantId) : ICurrentTenant { public Guid? UserId => TenantId; public StudioRole? Role => TenantId.HasValue ? StudioRole.StudioAdmin : null; }
    private static AppDbContext Db(string name, Guid? tenant) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options, new Context(tenant));

    [Fact] public async Task HealthIsPublicButDataRoutesAreAbsent()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("{\"status\":\"healthy\"}", await response.Content.ReadAsStringAsync());
        foreach (var path in new[] { "/api/condominiums", "/api/people" })
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }
    [Fact] public async Task FiltersAreScopedToEachContextAndFailClosed()
    {
        var name = Guid.NewGuid().ToString(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        await using (var db = Db(name, a)) { db.Add(new Condominium(a, "A", "Synthetic")); await db.SaveChangesAsync(); }
        await using (var db = Db(name, b)) { db.Add(new Condominium(b, "B", "Synthetic")); await db.SaveChangesAsync(); }
        await using (var db = Db(name, a)) Assert.Equal("A", (await db.Condominiums.SingleAsync()).Code);
        await using (var db = Db(name, b)) Assert.Equal("B", (await db.Condominiums.SingleAsync()).Code);
        await using (var db = Db(name, null)) Assert.Empty(await db.Condominiums.ToListAsync());
    }
    [Fact] public async Task WritesRequireMatchingTenantAndTenantCannotChange()
    {
        var name = Guid.NewGuid().ToString(); var a = Guid.NewGuid();
        await using (var db = Db(name, null)) {
            db.Add(new Person(a, "P", "Synthetic")); Assert.Throws<AccessDeniedException>(() => db.SaveChanges());
        }
        await using (var db = Db(name, Guid.NewGuid())) {
            db.Add(new Person(a, "P", "Synthetic")); await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = Db(name, a)) {
            var row = new Person(a, "P", "Synthetic"); db.Add(row); await db.SaveChangesAsync();
            Assert.Throws<InvalidOperationException>(() => db.Entry(row).Property(x => x.TenantId).CurrentValue = Guid.NewGuid());
        }
    }
    [Fact] public void PostgresModelHasCompositeForeignKeysAndPrecision()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options, new Context(null));
        var model = db.Model;
        foreach (var type in model.GetEntityTypes().Where(t => typeof(TenantEntity).IsAssignableFrom(t.ClrType)))
        {
            Assert.NotNull(type.GetQueryFilter());
            foreach (var fk in type.GetForeignKeys().Where(f => f.PrincipalEntityType.ClrType != typeof(Tenant)))
                Assert.Contains(fk.Properties, p => p.Name == "TenantId");
        }
        Assert.Equal(6, model.FindEntityType(typeof(AllocationTableEntry))!.FindProperty("Value")!.GetScale());
        Assert.Equal(10, model.FindEntityType(typeof(OwnershipPeriod))!.FindProperty("Fraction")!.GetScale());
        Assert.Contains("CREATE TABLE", db.Database.GenerateCreateScript());
    }
}