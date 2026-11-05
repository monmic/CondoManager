namespace CondoManager.Application;

// Implementations must derive this value exclusively from verified authentication.
public interface ITenantContext
{
    Guid? TenantId { get; }
}
public sealed class UnauthenticatedTenantContext : ITenantContext
{
    public Guid? TenantId => null;
}