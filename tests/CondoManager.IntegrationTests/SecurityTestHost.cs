using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CondoManager.IntegrationTests;

public sealed record TestAccount(Guid Id, string Email, string Password);
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan span) => now += span;
}
public sealed class SecurityTestHost(Guid? defaultTenant = null, string environment = "Development") : WebApplicationFactory<Program>
{
    public TestClock Clock { get; } = new();
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        NumberHandling = JsonNumberHandling.AllowReadingFromString, Converters = { new JsonStringEnumConverter() }
    };
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:CondoManager"] = Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES"),
            ["Documents:RootPath"] = Path.Combine(Directory.GetCurrentDirectory(), ".artifacts", "document-integration", defaultTenant?.ToString("N") ?? Guid.NewGuid().ToString("N")),
            ["DevelopmentTenant:Id"] = defaultTenant?.ToString()
        }));
        builder.ConfigureServices(services => {
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(Clock);
        });
    }
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
    });
    public static string Password() => Convert.ToHexString(RandomNumberGenerator.GetBytes(18)) + "aA1!";
    public async Task<TestAccount> Account(Guid? tenantId, StudioRole role = StudioRole.StudioAdmin, bool platform = false)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = "synthetic-" + Guid.NewGuid().ToString("N") + "@test.invalid";
        var password = Password();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        IdentityService.Ensure(await manager.CreateAsync(user, password));
        if (tenantId.HasValue) db.Memberships.Add(new(tenantId.Value, user.Id, role));
        if (platform)
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roles.RoleExistsAsync(IdentityService.PlatformRole)) IdentityService.Ensure(await roles.CreateAsync(new(IdentityService.PlatformRole)));
            IdentityService.Ensure(await manager.AddToRoleAsync(user, IdentityService.PlatformRole));
        }
        await db.SaveChangesAsync();
        return new(user.Id, email, password);
    }
    public static async Task Csrf(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", body.GetProperty("token").GetString());
    }
    public static async Task Login(HttpClient client, TestAccount account, Guid? tenant = null)
    {
        await Csrf(client);
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginInput(account.Email, account.Password));
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        await Csrf(client);
        if (tenant.HasValue) (await client.PostAsJsonAsync("/api/auth/tenant", new SelectTenantInput(tenant.Value))).EnsureSuccessStatusCode();
    }
    public async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = Browser();
        await Login(client, await Account(defaultTenant), defaultTenant);
        return client;
    }
}
