using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;

namespace CondoManager.Api;

public static class SecurityEndpoints
{
    public static void MapSecurity(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken })).AllowAnonymous();
        auth.MapPost("/login", async (LoginInput input, IdentityService service, CancellationToken ct) =>
            await service.Login(input, ct) ? Results.NoContent() : Results.Problem(statusCode: 401, title: "Credenziali non valide o accesso temporaneamente sospeso."))
            .AllowAnonymous().RequireRateLimiting("authentication");
        auth.MapPost("/forgot-password", async (ForgotPasswordInput input, IdentityService service, CancellationToken ct) => {
            await service.ForgotPassword(input.Email, ct); return Results.Accepted();
        }).AllowAnonymous().RequireRateLimiting("authentication");
        auth.MapPost("/reset-password", async (ResetPasswordInput input, IdentityService service, CancellationToken ct) => {
            await service.ResetPassword(input, ct); return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("authentication");
        auth.MapPost("/accept-invitation", async (AcceptInvitationInput input, IdentityService service, CancellationToken ct) => {
            await service.AcceptInvitation(input, ct); return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("authentication");
        auth.MapGet("/me", (IdentityService service, CancellationToken ct) => service.Me(ct)).RequireAuthorization();
        auth.MapPost("/logout", async (IdentityService service, CancellationToken ct) => {
            await service.Logout(ct); return Results.NoContent();
        }).RequireAuthorization();
        auth.MapPost("/tenant", async (SelectTenantInput input, IdentityService service, CancellationToken ct) => {
            await service.SelectTenant(input.TenantId, ct); return Results.NoContent();
        }).RequireAuthorization();
        auth.MapPost("/change-password", async (ChangePasswordInput input, IdentityService service, CancellationToken ct) => {
            await service.ChangePassword(input, ct); return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("authentication");

        var studio = app.MapGroup("/api/studio").RequireAuthorization("StudioAdmin");
        studio.MapGet("/members", (IdentityService service, CancellationToken ct) => service.Members(ct));
        studio.MapPut("/members/{id:guid}", async (Guid id, MembershipInput input, IdentityService service, CancellationToken ct) => {
            await service.ChangeMembership(id, input, ct); return Results.NoContent();
        });
        studio.MapGet("/invitations", (IdentityService service, CancellationToken ct) => service.Invitations(ct));
        studio.MapPost("/invitations", (InviteInput input, IdentityService service, CancellationToken ct) => service.Invite(input, ct));
        studio.MapPost("/invitations/{id:guid}/revoke", async (Guid id, IdentityService service, CancellationToken ct) => {
            await service.RevokeInvitation(id, ct); return Results.NoContent();
        });
        studio.MapGet("/audit", (IdentityService service, CancellationToken ct) => service.StudioAudit(ct));

        var platform = app.MapGroup("/api/platform").RequireAuthorization("PlatformAdmin");
        platform.MapGet("/users", (IdentityService service, CancellationToken ct) => service.PlatformUsers(ct));
        platform.MapPut("/users/{id:guid}/status", async (Guid id, UserStatusInput input, IdentityService service, CancellationToken ct) => {
            await service.SetUserStatus(id, input.IsActive, ct); return Results.NoContent();
        });
        if (app.Environment.IsDevelopment())
            app.MapGet("/api/development/messages", async (IdentityService service, DevelopmentMailbox mailbox, CancellationToken ct) => {
                var me = await service.Me(ct);
                return mailbox.ForUser(me.Id, me.Email, me.ActiveTenantId, me.Role == StudioRole.StudioAdmin);
            }).RequireAuthorization();
    }
}