namespace CondoManager.Domain;

public enum StudioRole { StudioAdmin, Collaborator, Viewer }

public sealed class TenantMembership
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public StudioRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public TenantMembership(Guid tenantId, Guid userId, StudioRole role)
    {
        TenantId = Guard.Id(tenantId); UserId = Guard.Id(userId); SetAccess(role, true);
    }
    public void SetAccess(StudioRole role, bool isActive)
    {
        Role = Guard.EnumValue(role); IsActive = isActive; UpdatedAt = DateTimeOffset.UtcNow;
    }
}