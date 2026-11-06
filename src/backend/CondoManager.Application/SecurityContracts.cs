using CondoManager.Domain;

namespace CondoManager.Application;

public record LoginInput(string Email, string Password);
public record ChangePasswordInput(string CurrentPassword, string NewPassword);
public record ForgotPasswordInput(string Email);
public record ResetPasswordInput(string Token, string NewPassword);
public record AcceptInvitationInput(string Token, string? Password);
public record SelectTenantInput(Guid TenantId);
public record InviteInput(string Email, [property: System.Text.Json.Serialization.JsonRequired] StudioRole Role);
public record MembershipInput([property: System.Text.Json.Serialization.JsonRequired] StudioRole Role, [property: System.Text.Json.Serialization.JsonRequired] bool IsActive);
public record UserStatusInput([property: System.Text.Json.Serialization.JsonRequired] bool IsActive);
public record MembershipDto(Guid Id, Guid UserId, string Email, StudioRole Role, bool IsActive, bool UserIsActive);
public record TenantAccessDto(Guid TenantId, string Name, StudioRole Role);
public record CurrentUserDto(Guid Id, string Email, bool IsPlatformAdmin, IReadOnlyList<TenantAccessDto> Memberships,
    Guid? ActiveTenantId, string? ActiveTenantName, StudioRole? Role, bool CanWrite, bool IsDevelopment = false);
public record InvitationDto(Guid Id, string Email, StudioRole Role, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt, DateTimeOffset? RevokedAt);
public record PlatformUserDto(Guid Id, string Email, bool IsActive);
public record AuditDto(Guid Id, DateTimeOffset Timestamp, string Event, Guid? ActorUserId, Guid? SubjectId);
public record SecurityMessage(string Recipient, string Kind, string Token, Guid? ActorUserId, Guid? TenantId);
public interface ISecurityMessageSender
{
    bool IsAvailable { get; }
    Task Send(SecurityMessage message, CancellationToken ct);
}
public sealed class SecurityDeliveryUnavailableException() : Exception("Invio email non configurato.");