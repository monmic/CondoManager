using System.Net.Mail;

namespace CondoManager.Domain;

public static class Guard
{
    public static Guid Id(Guid value) => value != Guid.Empty ? value : throw new ArgumentException("UUID obbligatorio.");
    public static string Text(string? value, int max = 160) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max
        ? value.Trim() : throw new ArgumentException($"Testo obbligatorio, massimo {max} caratteri.");
    public static string? Optional(string? value, int max = 160)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Trim().Length > max) throw new ArgumentException($"Massimo {max} caratteri.");
        return value.Trim();
    }
    public static void Dates(DateOnly start, DateOnly? end)
    {
        if (start == default) throw new ArgumentException("Data iniziale obbligatoria.");
        if (end == DateOnly.MinValue) throw new ArgumentException("Data finale non valida.");
        if (end < start) throw new ArgumentException("La fine precede l'inizio.");
    }
    public static decimal Decimal(decimal value, int scale, decimal min, decimal max)
    {
        if (value < min || value > max || decimal.Round(value, scale) != value)
            throw new ArgumentOutOfRangeException(nameof(value), $"Valore tra {min} e {max}, massimo {scale} decimali.");
        return value;
    }
    public static T EnumValue<T>(T value) where T : struct, Enum => Enum.IsDefined(value)
        ? value : throw new ArgumentException("Valore enumerato non valido.");
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
    public string? TaxCode { get; private set; }
    public string Address { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Province { get; private set; } = "";
    public string Country { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public Condominium(Guid tenantId, string code, string name) : base(tenantId)
    { Code = Guard.Text(code); Name = Guard.Text(name); }
    public void Update(string code, string name, string? taxCode, string address, string postalCode, string city, string province, string country, bool isActive)
    {
        Code = Guard.Text(code); Name = Guard.Text(name); TaxCode = Guard.Optional(taxCode, 32)?.ToUpperInvariant();
        Address = Guard.Text(address); PostalCode = Guard.Text(postalCode, 16); City = Guard.Text(city);
        Province = Guard.Text(province, 40); Country = Guard.Text(country, 80); IsActive = isActive;
    }
    public void Deactivate() => IsActive = false;
}
public sealed class Building : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public Building(Guid tenantId, Guid condominiumId, string code) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); }
    public void Update(string code, string name, bool isActive) { Code = Guard.Text(code); Name = Guard.Text(name); IsActive = isActive; }
}
public sealed class Staircase : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public Staircase(Guid tenantId, Guid condominiumId, string code, Guid? buildingId) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); BuildingId = buildingId.HasValue ? Guard.Id(buildingId.Value) : null; }
    public void Update(string code, string name, bool isActive) { Code = Guard.Text(code); Name = Guard.Text(name); IsActive = isActive; }
}
public enum PropertyUnitKind { Apartment, Garage, Cellar, Shop, Other }
public sealed class PropertyUnit : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid? BuildingId { get; private set; }
    public Guid? StaircaseId { get; private set; }
    public string Code { get; private set; }
    public string? Interior { get; private set; }
    public string? Floor { get; private set; }
    public PropertyUnitKind Kind { get; private set; }
    public string? CadastralData { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public PropertyUnit(Guid tenantId, Guid condominiumId, string code, Guid? buildingId = null) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); BuildingId = buildingId.HasValue ? Guard.Id(buildingId.Value) : null; }
    public void Update(string code, Guid? buildingId, Guid? staircaseId, string? interior, string? floor, PropertyUnitKind kind, string? cadastralData, string? notes, bool isActive)
    {
        Code = Guard.Text(code); BuildingId = buildingId.HasValue ? Guard.Id(buildingId.Value) : null;
        StaircaseId = staircaseId.HasValue ? Guard.Id(staircaseId.Value) : null;
        Interior = Guard.Optional(interior); Floor = Guard.Optional(floor); Kind = Guard.EnumValue(kind);
        CadastralData = Guard.Optional(cadastralData, 2000); Notes = Guard.Optional(notes, 4000); IsActive = isActive;
    }
}
public enum PersonKind { Unspecified, NaturalPerson, LegalEntity }
public sealed class Person : TenantEntity
{
    public string Code { get; private set; }
    public string DisplayName { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastNameOrCompany { get; private set; }
    public PersonKind Kind { get; private set; }
    public string? TaxCode { get; private set; }
    public string? VatNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? MailingAddress { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Person(Guid tenantId, string code, string displayName) : base(tenantId)
    { Code = Guard.Text(code); DisplayName = Guard.Text(displayName); }
    public void Update(string code, PersonKind kind, string? firstName, string lastNameOrCompany, string taxCode, string? vatNumber, string? email, string? phone, string? mailingAddress, string? notes, bool isActive)
    {
        if (Guard.EnumValue(kind) == PersonKind.Unspecified) throw new ArgumentException("Specificare persona fisica o giuridica.");
        FirstName = kind == PersonKind.NaturalPerson ? Guard.Text(firstName) : Guard.Optional(firstName);
        LastNameOrCompany = Guard.Text(lastNameOrCompany); Kind = kind; Code = Guard.Text(code);
        DisplayName = Guard.Text(kind == PersonKind.NaturalPerson ? $"{FirstName} {LastNameOrCompany}" : LastNameOrCompany);
        TaxCode = Guard.Text(taxCode, 32).ToUpperInvariant(); VatNumber = Guard.Optional(vatNumber, 32);
        Email = Guard.Optional(email, 254);
        if (Email != null && (!MailAddress.TryCreate(Email, out var parsed) || parsed.Address != Email)) throw new ArgumentException("Email non valida.");
        Phone = Guard.Optional(phone, 60); MailingAddress = Guard.Optional(mailingAddress, 500);
        Notes = Guard.Optional(notes, 4000); IsActive = isActive;
    }
}
public enum UnitPersonRole { Owner, CoOwner, Tenant, Usufructuary, Other }
public sealed class OwnershipPeriod : TenantEntity
{
    public Guid PropertyUnitId { get; private set; }
    public Guid PersonId { get; private set; }
    public UnitPersonRole Role { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal? Fraction { get; private set; }
    public OwnershipPeriod(Guid tenantId, Guid propertyUnitId, Guid personId, DateOnly validFrom, DateOnly? validTo, decimal? fraction, UnitPersonRole role = UnitPersonRole.Owner) : base(tenantId)
    {
        Guard.Dates(validFrom, validTo); Role = Guard.EnumValue(role);
        if (role is UnitPersonRole.Owner or UnitPersonRole.CoOwner)
        {
            if (fraction is null || fraction <= 0) throw new ArgumentOutOfRangeException(nameof(fraction));
            Fraction = Guard.Decimal(fraction.Value, 10, 0.0000000001m, 1);
        }
        else if (fraction != null) throw new ArgumentException("La quota e prevista solo per proprietari e comproprietari.");
        PropertyUnitId = Guard.Id(propertyUnitId); PersonId = Guard.Id(personId); ValidFrom = validFrom; ValidTo = validTo;
    }
    public void End(DateOnly validTo)
    {
        if (ValidTo != null) throw new InvalidOperationException("Associazione gia conclusa: storico immutabile.");
        Guard.Dates(ValidFrom, validTo); ValidTo = validTo;
    }
}
public enum AllocationTableStatus { Draft, Active }
public sealed record AllocationValidation(decimal Total, decimal Denominator, IReadOnlyList<Guid> MissingUnitIds, IReadOnlyList<Guid> UnexpectedUnitIds, bool HasDuplicates)
{
    public bool IsValid => Total == Denominator && MissingUnitIds.Count == 0 && UnexpectedUnitIds.Count == 0 && !HasDuplicates;
}
public sealed class AllocationTable : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; } = "";
    public decimal Denominator { get; private set; } = 1000;
    public DateOnly ValidFrom { get; private set; }
    public AllocationTableStatus Status { get; private set; }
    public bool IsArchived { get; private set; }
    public AllocationTable(Guid tenantId, Guid condominiumId, string code) : base(tenantId)
    { CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); }
    public void EnsureDraft()
    {
        if (Status != AllocationTableStatus.Draft || IsArchived) throw new InvalidOperationException("Tabella attiva o archiviata: creare una nuova versione.");
    }
    public void Update(string code, string name, decimal denominator, DateOnly validFrom)
    {
        EnsureDraft(); Guard.Dates(validFrom, null); Code = Guard.Text(code); Name = Guard.Text(name);
        Denominator = Guard.Decimal(denominator, 6, 0.000001m, 999999999999.999999m); ValidFrom = validFrom;
    }
    public AllocationValidation Validate(IReadOnlyCollection<Guid> units, IReadOnlyCollection<AllocationTableEntry> entries)
    {
        var effective = entries.Where(x => x.AllocationTableId == Id && x.TenantId == TenantId && x.CondominiumId == CondominiumId && x.ValidFrom <= ValidFrom && (x.ValidTo == null || x.ValidTo >= ValidFrom)).ToArray();
        var ids = effective.Select(x => x.PropertyUnitId).ToArray();
        return new(effective.Sum(x => x.Value), Denominator, units.Except(ids).ToArray(), ids.Except(units).ToArray(), ids.Distinct().Count() != ids.Length);
    }
    public void Activate(IReadOnlyCollection<Guid> units, IReadOnlyCollection<AllocationTableEntry> entries)
    {
        EnsureDraft();
        if (units.Count == 0 || !Validate(units, entries).IsValid) throw new InvalidOperationException("Tabella incompleta o totale diverso dal denominatore.");
        Status = AllocationTableStatus.Active;
    }
    public void Archive() { EnsureDraft(); IsArchived = true; }
}
public sealed class AllocationTableEntry : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public Guid AllocationTableId { get; private set; }
    public Guid PropertyUnitId { get; private set; }
    public decimal Value { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public AllocationTableEntry(Guid tenantId, Guid condominiumId, Guid allocationTableId, Guid propertyUnitId, decimal value, DateOnly validFrom, DateOnly? validTo) : base(tenantId)
    {
        Guard.Dates(validFrom, validTo); CondominiumId = Guard.Id(condominiumId); AllocationTableId = Guard.Id(allocationTableId);
        PropertyUnitId = Guard.Id(propertyUnitId); Value = Guard.Decimal(value, 6, 0, 999999999999.999999m); ValidFrom = validFrom; ValidTo = validTo;
    }
    public void SetValue(decimal value) => Value = Guard.Decimal(value, 6, 0, 999999999999.999999m);
}
public enum AccountingPeriodStatus { Draft, Open, Closed }
public sealed class AccountingPeriod : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; } = "";
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public AccountingPeriodStatus Status { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public AccountingPeriod(Guid tenantId, Guid condominiumId, string code, DateOnly startsOn, DateOnly endsOn) : base(tenantId)
    { Guard.Dates(startsOn, endsOn); CondominiumId = Guard.Id(condominiumId); Code = Guard.Text(code); StartsOn = startsOn; EndsOn = endsOn; }
    public bool Overlaps(DateOnly start, DateOnly end) => StartsOn <= end && EndsOn >= start;
    public void Update(string code, string name, DateOnly startsOn, DateOnly endsOn)
    {
        if (Status != AccountingPeriodStatus.Draft) throw new InvalidOperationException("Solo gli esercizi Draft sono modificabili.");
        Guard.Dates(startsOn, endsOn); Code = Guard.Text(code); Name = Guard.Text(name); StartsOn = startsOn; EndsOn = endsOn;
    }
    public void Open()
    {
        if (Status != AccountingPeriodStatus.Draft) throw new InvalidOperationException("Apertura consentita solo da Draft.");
        Status = AccountingPeriodStatus.Open;
    }
    public void Close()
    {
        if (Status != AccountingPeriodStatus.Open) throw new InvalidOperationException("Chiusura consentita solo da Open.");
        Status = AccountingPeriodStatus.Closed; ClosedAt = DateTimeOffset.UtcNow;
    }
}