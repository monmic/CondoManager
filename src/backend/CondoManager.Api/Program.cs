using CondoManager.Application;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ITenantContext, UnauthenticatedTenantContext>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("CondoManager")
        ?? throw new InvalidOperationException("Configurare ConnectionStrings__CondoManager.");
    options.UseNpgsql(connection);
});
builder.Services.AddHealthChecks();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();
public partial class Program { }