using CondoManager.Domain;
using Microsoft.AspNetCore.Identity;

namespace CondoManager.Infrastructure;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser() => Id = Guid.NewGuid();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? ActiveTenantId { get; set; }
    public string SecurityStamp { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public enum GrantPurpose { Invitation, PasswordReset }
public sealed class SecurityGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GrantPurpose Purpose { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public StudioRole? Role { get; set; }
    public string TokenHash { get; set; } = "";
    public string? SecurityStamp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public sealed class SecurityAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? SubjectId { get; set; }
    public string Event { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}