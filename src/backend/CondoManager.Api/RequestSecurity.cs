using System.Security.Claims;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.Api;

public sealed class CurrentTenant : ICurrentTenant
{
    public Guid? UserId { get; private set; }
    public Guid? TenantId { get; private set; }
    public StudioRole? Role { get; private set; }
    internal void Resolve(Guid userId, TenantMembership? membership)
    {
        UserId = userId; TenantId = membership?.TenantId; Role = membership?.Role;
    }
}
public static class RequestSecurity
{
    public static async Task ValidateCookie(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !Guid.TryParse(context.Principal?.FindFirstValue(IdentityService.SessionClaim), out var sessionId))
        { context.RejectPrincipal(); return; }
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var now = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, context.HttpContext.RequestAborted);
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now, context.HttpContext.RequestAborted);
        if (user == null || !user.IsActive || session == null || user.SecurityStamp != session.SecurityStamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        context.HttpContext.Items[typeof(UserSession)] = session;
    }
    public static async Task ResolveTenant(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.Items[typeof(UserSession)] is UserSession session)
        {
            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            var membership = session.ActiveTenantId.HasValue ? await db.Memberships.AsNoTracking().SingleOrDefaultAsync(x =>
                x.UserId == session.UserId && x.TenantId == session.ActiveTenantId && x.IsActive, context.RequestAborted) : null;
            context.RequestServices.GetRequiredService<CurrentTenant>().Resolve(session.UserId, membership);
        }
        await next(context);
    }
    public static async Task<bool> IsPlatformAdmin(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true || !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return false;
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        return await (from userRole in db.UserRoles join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == id && role.Name == IdentityService.PlatformRole select userRole.UserId).AnyAsync(context.RequestAborted);
    }
}