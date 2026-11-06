using CondoManager.Application;
using CondoManager.Domain;

namespace CondoManager.UnitTests;

public class MembershipTests
{
    [Fact] public void LastAdminCannotBeRemovedButOtherRolesAndAdditionalAdminsCan()
    {
        Assert.Throws<InvalidOperationException>(() => MembershipRules.EnsureAdminRemains(true, 0));
        MembershipRules.EnsureAdminRemains(false, 0);
        MembershipRules.EnsureAdminRemains(true, 1);
    }
    [Fact] public void MembershipHasSeparateIdentityAndCanBeDisabledWithoutDeletingAccount()
    {
        var user = Guid.NewGuid(); var a = new TenantMembership(Guid.NewGuid(), user, StudioRole.Viewer);
        var b = new TenantMembership(Guid.NewGuid(), user, StudioRole.StudioAdmin);
        a.SetAccess(StudioRole.Collaborator, false);
        Assert.Equal(user, a.UserId); Assert.Equal(user, b.UserId);
        Assert.NotEqual(a.TenantId, b.TenantId); Assert.False(a.IsActive); Assert.True(b.IsActive);
        Assert.Throws<ArgumentException>(() => a.SetAccess((StudioRole)99, true));
        Assert.Throws<ArgumentException>(() => new TenantMembership(Guid.Empty, user, StudioRole.Viewer));
    }
}