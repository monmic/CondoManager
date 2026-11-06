using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CondoManager.IntegrationTests;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public class PostgresCollection { }

[Collection("PostgreSQL")]
public class RegistryApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };
    private sealed record Context(Guid? TenantId) : ICurrentTenant { public Guid? UserId => TenantId; public StudioRole? Role => TenantId.HasValue ? StudioRole.StudioAdmin : null; }
    private static string Connection => Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")!;
    private static AppDbContext Db(Guid? tenant, string? connection = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection ?? Connection).Options, new Context(tenant));
    private static SecurityTestHost Factory(Guid tenant, string environment = "Development") => new(tenant, environment);
    private static async Task<Guid> SeedTenant()
    {
        var id = Guid.NewGuid();
        await using var db = Db(null);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tenants" ("Id","Code","Name","CreatedAt","UpdatedAt")
            VALUES ({id},{id.ToString()},{"Synthetic studio"},now(),now())
            """);
        return id;
    }
    private static CondominiumInput Condo(string code = "C") => new(code, "Synthetic condominium", null, "Synthetic address", "00000", "Synthetic city", "XX", "Test country");
    private static PropertyUnitInput Unit(string code, Guid? building = null) => new(code, building, null, "1", "0", PropertyUnitKind.Apartment, null, null);
    private static PersonInput Person(string code = "P") => new(code, PersonKind.NaturalPerson, "Synthetic", "Subject", "SYNTH-" + Guid.NewGuid().ToString("N")[..12], null, null, null, null, null);
    private static async Task<T> Send<T>(HttpClient client, string path, HttpMethod method, object? input = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (input != null) request.Content = JsonContent.Create(input, options: Json);
        using var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }
    private static Task<T> Post<T>(HttpClient client, string path, object? input = null) => Send<T>(client, path, HttpMethod.Post, input);
    private static Task<T> Put<T>(HttpClient client, string path, object input) => Send<T>(client, path, HttpMethod.Put, input);
    private static async Task<T> Get<T>(HttpClient client, string path) => (await client.GetFromJsonAsync<T>(path, Json))!;

    [PostgresFact]
    public async Task CondominiumCrudValidationAndScopedUniqueCodes()
    {
        var tenant = await SeedTenant();
        await using var factory = Factory(tenant);
        using var client = await factory.CreateAuthenticatedClient();
        var row = await Post<CondominiumDto>(client, "/api/condominiums", Condo());
        Assert.True(row.IsActive); Assert.NotEqual(Guid.Empty, row.Id);
        Assert.Single(await Get<CondominiumDto[]>(client, "/api/condominiums"));
        Assert.Equal(row.Id, (await Get<CondominiumDto>(client, "/api/condominiums/" + row.Id)).Id);
        var duplicate = await client.PostAsJsonAsync("/api/condominiums", Condo(), Json);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/condominiums", Condo(" ") , Json)).StatusCode);
        var updated = await Put<CondominiumDto>(client, "/api/condominiums/" + row.Id, Condo() with { Name = "Synthetic changed" });
        Assert.Equal("Synthetic changed", updated.Name);
        Assert.True(updated.UpdatedAt >= row.UpdatedAt);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/condominiums/" + row.Id)).StatusCode);
        Assert.False((await Get<CondominiumDto>(client, "/api/condominiums/" + row.Id)).IsActive);
        Assert.Single(await Get<CondominiumDto[]>(client, "/api/condominiums")); // Logical deletion retains data.
    }
    [PostgresFact]
    public async Task TenantHeadersAndForeignRelationshipsCannotEscapeServerContext()
    {
        var a = await SeedTenant(); var b = await SeedTenant();
        await using var factoryA = Factory(a); await using var factoryB = Factory(b);
        using var ca = await factoryA.CreateAuthenticatedClient(); using var cb = await factoryB.CreateAuthenticatedClient();
        var condoA = await Post<CondominiumDto>(ca, "/api/condominiums", Condo());
        var condoB = await Post<CondominiumDto>(cb, "/api/condominiums", Condo()); // Same code, different tenant.
        ca.DefaultRequestHeaders.Add("TenantId", b.ToString());
        ca.DefaultRequestHeaders.Add("X-Tenant-Id", b.ToString());
        Assert.Equal(condoA.Id, Assert.Single(await Get<CondominiumDto[]>(ca, "/api/condominiums")).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.GetAsync("/api/condominiums/" + condoB.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.PutAsJsonAsync("/api/condominiums/" + condoB.Id, Condo(), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ca.DeleteAsync("/api/condominiums/" + condoB.Id)).StatusCode);
        var unitA = await Post<PropertyUnitDto>(ca, $"/api/condominiums/{condoA.Id}/units", Unit("U"));
        var personB = await Post<PersonDto>(cb, "/api/people", Person());
        Assert.Equal(HttpStatusCode.NotFound, (await ca.PostAsJsonAsync($"/api/condominiums/{condoA.Id}/units/{unitA.Id}/associations",
            new AssociationInput(personB.Id, UnitPersonRole.Owner, new(2026, 1, 1), null, 1), Json)).StatusCode);
        var buildingB = await Post<BuildingDto>(cb, $"/api/condominiums/{condoB.Id}/buildings", new BuildingInput("B", "Synthetic building"));
        Assert.Equal(HttpStatusCode.NotFound, (await ca.PostAsJsonAsync($"/api/condominiums/{condoA.Id}/units", Unit("U2", buildingB.Id), Json)).StatusCode);
        var other = await Post<CondominiumDto>(ca, "/api/condominiums", Condo("OTHER"));
        var otherBuilding = await Post<BuildingDto>(ca, $"/api/condominiums/{other.Id}/buildings", new BuildingInput("B", "Synthetic building"));
        Assert.Equal(HttpStatusCode.NotFound, (await ca.PostAsJsonAsync($"/api/condominiums/{condoA.Id}/units", Unit("U3", otherBuilding.Id), Json)).StatusCode);
        var injected = System.Text.Json.JsonSerializer.SerializeToNode(Condo("BODY"), Json)!;
        injected["tenantId"] = b.ToString();
        await Post<CondominiumDto>(ca, "/api/condominiums", injected);
        Assert.Single(await Get<CondominiumDto[]>(cb, "/api/condominiums"));
        Assert.Equal(3, (await Get<CondominiumDto[]>(ca, "/api/condominiums")).Length);
        await using var unavailable = Factory(Guid.NewGuid());
        using var missing = unavailable.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await missing.GetAsync("/api/condominiums")).StatusCode);
        await using var production = Factory(a, "Production");
        using var prod = await production.CreateAuthenticatedClient();
        Assert.Equal(HttpStatusCode.OK, (await prod.GetAsync("/api/condominiums")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await prod.PostAsJsonAsync("/api/people", Person(), Json)).StatusCode);
    }
    [PostgresFact]
    public async Task BuildingsStaircasesAndUnitsKeepConsistentHierarchy()
    {
        var tenant = await SeedTenant();
        await using var factory = Factory(tenant); using var client = await factory.CreateAuthenticatedClient();
        var condo = await Post<CondominiumDto>(client, "/api/condominiums", Condo());
        var root = $"/api/condominiums/{condo.Id}";
        var building = await Post<BuildingDto>(client, root + "/buildings", new BuildingInput("B", "Synthetic"));
        foreach (var code in new[] { "A", "B", "C" })
            await Post<StaircaseDto>(client, root + "/staircases", new StaircaseInput(code, "Synthetic " + code, building.Id));
        var structure = await Get<StructureDto>(client, root + "/structure");
        Assert.Equal(3, structure.Staircases.Count);
        var unit = await Post<PropertyUnitDto>(client, root + "/units", Unit("U") with { StaircaseId = structure.Staircases[0].Id });
        Assert.Equal(building.Id, unit.BuildingId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + "/units", Unit("U"), Json)).StatusCode);
        await Post<PropertyUnitDto>(client, root + "/units", Unit("DIRECT"));
        var second = await Post<BuildingDto>(client, root + "/buildings", new BuildingInput("B2", "Synthetic"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(root + "/units", Unit("INVALID", second.Id) with { StaircaseId = structure.Staircases[0].Id }, Json)).StatusCode);
    }
    [PostgresFact]
    public async Task HistoricalAssociationsAreAddedAndConcludedWithoutOverwriting()
    {
        var tenant = await SeedTenant();
        await using var factory = Factory(tenant); using var client = await factory.CreateAuthenticatedClient();
        var condo = await Post<CondominiumDto>(client, "/api/condominiums", Condo());
        var root = $"/api/condominiums/{condo.Id}";
        var unit = await Post<PropertyUnitDto>(client, root + "/units", Unit("U"));
        var person = await Post<PersonDto>(client, "/api/people", Person());
        var path = root + $"/units/{unit.Id}/associations";
        var old = await Post<AssociationDto>(client, path, new AssociationInput(person.Id, UnitPersonRole.Owner, new(2026, 1, 1), null, 0.5m));
        var end = await client.PostAsJsonAsync(root + $"/associations/{old.Id}/end", new EndAssociationInput(new(2026, 1, 31)), Json);
        Assert.Equal(HttpStatusCode.NoContent, end.StatusCode);
        var next = new AssociationInput(person.Id, UnitPersonRole.Owner, new(2026, 2, 1), null, 1);
        await Post<AssociationDto>(client, path, next);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, next, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root + $"/associations/{old.Id}/end", new EndAssociationInput(new(2026, 2, 1)), Json)).StatusCode);
        var history = await Get<AssociationDto[]>(client, root + "/associations");
        Assert.Equal(2, history.Length);
        Assert.Equal(new DateOnly(2026, 1, 31), history.Single(x => x.Id == old.Id).ValidTo);
        await Post<AssociationDto>(client, path, new AssociationInput(person.Id, UnitPersonRole.Tenant, new(2026, 2, 1), null, null));
    }
    [PostgresFact]
    public async Task AllocationDraftValidationActivationAndDecimalRoundTrip()
    {
        var tenant = await SeedTenant();
        await using var factory = Factory(tenant); using var client = await factory.CreateAuthenticatedClient();
        var condo = await Post<CondominiumDto>(client, "/api/condominiums", Condo());
        var root = $"/api/condominiums/{condo.Id}";
        var a = await Post<PropertyUnitDto>(client, root + "/units", Unit("A"));
        var b = await Post<PropertyUnitDto>(client, root + "/units", Unit("B"));
        var table = await Post<AllocationTableDto>(client, root + "/allocation-tables", new AllocationTableInput("T", "Synthetic table", 1000, new(2026, 1, 1)));
        var path = root + "/allocation-tables/" + table.Id;
        Assert.Equal(2, table.Validation.MissingUnitIds.Count); Assert.Equal(AllocationTableStatus.Draft, table.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path + "/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path + "/entries/" + a.Id, new { value = "-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path + "/entries/" + a.Id, new { value = "1.1234567" })).StatusCode);
        table = await Put<AllocationTableDto>(client, path + "/entries/" + a.Id, new { value = "333.333333" });
        Assert.Equal(333.333333m, table.Validation.Total);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path + "/activate", null)).StatusCode);
        table = await Put<AllocationTableDto>(client, path + "/entries/" + b.Id, new { value = "666.666667" });
        Assert.True(table.Validation.IsValid); Assert.Equal(1000m, table.Validation.Total);
        table = await Post<AllocationTableDto>(client, path + "/activate");
        Assert.Equal(AllocationTableStatus.Active, table.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path + "/entries/" + a.Id, new { value = "0" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(path)).StatusCode);
        await using var db = Db(tenant);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "MillesimalShares" SET "Value" = 0 WHERE "MillesimalTableId" = {table.Id}
            """));
        Assert.Equal("55000", exception.SqlState);
    }
    [PostgresFact]
    public async Task AccountingIntervalsExcludeOverlapAndClosedPeriodsAreImmutable()
    {
        var tenant = await SeedTenant();
        await using var factory = Factory(tenant); using var client = await factory.CreateAuthenticatedClient();
        var condo = await Post<CondominiumDto>(client, "/api/condominiums", Condo());
        var path = $"/api/condominiums/{condo.Id}/accounting-periods";
        var input = new AccountingPeriodInput("A", "Synthetic exercise", new(2026, 7, 1), new(2027, 6, 30));
        var period = await Post<AccountingPeriodDto>(client, path, input);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, input with { Code = "B", StartsOn = new(2027, 6, 30), EndsOn = new(2028, 6, 30) }, Json)).StatusCode);
        await Post<AccountingPeriodDto>(client, path, input with { Code = "B", StartsOn = new(2027, 7, 1), EndsOn = new(2028, 6, 30) });
        period = await Post<AccountingPeriodDto>(client, path + "/" + period.Id + "/open");
        Assert.Equal(AccountingPeriodStatus.Open, period.Status);
        period = await Post<AccountingPeriodDto>(client, path + "/" + period.Id + "/close");
        Assert.Equal(AccountingPeriodStatus.Closed, period.Status); Assert.NotNull(period.ClosedAt);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path + "/" + period.Id, input, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path + "/" + period.Id + "/open", null)).StatusCode);
        await using var db = Db(tenant);
        var closed = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "AccountingPeriods" SET "Name" = 'Invalid change' WHERE "Id" = {period.Id}
            """));
        Assert.Equal("55000", closed.SqlState);
        var overlap = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AccountingPeriods" ("Id","TenantId","CondominiumId","Code","Name","StartsOn","EndsOn","Status","CreatedAt","UpdatedAt")
            VALUES ({Guid.NewGuid()},{tenant},{condo.Id},{"RAW"},{"Synthetic"},DATE '2026-09-01',DATE '2026-10-01',0,now(),now())
            """));
        Assert.Equal(PostgresErrorCodes.ExclusionViolation, overlap.SqlState);
        var future = input with { Code = "CONCURRENT", StartsOn = new(2030, 1, 1), EndsOn = new(2030, 12, 31) };
        var responses = await Task.WhenAll(client.PostAsJsonAsync(path, future, Json), client.PostAsJsonAsync(path, future with { Code = "CONCURRENT2" }, Json));
        Assert.Single(responses.Where(x => x.IsSuccessStatusCode));
        Assert.Single(responses.Where(x => x.StatusCode == HttpStatusCode.Conflict));
    }
    [PostgresFact]
    public async Task MigrationUpgradesPopulatedMilestone01WithoutLosingIdsOrHistory()
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection);
        var database = "cm_upgrade_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(Connection)) {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }
        builder.Database = database;
        var tenant = Guid.NewGuid(); var condo = Guid.NewGuid(); var unit = Guid.NewGuid(); var person = Guid.NewGuid(); var link = Guid.NewGuid();
        await using var db = Db(tenant, builder.ConnectionString);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261105220532_InitialFoundation");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Tenants" ("Id","Code","Name","CreatedAt","UpdatedAt") VALUES ({tenant},{"OLD"},{"Synthetic"},now(),now());
            INSERT INTO "Condominiums" ("Id","TenantId","Code","Name","CreatedAt","UpdatedAt") VALUES ({condo},{tenant},{"C"},{"Synthetic"},now(),now());
            INSERT INTO "PropertyUnits" ("Id","TenantId","CondominiumId","Code","CreatedAt","UpdatedAt") VALUES ({unit},{tenant},{condo},{"U"},now(),now());
            INSERT INTO "People" ("Id","TenantId","Code","DisplayName","CreatedAt","UpdatedAt") VALUES ({person},{tenant},{"P"},{"Synthetic subject"},now(),now());
            INSERT INTO "OwnershipPeriods" ("Id","TenantId","PropertyUnitId","PersonId","Fraction","ValidFrom","CreatedAt","UpdatedAt")
                VALUES ({link},{tenant},{unit},{person},0.5,DATE '2026-01-01',now(),now());
            """);
        await migrator.MigrateAsync();
        Assert.True((await db.Condominiums.SingleAsync()).IsActive);
        Assert.Equal(unit, (await db.PropertyUnits.SingleAsync()).Id);
        var old = await db.OwnershipPeriods.SingleAsync();
        Assert.Equal(link, old.Id); Assert.Equal(0.5m, old.Fraction); Assert.Equal(UnitPersonRole.Owner, old.Role);
        var subject = await db.People.SingleAsync();
        Assert.Equal(PersonKind.Unspecified, subject.Kind); Assert.Equal("Synthetic subject", subject.LastNameOrCompany);
    }
}