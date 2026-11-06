using CondoManager.Domain;

namespace CondoManager.Application;

public sealed class NotFoundException() : Exception("Risorsa non trovata nello studio corrente.");
public sealed class TenantUnavailableException() : Exception("Tenant non disponibile.");
public record CondominiumInput(string Code, string Name, string? TaxCode, string Address, string PostalCode, string City, string Province, string Country, bool IsActive = true);
public record CondominiumDto(Guid Id, string Code, string Name, string? TaxCode, string Address, string PostalCode, string City, string Province, string Country, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record BuildingInput(string Code, string Name, bool IsActive = true);
public record BuildingDto(Guid Id, Guid CondominiumId, string Code, string Name, bool IsActive);
public record StaircaseInput(string Code, string Name, Guid? BuildingId, bool IsActive = true);
public record StaircaseDto(Guid Id, Guid CondominiumId, Guid? BuildingId, string Code, string Name, bool IsActive);
public record PropertyUnitInput(string Code, Guid? BuildingId, Guid? StaircaseId, string? Interior, string? Floor, PropertyUnitKind Kind, string? CadastralData, string? Notes, bool IsActive = true);
public record PropertyUnitDto(Guid Id, Guid CondominiumId, string Code, Guid? BuildingId, Guid? StaircaseId, string? Interior, string? Floor, PropertyUnitKind Kind, string? CadastralData, string? Notes, bool IsActive);
public record StructureDto(IReadOnlyList<BuildingDto> Buildings, IReadOnlyList<StaircaseDto> Staircases, IReadOnlyList<PropertyUnitDto> Units);
public record PersonInput(string Code, PersonKind Kind, string? FirstName, string LastNameOrCompany, string TaxCode, string? VatNumber, string? Email, string? Phone, string? MailingAddress, string? Notes, bool IsActive = true);
public record PersonDto(Guid Id, string Code, PersonKind Kind, string DisplayName, string? FirstName, string? LastNameOrCompany, string? TaxCode, string? VatNumber, string? Email, string? Phone, string? MailingAddress, string? Notes, bool IsActive);
public record AssociationInput(Guid PersonId, UnitPersonRole Role, DateOnly ValidFrom, DateOnly? ValidTo, decimal? Fraction);
public record EndAssociationInput(DateOnly ValidTo);
public record AssociationDto(Guid Id, Guid PropertyUnitId, Guid PersonId, string PersonName, string UnitCode, UnitPersonRole Role, DateOnly ValidFrom, DateOnly? ValidTo, decimal? Fraction);
public record AllocationTableInput(string Code, string Name, decimal Denominator, DateOnly ValidFrom);
public record AllocationEntryInput(decimal Value);
public record AllocationEntryDto(Guid Id, Guid PropertyUnitId, decimal Value, DateOnly ValidFrom, DateOnly? ValidTo);
public record AllocationTableDto(Guid Id, Guid CondominiumId, string Code, string Name, decimal Denominator, DateOnly ValidFrom, AllocationTableStatus Status, bool IsArchived, AllocationValidation Validation, IReadOnlyList<AllocationEntryDto> Entries);
public record AccountingPeriodInput(string Code, string Name, DateOnly StartsOn, DateOnly EndsOn);
public record AccountingPeriodDto(Guid Id, Guid CondominiumId, string Code, string Name, DateOnly StartsOn, DateOnly EndsOn, AccountingPeriodStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt);

public interface IRegistryService
{
    Task<IReadOnlyList<CondominiumDto>> Condominiums(CancellationToken ct);
    Task<CondominiumDto> Condominium(Guid id, CancellationToken ct);
    Task<CondominiumDto> SaveCondominium(Guid? id, CondominiumInput input, CancellationToken ct);
    Task DeactivateCondominium(Guid id, CancellationToken ct);
    Task<StructureDto> Structure(Guid condominiumId, CancellationToken ct);
    Task<BuildingDto> SaveBuilding(Guid condominiumId, Guid? id, BuildingInput input, CancellationToken ct);
    Task<StaircaseDto> SaveStaircase(Guid condominiumId, Guid? id, StaircaseInput input, CancellationToken ct);
    Task<PropertyUnitDto> SaveUnit(Guid condominiumId, Guid? id, PropertyUnitInput input, CancellationToken ct);
    Task<IReadOnlyList<PersonDto>> People(CancellationToken ct);
    Task<PersonDto> SavePerson(Guid? id, PersonInput input, CancellationToken ct);
    Task<IReadOnlyList<AssociationDto>> Associations(Guid condominiumId, CancellationToken ct);
    Task<AssociationDto> Associate(Guid condominiumId, Guid unitId, AssociationInput input, CancellationToken ct);
    Task EndAssociation(Guid condominiumId, Guid id, DateOnly validTo, CancellationToken ct);
    Task<IReadOnlyList<AllocationTableDto>> Tables(Guid condominiumId, CancellationToken ct);
    Task<AllocationTableDto> SaveTable(Guid condominiumId, Guid? id, AllocationTableInput input, CancellationToken ct);
    Task<AllocationTableDto> SetEntry(Guid condominiumId, Guid tableId, Guid unitId, decimal value, CancellationToken ct);
    Task<AllocationTableDto> ActivateTable(Guid condominiumId, Guid id, CancellationToken ct);
    Task ArchiveTable(Guid condominiumId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<AccountingPeriodDto>> Periods(Guid condominiumId, CancellationToken ct);
    Task<AccountingPeriodDto> SavePeriod(Guid condominiumId, Guid? id, AccountingPeriodInput input, CancellationToken ct);
    Task<AccountingPeriodDto> TransitionPeriod(Guid condominiumId, Guid id, bool close, CancellationToken ct);
}