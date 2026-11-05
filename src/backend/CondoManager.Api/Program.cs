using System.Text.Json.Serialization;
using CondoManager.Api;
using CondoManager.Application;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ITenantContext>(services => new ServerTenantContext(
    DevelopmentTenant.Read(services.GetRequiredService<IHostEnvironment>(), services.GetRequiredService<IConfiguration>())));
builder.Services.AddScoped<IRegistryService, RegistryService>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("CondoManager")
        ?? throw new TenantUnavailableException();
    options.UseNpgsql(connection);
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.SerializerOptions.Converters.Add(new DecimalJsonConverter());
});
builder.Services.AddProblemDetails();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}
var app = builder.Build();
var developmentTenant = DevelopmentTenant.Read(app.Environment, app.Configuration);
app.UseExceptionHandler(error => error.Run(context => ApiErrors.Handle(context, context.Features.Get<IExceptionHandlerFeature>()!.Error)));
app.Use(async (context, next) => {
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/session", () => Results.Ok(new {
    dataAccessEnabled = app.Environment.IsDevelopment() && developmentTenant.HasValue,
    mode = app.Environment.IsDevelopment() ? "Development" : "Unavailable"
}));
if (app.Environment.IsDevelopment() && developmentTenant.HasValue) app.MapRegistry();
app.Run();
public partial class Program { }
internal sealed record ServerTenantContext(Guid? TenantId) : ITenantContext;
internal static class DevelopmentTenant
{
    public static Guid? Read(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment()) return null;
        var configured = configuration["DevelopmentTenant:Id"];
        if (string.IsNullOrWhiteSpace(configured)) return null;
        if (!Guid.TryParse(configured, out var id) || id == Guid.Empty)
            throw new InvalidOperationException("DevelopmentTenant:Id deve essere un UUID non vuoto.");
        return id;
    }
}