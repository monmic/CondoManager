namespace CondoManager.Domain;

public static class Guard
{
    public static Guid Id(Guid value) => value != Guid.Empty ? value : throw new ArgumentException("UUID obbligatorio.");
    public static string Text(string value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 160
        ? value.Trim() : throw new ArgumentException("Testo obbligatorio, massimo 160 caratteri.");
    public static void Dates(DateOnly start, DateOnly? end)
    {
        if (end < start) throw new ArgumentException("La fine precede l'inizio.");
    }
}

public abstract class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
}
public abstract class TenantEntity : Entity
{
    public Guid TenantId { get; private set; }
    protected TenantEntity(Guid tenantId) => TenantId = Guard.Id(tenantId);
}
public sealed class Tenant : Entity
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public Tenant(string code, string name) { Code = Guard.Text(code); Name = Guard.Text(name); }
}
public sealed class Condominium : TenantEntity
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public Condominium(Guid tenantId, string code, string name) : base(tenantId)
    { Code = Guard.Text(code); Name = Guard.Text(name); }
}
public sealed class BuildingOrStaircase : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public BuildingOrStaircase(Guid tenantId, Guid condominiumId, string code) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); }
}
public sealed class PropertyUnit : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid? BuildingOrStaircaseId { get; private set; }
    public string Code { get; private set; }
    public PropertyUnit(Guid tenantId, Guid condominiumId, string code, Guid? buildingOrStaircaseId = null) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); BuildingOrStaircaseId = buildingOrStaircaseId is null ? null : Guard.Id(buildingOrStaircaseId.Value); }
}
public sealed class Person : TenantEntity
{
    public string Code { get; private set; }
    public string DisplayName { get; private set; }
    public Person(Guid tenantId, string code, string displayName) : base(tenantId)
    { Code = Guard.Text(code); DisplayName = Guard.Text(displayName); }
}
public sealed class OwnershipPeriod : TenantEntity
{
    public Guid PropertyUnitId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal Fraction { get; private set; }
    public OwnershipPeriod(Guid tenantId, Guid propertyUnitId, Guid personId, DateOnly validFrom, DateOnly? validTo, decimal fraction) : base(tenantId)
    {
        Guard.Dates(validFrom, validTo);
        if (fraction <= 0 || fraction > 1) throw new ArgumentOutOfRangeException(nameof(fraction));
        PropertyUnitId = Guard.Id(propertyUnitId); PersonId = Guard.Id(personId);
        ValidFrom = validFrom; ValidTo = validTo; Fraction = fraction;
    }
}
public sealed class MillesimalTable : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public MillesimalTable(Guid tenantId, Guid condominiumId, string code) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); }
}
public sealed class MillesimalShare : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid MillesimalTableId { get; private set; }
    public Guid PropertyUnitId { get; private set; }
    public decimal Value { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public MillesimalShare(Guid tenantId, Guid condominiumId, Guid millesimalTableId, Guid propertyUnitId, decimal value, DateOnly validFrom, DateOnly? validTo) : base(tenantId)
    {
        Guard.Dates(validFrom, validTo);
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        CondominiumId = Guard.Id(condominiumId); MillesimalTableId = Guard.Id(millesimalTableId);
        PropertyUnitId = Guard.Id(propertyUnitId); Value = value; ValidFrom = validFrom; ValidTo = validTo;
    }
}
public sealed class AccountingPeriod : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public AccountingPeriod(Guid tenantId, Guid condominiumId, string code, DateOnly startsOn, DateOnly endsOn) : base(tenantId)
    { Guard.Dates(startsOn, endsOn); CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); StartsOn = startsOn; EndsOn = endsOn; }
}