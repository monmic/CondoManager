using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using CondoManager.Api;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args.Where(x => x != "--bootstrap").ToArray());
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ITenantContext>(s => s.GetRequiredService<CurrentTenant>());
builder.Services.AddScoped<ICurrentTenant>(s => s.GetRequiredService<CurrentTenant>());
builder.Services.AddScoped<IRegistryService, RegistryService>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<IAccountingService>(s=>s.GetRequiredService<AccountingService>());
builder.Services.AddScoped<IFinalStatementService>(s=>s.GetRequiredService<AccountingService>());
builder.Services.AddScoped<IResidentFinanceService>(s=>s.GetRequiredService<AccountingService>());
builder.Services.AddSingleton<IAllocationEngine, AllocationEngine>();
builder.Services.AddScoped<AllocationService>();
builder.Services.AddScoped<IAllocationService>(s=>s.GetRequiredService<AllocationService>());
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<IdentityBootstrap>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("CondoManager")
        ?? throw new TenantUnavailableException();
    options.UseNpgsql(connection);
});
builder.Services.AddIdentityCore<ApplicationUser>(options => {
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12; options.Password.RequiredUniqueChars = 4;
    options.Password.RequireDigit = true; options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedEmail = true;
}).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>()
  .AddSignInManager().AddDefaultTokenProviders().AddPasswordValidator<BoundedPasswordValidator>();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.Name = "__Host-CondoManager"; options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/"; options.SlidingExpiration = false; options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnValidatePrincipal = RequestSecurity.ValidateCookie;
    options.Events.OnRedirectToLogin = context => Results.Problem(statusCode: 401, title: "Autenticazione richiesta.").ExecuteAsync(context.HttpContext);
    options.Events.OnRedirectToAccessDenied = context => Results.Problem(statusCode: 403, title: "Accesso non consentito.").ExecuteAsync(context.HttpContext);
});
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-CondoManager-CSRF";
    options.Cookie.HttpOnly = true; options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("TenantMember", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<CurrentTenant>().Role is StudioRole.StudioAdmin or StudioRole.Collaborator or StudioRole.Viewer));
    options.AddPolicy("StudioAdmin", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<CurrentTenant>().Role == StudioRole.StudioAdmin));
    options.AddPolicy("PlatformAdmin", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.Resource is HttpContext http ? RequestSecurity.IsPlatformAdmin(http) : Task.FromResult(false)));
});
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = (context, _) => new ValueTask(Results.Problem(statusCode: 429, title: "Troppe richieste. Riprova tra poco.").ExecuteAsync(context.HttpContext));
});
var protection = builder.Services.AddDataProtection().SetApplicationName("CondoManager");
if (builder.Configuration["DataProtection:KeyPath"] is string keys)
{
    protection.PersistKeysToFileSystem(new DirectoryInfo(keys));
    if (builder.Configuration["DataProtection:CertificatePath"] is string certificate)
        protection.ProtectKeysWithCertificate(new X509Certificate2(certificate, builder.Configuration["DataProtection:CertificatePassword"]));
    else if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
    else if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Proteggere il key ring con un certificato.");
}
builder.Services.AddSingleton<DevelopmentMailbox>();
builder.Services.AddScoped<ISecurityMessageSender>(services => {
    var config = services.GetRequiredService<IConfiguration>();
    if (services.GetRequiredService<IHostEnvironment>().IsDevelopment() && string.IsNullOrWhiteSpace(config["Smtp:Host"]))
        return services.GetRequiredService<DevelopmentMailbox>();
    return new SmtpSecurityMessageSender(config, services.GetRequiredService<IHostEnvironment>());
});
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.SerializerOptions.Converters.Add(new DecimalJsonConverter());
});
builder.Services.AddProblemDetails();
if (builder.Environment.IsDevelopment()) { builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(); }
var app = builder.Build();
if (args.Contains("--bootstrap"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IdentityBootstrap>().Run();
    System.Console.WriteLine("Bootstrap completato. Nessuna credenziale visualizzata.");
    return;
}
app.UseExceptionHandler(error => error.Run(context => ApiErrors.Handle(context, context.Features.Get<IExceptionHandlerFeature>()!.Error)));
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (context, next) => {
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    if (!context.Request.Path.StartsWithSegments("/swagger"))
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api") && !context.Request.IsHttps) {
        await Results.Problem(statusCode: 400, title: "HTTPS obbligatorio.").ExecuteAsync(context); return;
    }
    if (context.Request.Path.StartsWithSegments("/api/auth") && context.Request.ContentLength > 16384) {
        context.Response.StatusCode = 413; return;
    }
    await next(context);
});
// A built SPA is public; data stays behind authenticated API policies.
var configuredFrontend = app.Configuration["Frontend:RootPath"];
var frontendPath = configuredFrontend == null ? null : Path.GetFullPath(configuredFrontend, app.Environment.ContentRootPath);
if (!string.IsNullOrEmpty(frontendPath) && Directory.Exists(frontendPath))
{
    var files = new PhysicalFileProvider(Path.GetFullPath(frontendPath));
    app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
    app.MapGet("/", () => Results.File(Path.Combine(files.Root, "index.html"), "text/html")).AllowAnonymous();
}
app.UseRouting();
app.UseAuthentication();
app.Use(RequestSecurity.ResolveTenant);
app.UseAuthorization();
app.Use(async (context, next) => {
    var business = context.Request.Path.StartsWithSegments("/api/final-statements") || context.Request.Path.StartsWithSegments("/api/resident-finance") || context.Request.Path.StartsWithSegments("/api/budgets") || context.Request.Path.StartsWithSegments("/api/allocations") || context.Request.Path.StartsWithSegments("/api/accounting") || context.Request.Path.StartsWithSegments("/api/condominiums") || context.Request.Path.StartsWithSegments("/api/people") || context.Request.Path.StartsWithSegments("/api/studio");
    var active = context.RequestServices.GetRequiredService<CurrentTenant>().TenantId;
    if (business && active.HasValue && context.Request.Headers.TryGetValue("X-Expected-Tenant", out var expected) &&
        (!Guid.TryParse(expected.ToString(), out var expectedId) || expectedId != active))
    {
        await Results.Problem(statusCode: 409, title: "Lo studio attivo e cambiato. Ricarica i dati.",
            extensions: new Dictionary<string, object?> { ["code"] = "tenant_changed" }).ExecuteAsync(context); return;
    }
    await next(context);
});
app.UseRateLimiter();
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
    {
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) {
            await Results.Problem(statusCode: 400, title: "Verifica CSRF non riuscita. Ricarica la pagina.").ExecuteAsync(context); return;
        }
    }
    await next(context);
});
if (app.Environment.IsDevelopment()) {
    app.MapSwagger().RequireAuthorization();
    app.UseSwaggerUI();
}
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapSecurity();
app.MapRegistry();
app.MapAccounting();
app.MapAllocations();
app.MapBudgets();
app.MapResidentFinance();
app.MapFinalStatements();
app.Run();
public partial class Program { }