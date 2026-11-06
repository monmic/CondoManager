using CondoManager.Domain;

namespace CondoManager.UnitTests;
public class Milestone02Tests
{
    [Fact] public void AllocationNeedsEveryUnitAndExactDecimalTotal()
    {
        var tenant = Guid.NewGuid(); var condo = Guid.NewGuid(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 1);
        var table = new AllocationTable(tenant, condo, "T");
        table.Update("T", "Synthetic", 1000, date);
        var entryA = new AllocationTableEntry(tenant, condo, table.Id, a, 333.333333m, date, null);
        var entryB = new AllocationTableEntry(tenant, condo, table.Id, b, 666.666667m, date, null);
        Assert.Contains(b, table.Validate(new[] { a, b }, new[] { entryA }).MissingUnitIds);
        Assert.Throws<InvalidOperationException>(() => table.Activate(new[] { a, b }, new[] { entryA }));
        Assert.Equal(1000m, table.Validate(new[] { a, b }, new[] { entryA, entryB }).Total);
        table.Activate(new[] { a, b }, new[] { entryA, entryB });
        Assert.Equal(AllocationTableStatus.Active, table.Status);
        Assert.Throws<InvalidOperationException>(() => table.Update("T", "Change", 1000, date));
        Assert.Throws<InvalidOperationException>(() => table.Archive());
        Assert.Throws<ArgumentOutOfRangeException>(() => entryA.SetValue(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => entryA.SetValue(0.1234567m));
    }
    [Fact] public void DuplicateUnexpectedAndNoUnitsCannotActivate()
    {
        var id = Guid.NewGuid(); var unit = Guid.NewGuid(); var date = new DateOnly(2026, 1, 1);
        var table = new AllocationTable(id, id, "T"); table.Update("T", "Synthetic", 1000, date);
        var entry = new AllocationTableEntry(id, id, table.Id, unit, 500, date, null);
        Assert.True(table.Validate(new[] { unit }, new[] { entry, entry }).HasDuplicates);
        Assert.False(table.Validate(Array.Empty<Guid>(), new[] { entry }).IsValid);
        Assert.Throws<InvalidOperationException>(() => table.Activate(Array.Empty<Guid>(), Array.Empty<AllocationTableEntry>()));
    }
    [Fact] public void ClosedPeriodsCannotBeEditedOrReopened()
    {
        var id = Guid.NewGuid();
        var period = new AccountingPeriod(id, id, "A", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30));
        Assert.True(period.Overlaps(new DateOnly(2027, 6, 30), new DateOnly(2028, 1, 1)));
        Assert.False(period.Overlaps(new DateOnly(2027, 7, 1), new DateOnly(2028, 6, 30)));
        Assert.Throws<InvalidOperationException>(() => period.Close());
        period.Open();
        Assert.Throws<InvalidOperationException>(() => period.Update("A", "Change", period.StartsOn, period.EndsOn));
        period.Close();
        Assert.NotNull(period.ClosedAt);
        Assert.Equal(AccountingPeriodStatus.Closed, period.Status);
        Assert.Throws<InvalidOperationException>(() => period.Open());
        Assert.Throws<InvalidOperationException>(() => period.Close());
        Assert.Throws<InvalidOperationException>(() => period.Update("A", "Change", period.StartsOn, period.EndsOn));
    }
    [Fact] public void AssociationsCloseWithoutReplacingHistoricalIdentity()
    {
        var id = Guid.NewGuid(); var date = new DateOnly(2026, 1, 1);
        var link = new OwnershipPeriod(id, id, id, date, null, null, UnitPersonRole.Tenant);
        var oldId = link.Id;
        link.End(date.AddMonths(1));
        Assert.Equal(oldId, link.Id); Assert.Equal(date, link.ValidFrom);
        Assert.Throws<InvalidOperationException>(() => link.End(date.AddMonths(2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OwnershipPeriod(id, id, id, date, null, null));
        Assert.Throws<ArgumentException>(() => new OwnershipPeriod(id, id, id, date, null, 1, UnitPersonRole.Tenant));
    }
    [Fact] public void PersonAndAddressInputAreValidated()
    {
        var person = new Person(Guid.NewGuid(), "P", "Synthetic");
        Assert.Throws<ArgumentException>(() => person.Update("P", PersonKind.NaturalPerson, null, "Synthetic", "SYNTHETIC", null, null, null, null, null, true));
        Assert.Throws<ArgumentException>(() => person.Update("P", PersonKind.LegalEntity, null, "Synthetic", "SYNTHETIC", null, "invalid", null, null, null, true));
        var condo = new Condominium(Guid.NewGuid(), "C", "Synthetic");
        Assert.Throws<ArgumentException>(() => condo.Update("C", "Synthetic", null, "", "00000", "Synthetic", "XX", "Test", true));
    }
}