using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CondoManager.IntegrationTests;
public class TenantConfigurationTests
{
    [Theory]
    [InlineData("Production", "valid")]
    [InlineData("Staging", "valid")]
    [InlineData("Development", "valid")]
    [InlineData("Development", "invalid")]
    public async Task LegacyDevelopmentTenantNeverAuthorizesAnonymousAccess(string environment, string value)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
                ["DevelopmentTenant:Id"] = value == "valid" ? Guid.NewGuid().ToString() : "not-an-id"
            }));
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("TenantId", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/condominiums")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/people")).StatusCode);
    }
}