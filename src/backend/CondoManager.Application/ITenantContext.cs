using CondoManager.Domain;

namespace CondoManager.Application;

public interface ITenantContext { Guid? TenantId { get; } }

// Only the server authentication/membership pipeline populates this scoped context.
public interface ICurrentTenant : ITenantContext
{
    Guid? UserId { get; }
    StudioRole? Role { get; }
    bool CanWrite => Role is StudioRole.StudioAdmin or StudioRole.Collaborator;
    Guid RequireRead()
    {
        if (UserId is null || TenantId is null || Role is null) throw new AccessDeniedException();
        return TenantId.Value;
    }
    void RequireWrite()
    {
        RequireRead();
        if (!CanWrite) throw new AccessDeniedException();
    }
    void RequireAdmin()
    {
        RequireRead();
        if (Role != StudioRole.StudioAdmin) throw new AccessDeniedException();
    }
}
public sealed class AccessDeniedException() : Exception("Accesso non consentito per lo studio o il ruolo corrente.");
public sealed class UnauthenticatedTenantContext : ICurrentTenant
{
    public Guid? TenantId => null;
    public Guid? UserId => null;
    public StudioRole? Role => null;
}