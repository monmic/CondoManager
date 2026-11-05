using CondoManager.Domain;

namespace CondoManager.UnitTests;
public class DomainTests
{
    [Fact] public void TenantAndCodesAreRequired()
    {
        Assert.Throws<ArgumentException>(() => new Condominium(Guid.Empty, "A", "Test"));
        Assert.Throws<ArgumentException>(() => new Person(Guid.NewGuid(), " ", "Test"));
        Assert.Throws<ArgumentException>(() => new Condominium(Guid.NewGuid(), "A", new string('x', 161)));
    }
    [Fact] public void OwnershipRequiresPositiveFractionAndOrderedDates()
    {
        var id = Guid.NewGuid(); var date = new DateOnly(2026, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new OwnershipPeriod(id, id, id, date, null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OwnershipPeriod(id, id, id, date, null, 1.01m));
        Assert.Throws<ArgumentException>(() => new OwnershipPeriod(id, id, id, date, date.AddDays(-1), 0.5m));
        Assert.Equal(0.3333333333m, new OwnershipPeriod(id, id, id, date, null, 0.3333333333m).Fraction);
    }
    [Fact] public void MillesimalValuesAndPeriodsAreValidated()
    {
        var id = Guid.NewGuid(); var date = new DateOnly(2026, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MillesimalShare(id, id, id, id, -1, date, null));
        Assert.Throws<ArgumentException>(() => new AccountingPeriod(id, id, "2026", date, date.AddDays(-1)));
        var row = new MillesimalShare(id, id, id, id, 42.123456m, date, date);
        Assert.Equal(42.123456m, row.Value);
        Assert.NotEqual(Guid.Empty, row.Id);
        Assert.Equal(row.CreatedAt.Date, row.UpdatedAt.Date);
    }
}