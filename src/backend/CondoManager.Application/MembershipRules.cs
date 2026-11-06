namespace CondoManager.Application;

public static class MembershipRules
{
    public static void EnsureAdminRemains(bool removesActiveAdmin, int otherActiveAdmins)
    {
        if (removesActiveAdmin && otherActiveAdmins == 0)
            throw new InvalidOperationException("Non e possibile rimuovere o disabilitare l'ultimo StudioAdmin attivo.");
    }
}