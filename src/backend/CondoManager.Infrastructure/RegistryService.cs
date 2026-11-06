using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.Infrastructure;

// EF-specific implementation of the application use-case contract.
// Domain owns invariants; this class coordinates scoped persistence and transactions.
public sealed class RegistryService(AppDbContext db, ITenantContext tenant) : IRegistryService
{
    private ICurrentTenant Access => tenant as ICurrentTenant ?? throw new AccessDeniedException();
    private Guid TenantId => Access.RequireRead();
    private async Task<Condominium> Condo(Guid id, CancellationToken ct)
    {
        _ = TenantId;
        return await db.Condominiums.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException();
    }
    private async Task<T> Write<T>(Guid? condominiumId, Func<Task<T>> operation, CancellationToken ct)
    {
        Access.RequireWrite();
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (condominiumId is Guid id)
        {
            var condominium = await Condo(id, ct);
            // Serialize edits of structure, tables, associations and periods for this tenant/condominium.
            if (transaction != null)
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"Id\"={id} AND \"TenantId\"={TenantId} FOR UPDATE", ct);
            if (transaction != null) await db.Entry(condominium).ReloadAsync(ct);
            if (!condominium.IsActive) throw new InvalidOperationException("Condominio inattivo: riattivarlo prima di modificarne i dati.");
        }
        var result = await operation();
        await db.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return result;
    }
    public async Task<IReadOnlyList<CondominiumDto>> Condominiums(CancellationToken ct)
    { _ = TenantId; return (await db.Condominiums.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct)).Select(ToDto).ToArray(); }
    public async Task<CondominiumDto> Condominium(Guid id, CancellationToken ct) => ToDto(await Condo(id, ct));
    public Task<CondominiumDto> SaveCondominium(Guid? id, CondominiumInput input, CancellationToken ct) => Write<CondominiumDto>(null, async () =>
    {
        var row = id.HasValue ? await Condo(id.Value, ct) : new Condominium(TenantId, input.Code, input.Name);
        row.Update(input.Code, input.Name, input.TaxCode, input.Address, input.PostalCode, input.City, input.Province, input.Country, input.IsActive);
        if (!id.HasValue) { db.Add(row); db.AddRange(Account.Defaults(TenantId,row.Id)); }
        await db.SaveChangesAsync(ct);
        return ToDto(row);
    }, ct);
    public async Task DeactivateCondominium(Guid id, CancellationToken ct) => await Write<object?>(null, async () =>
    { (await Condo(id, ct)).Deactivate(); return null; }, ct);
    public async Task<StructureDto> Structure(Guid condominiumId, CancellationToken ct)
    {
        await Condo(condominiumId, ct);
        return new((await db.Buildings.Where(x => x.CondominiumId == condominiumId).OrderBy(x => x.Code).ToListAsync(ct)).Select(ToDto).ToArray(),
            (await db.Staircases.Where(x => x.CondominiumId == condominiumId).OrderBy(x => x.Code).ToListAsync(ct)).Select(ToDto).ToArray(),
            (await db.PropertyUnits.Where(x => x.CondominiumId == condominiumId).OrderBy(x => x.Code).ToListAsync(ct)).Select(ToDto).ToArray());
    }
    public Task<BuildingDto> SaveBuilding(Guid condominiumId, Guid? id, BuildingInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var row = id.HasValue ? await db.Buildings.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException()
            : new Building(TenantId, condominiumId, input.Code);
        if (!input.IsActive && (await db.PropertyUnits.AnyAsync(x => x.BuildingId == row.Id && x.IsActive, ct) ||
            await db.Staircases.AnyAsync(x => x.BuildingId == row.Id && x.IsActive, ct)))
            throw new InvalidOperationException("Disattivare prima le scale e le unita collegate.");
        row.Update(input.Code, input.Name, input.IsActive);
        if (!id.HasValue) db.Add(row);
        return ToDto(row);
    }, ct);
    public Task<StaircaseDto> SaveStaircase(Guid condominiumId, Guid? id, StaircaseInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        if (input.BuildingId.HasValue && !await db.Buildings.AnyAsync(x => x.Id == input.BuildingId && x.CondominiumId == condominiumId && x.IsActive, ct))
            throw new NotFoundException();
        var row = id.HasValue ? await db.Staircases.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException()
            : new Staircase(TenantId, condominiumId, input.Code, input.BuildingId);
        if (row.BuildingId != input.BuildingId) throw new InvalidOperationException("L'edificio di una scala esistente non puo essere cambiato.");
        if (!input.IsActive && await db.PropertyUnits.AnyAsync(x => x.StaircaseId == row.Id && x.IsActive, ct))
            throw new InvalidOperationException("Disattivare prima le unita collegate.");
        row.Update(input.Code, input.Name, input.IsActive);
        if (!id.HasValue) db.Add(row);
        return ToDto(row);
    }, ct);
    public Task<PropertyUnitDto> SaveUnit(Guid condominiumId, Guid? id, PropertyUnitInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var buildingId = input.BuildingId;
        if (input.StaircaseId.HasValue)
        {
            var staircase = await db.Staircases.SingleOrDefaultAsync(x => x.Id == input.StaircaseId && x.CondominiumId == condominiumId && x.IsActive, ct) ?? throw new NotFoundException();
            if (buildingId != null && staircase.BuildingId != null && buildingId != staircase.BuildingId) throw new ArgumentException("Scala ed edificio non corrispondono.");
            buildingId ??= staircase.BuildingId;
        }
        if (buildingId.HasValue && !await db.Buildings.AnyAsync(x => x.Id == buildingId && x.CondominiumId == condominiumId && x.IsActive, ct)) throw new NotFoundException();
        var row = id.HasValue ? await db.PropertyUnits.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException()
            : new PropertyUnit(TenantId, condominiumId, input.Code);
        row.Update(input.Code, buildingId, input.StaircaseId, input.Interior, input.Floor, input.Kind, input.CadastralData, input.Notes, input.IsActive);
        if (!id.HasValue) db.Add(row);
        return ToDto(row);
    }, ct);
    public async Task<IReadOnlyList<PersonDto>> People(CancellationToken ct)
    { _ = TenantId; return (await db.People.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct)).Select(ToDto).ToArray(); }
    public Task<PersonDto> SavePerson(Guid? id, PersonInput input, CancellationToken ct) => Write<PersonDto>(null, async () =>
    {
        var row = id.HasValue ? await db.People.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException()
            : new Person(TenantId, input.Code, input.LastNameOrCompany);
        row.Update(input.Code, input.Kind, input.FirstName, input.LastNameOrCompany, input.TaxCode, input.VatNumber, input.Email, input.Phone, input.MailingAddress, input.Notes, input.IsActive);
        if (!id.HasValue) db.Add(row);
        return ToDto(row);
    }, ct);
    public async Task<IReadOnlyList<AssociationDto>> Associations(Guid condominiumId, CancellationToken ct)
    {
        await Condo(condominiumId, ct);
        return await (from link in db.OwnershipPeriods
            join unit in db.PropertyUnits on link.PropertyUnitId equals unit.Id
            join person in db.People on link.PersonId equals person.Id
            where unit.CondominiumId == condominiumId
            orderby link.ValidFrom descending
            select new AssociationDto(link.Id, unit.Id, person.Id, person.DisplayName, unit.Code, link.Role, link.ValidFrom, link.ValidTo, link.Fraction)).ToListAsync(ct);
    }
    public Task<AssociationDto> Associate(Guid condominiumId, Guid unitId, AssociationInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var unit = await db.PropertyUnits.SingleOrDefaultAsync(x => x.Id == unitId && x.CondominiumId == condominiumId && x.IsActive, ct) ?? throw new NotFoundException();
        var person = await db.People.SingleOrDefaultAsync(x => x.Id == input.PersonId && x.IsActive, ct) ?? throw new NotFoundException();
        var row = new OwnershipPeriod(TenantId, unitId, person.Id, input.ValidFrom, input.ValidTo, input.Fraction, input.Role);
        if (await db.OwnershipPeriods.AnyAsync(x => x.PropertyUnitId == unitId && x.PersonId == person.Id && x.Role == input.Role &&
            (x.ValidTo == null || x.ValidTo >= input.ValidFrom) && (input.ValidTo == null || x.ValidFrom <= input.ValidTo), ct))
            throw new InvalidOperationException("Associazione sovrapposta per soggetto, unita e ruolo.");
        db.Add(row);
        return new AssociationDto(row.Id, unitId, person.Id, person.DisplayName, unit.Code, row.Role, row.ValidFrom, row.ValidTo, row.Fraction);
    }, ct);
    public async Task EndAssociation(Guid condominiumId, Guid id, DateOnly validTo, CancellationToken ct) => await Write<object?>(condominiumId, async () =>
    {
        var row = await (from link in db.OwnershipPeriods join unit in db.PropertyUnits on link.PropertyUnitId equals unit.Id
            where link.Id == id && unit.CondominiumId == condominiumId select link).SingleOrDefaultAsync(ct) ?? throw new NotFoundException();
        row.End(validTo); return null;
    }, ct);
    private async Task<AllocationTable> Table(Guid condominiumId, Guid id, CancellationToken ct) =>
        await db.AllocationTables.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException();
    private async Task<AllocationTableDto> TableDto(AllocationTable row, CancellationToken ct)
    {
        var entries = await db.AllocationTableEntries.Where(x => x.AllocationTableId == row.Id).ToListAsync(ct);
        var units = await db.PropertyUnits.Where(x => x.CondominiumId == row.CondominiumId && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        return new(row.Id, row.CondominiumId, row.Code, row.Name, row.Denominator, row.ValidFrom, row.Status, row.IsArchived,
            row.Validate(units, entries), entries.Where(x => x.ValidFrom <= row.ValidFrom && (x.ValidTo == null || x.ValidTo >= row.ValidFrom))
                .Select(x => new AllocationEntryDto(x.Id, x.PropertyUnitId, x.Value, x.ValidFrom, x.ValidTo)).ToArray());
    }
    public async Task<IReadOnlyList<AllocationTableDto>> Tables(Guid condominiumId, CancellationToken ct)
    {
        await Condo(condominiumId, ct);
        var result = new List<AllocationTableDto>();
        foreach (var table in await db.AllocationTables.Where(x => x.CondominiumId == condominiumId).OrderBy(x => x.Code).ToListAsync(ct))
            result.Add(await TableDto(table, ct));
        return result;
    }
    public Task<AllocationTableDto> SaveTable(Guid condominiumId, Guid? id, AllocationTableInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var row = id.HasValue ? await Table(condominiumId, id.Value, ct) : new AllocationTable(TenantId, condominiumId, input.Code);
        if (id.HasValue && row.ValidFrom != input.ValidFrom && await db.AllocationTableEntries.AnyAsync(x => x.AllocationTableId == row.Id, ct))
            throw new InvalidOperationException("La decorrenza non puo cambiare dopo l'inserimento delle quote.");
        row.Update(input.Code, input.Name, input.Denominator, input.ValidFrom);
        if (!id.HasValue) db.Add(row);
        await db.SaveChangesAsync(ct);
        return await TableDto(row, ct);
    }, ct);
    public Task<AllocationTableDto> SetEntry(Guid condominiumId, Guid tableId, Guid unitId, decimal value, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var table = await Table(condominiumId, tableId, ct); table.EnsureDraft();
        if (!await db.PropertyUnits.AnyAsync(x => x.Id == unitId && x.CondominiumId == condominiumId && x.IsActive, ct)) throw new NotFoundException();
        var entries = await db.AllocationTableEntries.Where(x => x.AllocationTableId == tableId && x.PropertyUnitId == unitId &&
            x.ValidFrom <= table.ValidFrom && (x.ValidTo == null || x.ValidTo >= table.ValidFrom)).ToListAsync(ct);
        if (entries.Count > 1) throw new InvalidOperationException("Quote storiche sovrapposte: correggere i dati prima di procedere.");
        if (entries.Count == 1) entries[0].SetValue(value);
        else db.Add(new AllocationTableEntry(TenantId, condominiumId, table.Id, unitId, value, table.ValidFrom, null));
        await db.SaveChangesAsync(ct);
        return await TableDto(table, ct);
    }, ct);
    public Task<AllocationTableDto> ActivateTable(Guid condominiumId, Guid id, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var table = await Table(condominiumId, id, ct);
        var units = await db.PropertyUnits.Where(x => x.CondominiumId == condominiumId && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        var entries = await db.AllocationTableEntries.Where(x => x.AllocationTableId == id).ToListAsync(ct);
        table.Activate(units, entries);
        await db.SaveChangesAsync(ct);
        return await TableDto(table, ct);
    }, ct);
    public async Task ArchiveTable(Guid condominiumId, Guid id, CancellationToken ct) => await Write<object?>(condominiumId, async () =>
    { (await Table(condominiumId, id, ct)).Archive(); return null; }, ct);
    public async Task<IReadOnlyList<AccountingPeriodDto>> Periods(Guid condominiumId, CancellationToken ct)
    { await Condo(condominiumId, ct); return (await db.AccountingPeriods.Where(x => x.CondominiumId == condominiumId).OrderByDescending(x => x.StartsOn).ToListAsync(ct)).Select(ToDto).ToArray(); }
    public Task<AccountingPeriodDto> SavePeriod(Guid condominiumId, Guid? id, AccountingPeriodInput input, CancellationToken ct) => Write(condominiumId, async () =>
    {
        if (await db.AccountingPeriods.AnyAsync(x => x.CondominiumId == condominiumId && x.Id != id && x.StartsOn <= input.EndsOn && x.EndsOn >= input.StartsOn, ct))
            throw new InvalidOperationException("Esercizio sovrapposto a un intervallo esistente.");
        var row = id.HasValue ? await db.AccountingPeriods.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException()
            : new AccountingPeriod(TenantId, condominiumId, input.Code, input.StartsOn, input.EndsOn);
        row.Update(input.Code, input.Name, input.StartsOn, input.EndsOn);
        if (!id.HasValue) db.Add(row);
        return ToDto(row);
    }, ct);
    public Task<AccountingPeriodDto> TransitionPeriod(Guid condominiumId, Guid id, bool close, CancellationToken ct) => Write(condominiumId, async () =>
    {
        var row = await db.AccountingPeriods.SingleOrDefaultAsync(x => x.Id == id && x.CondominiumId == condominiumId, ct) ?? throw new NotFoundException();
        if (close) row.Close(); else row.Open();
        return ToDto(row);
    }, ct);

    private static CondominiumDto ToDto(Condominium x) => new(x.Id, x.Code, x.Name, x.TaxCode, x.Address, x.PostalCode, x.City, x.Province, x.Country, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static BuildingDto ToDto(Building x) => new(x.Id, x.CondominiumId, x.Code, x.Name, x.IsActive);
    private static StaircaseDto ToDto(Staircase x) => new(x.Id, x.CondominiumId, x.BuildingId, x.Code, x.Name, x.IsActive);
    private static PropertyUnitDto ToDto(PropertyUnit x) => new(x.Id, x.CondominiumId, x.Code, x.BuildingId, x.StaircaseId, x.Interior, x.Floor, x.Kind, x.CadastralData, x.Notes, x.IsActive);
    private static PersonDto ToDto(Person x) => new(x.Id, x.Code, x.Kind, x.DisplayName, x.FirstName, x.LastNameOrCompany, x.TaxCode, x.VatNumber, x.Email, x.Phone, x.MailingAddress, x.Notes, x.IsActive);
    private static AccountingPeriodDto ToDto(AccountingPeriod x) => new(x.Id, x.CondominiumId, x.Code, x.Name, x.StartsOn, x.EndsOn, x.Status, x.CreatedAt, x.ClosedAt);
}