using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AllocationService(AppDbContext db,ICurrentTenant current,IAllocationEngine engine) : IAllocationService
{
    private Guid TenantId=>current.RequireRead();
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web) { NumberHandling=JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString, Converters={new JsonStringEnumConverter()} };
    private static string Serialize<T>(T value)=>JsonSerializer.Serialize(value,Json);
    private static T Deserialize<T>(string value)=>JsonSerializer.Deserialize<T>(value,Json)??throw new InvalidOperationException("Snapshot non valido.");
    private static string Hash<T>(T value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,Json)));
    private async Task Condo(Guid condo,CancellationToken ct) { _=TenantId;if(!await db.Condominiums.AnyAsync(x=>x.Id==condo,ct)) throw new NotFoundException(); }
    private async Task<T> Write<T>(Guid condo,Func<Task<T>> action,CancellationToken ct)
    {
        current.RequireWrite();await Condo(condo,ct);await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"Id\"={condo} AND \"TenantId\"={TenantId} FOR UPDATE",ct);
        if(!await db.Condominiums.AnyAsync(x=>x.Id==condo && x.IsActive,ct)) throw new InvalidOperationException("Condominio inattivo.");
        var result=await action();await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return result;
    }
    private void Audit(string name,Guid id)=>db.SecurityAudits.Add(new(){Id=Guid.NewGuid(),TenantId=TenantId,ActorUserId=current.UserId,SubjectId=id,Event=name,Timestamp=DateTimeOffset.UtcNow});
    private async Task<AccountingPeriod> Period(Guid condo,Guid id,CancellationToken ct,bool open=false)
    { var p=await db.AccountingPeriods.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct)??throw new NotFoundException();if(open && p.Status!=AccountingPeriodStatus.Open) throw new InvalidOperationException("Esercizio chiuso o non aperto.");return p; }
    private async Task<AllocationRule> Rule(Guid condo,Guid id,CancellationToken ct)=>await db.AllocationRules.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct)??throw new NotFoundException();
    private Task<List<AllocationRuleComponent>> Components(Guid id,CancellationToken ct)=>db.AllocationRuleComponents.Where(x=>x.RuleId==id).OrderBy(x=>x.Position).ToListAsync(ct);
    private async Task<RuleDto> RuleDtoFor(AllocationRule row,CancellationToken ct)=>new(row.Id,row.Name,row.Description,row.Type,row.Status,row.Version,row.PreviousVersionId,(await Components(row.Id,ct)).Select(c=>new RuleComponentDto(c.Position,c.Criterion,c.Percentage,c.TableId,c.GroupId,c.ConsumptionType,c.UnitOfMeasure,c.NormalizeAdmitted)).ToArray());
    public async Task<IReadOnlyList<RuleDto>> Rules(Guid condo,CancellationToken ct) { await Condo(condo,ct);var result=new List<RuleDto>();foreach(var row in await db.AllocationRules.Where(x=>x.CondominiumId==condo).OrderBy(x=>x.Name).ThenBy(x=>x.Version).ToListAsync(ct)) result.Add(await RuleDtoFor(row,ct));return result; }
    public Task<RuleDto> SaveRule(Guid condo,Guid? id,RuleInput input,CancellationToken ct)=>Write(condo,async()=> {
        if(input.Components==null || input.Components.Count is <1 or >20) throw new ArgumentException("Da 1 a 20 componenti richiesti.");
        var existing=id.HasValue?await Rule(condo,id.Value,ct):null;
        AllocationRule row;
        if(existing?.Status==AllocationRuleStatus.Active)
        {
            if(await db.AllocationRules.AnyAsync(x=>x.PreviousVersionId==existing.Id,ct)) throw new InvalidOperationException("Una versione successiva esiste gia: modificarne la bozza.");
            row=new(TenantId,condo,input.Name,input.Type,existing.Version+1,existing.Id);db.Add(row);
        }
        else { row=existing??new(TenantId,condo,input.Name,input.Type);if(existing==null) db.Add(row);else db.RemoveRange(await Components(row.Id,ct)); }
        row.Update(input.Name,input.Description,input.Type);var components=input.Components.Select((c,i)=>new AllocationRuleComponent(TenantId,condo,row.Id,i,c.Criterion,c.Percentage,c.TableId,c.GroupId,c.ConsumptionType,c.UnitOfMeasure,c.NormalizeAdmitted)).ToArray();
        row.Validate(components);foreach(var c in components) await ValidateComponentReferences(condo,c,ct,false);
        // Flush deletions before replacing positions protected by a unique index.
        await db.SaveChangesAsync(ct);db.AddRange(components);Audit("Allocation.RuleDraftSaved",row.Id);await db.SaveChangesAsync(ct);return await RuleDtoFor(row,ct);
    },ct);
    private async Task ValidateComponentReferences(Guid condo,AllocationRuleComponent component,CancellationToken ct,bool active)
    {
        if(component.TableId.HasValue) { var t=await db.AllocationTables.SingleOrDefaultAsync(x=>x.Id==component.TableId && x.CondominiumId==condo,ct)??throw new NotFoundException();if(active && (t.Status!=AllocationTableStatus.Active || t.IsArchived)) throw new InvalidOperationException("Tabella millesimale non attiva."); }
        if(component.GroupId.HasValue && !await db.AllocationGroups.AnyAsync(x=>x.Id==component.GroupId && x.CondominiumId==condo && x.IsActive,ct)) throw new NotFoundException();
    }
    public Task<RuleDto> ActivateRule(Guid condo,Guid id,CancellationToken ct)=>Write(condo,async()=> {var row=await Rule(condo,id,ct);var components=await Components(id,ct);foreach(var c in components) await ValidateComponentReferences(condo,c,ct,true);row.Activate(components);Audit("Allocation.RuleActivated",id);await db.SaveChangesAsync(ct);return await RuleDtoFor(row,ct);},ct);
    public async Task<IReadOnlyList<GroupDto>> Groups(Guid condo,CancellationToken ct)
    {await Condo(condo,ct);var rows=await db.AllocationGroups.Where(x=>x.CondominiumId==condo).OrderBy(x=>x.Name).ToListAsync(ct);var members=await db.AllocationGroupMembers.Where(x=>x.CondominiumId==condo).ToListAsync(ct);return rows.Select(r=>new GroupDto(r.Id,r.Name,r.IsActive,members.Where(m=>m.GroupId==r.Id).OrderBy(m=>m.PropertyUnitId).Select(m=>m.PropertyUnitId).ToArray())).ToArray();}
    public Task<GroupDto> SaveGroup(Guid condo,Guid? id,GroupInput input,CancellationToken ct)=>Write(condo,async()=> {
        if(input.UnitIds==null || input.UnitIds.Count==0 || input.UnitIds.Distinct().Count()!=input.UnitIds.Count) throw new ArgumentException("Il gruppo richiede unita univoche, almeno una.");
        if(await db.PropertyUnits.CountAsync(x=>x.CondominiumId==condo && input.UnitIds.Contains(x.Id) && x.IsActive,ct)!=input.UnitIds.Count) throw new NotFoundException();
        var row=id.HasValue?await db.AllocationGroups.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct)??throw new NotFoundException():new AllocationGroup(TenantId,condo,input.Name);
        row.Update(input.Name,input.IsActive);if(!id.HasValue) db.Add(row);else db.RemoveRange(await db.AllocationGroupMembers.Where(x=>x.GroupId==row.Id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);db.AddRange(input.UnitIds.Select(u=>new AllocationGroupMember(TenantId,condo,row.Id,u)));Audit("Allocation.GroupSaved",row.Id);return new GroupDto(row.Id,row.Name,row.IsActive,input.UnitIds.Order().ToArray());
    },ct);
    public async Task<IReadOnlyList<ReadingDto>> Readings(Guid condo,Guid? period,CancellationToken ct)
    {await Condo(condo,ct);var query=db.ConsumptionReadings.Where(x=>x.CondominiumId==condo);if(period.HasValue) query=query.Where(x=>x.AccountingPeriodId==period);var rows=await query.OrderBy(x=>x.Date).ThenBy(x=>x.Id).ToListAsync(ct);var replaced=rows.Where(x=>x.ReplacesReadingId!=null).Select(x=>x.ReplacesReadingId).ToHashSet();return rows.Select(x=>ReadingDtoFor(x,replaced.Contains(x.Id))).ToArray();}
    private static ReadingDto ReadingDtoFor(ConsumptionReading x,bool replaced)=>new(x.Id,x.PropertyUnitId,x.AccountingPeriodId,x.ConsumptionType,x.MeterCode,x.UnitOfMeasure,x.Date,x.Boundary,x.Value,x.IsEstimated,x.Reason,x.ReplacesReadingId,replaced);
    public Task<ReadingDto> AddReading(Guid condo,ReadingInput input,CancellationToken ct)=>Write(condo,async()=> {
        var period=await Period(condo,input.AccountingPeriodId,ct,true);if(input.Date<period.StartsOn || input.Date>period.EndsOn) throw new ArgumentException("Data lettura fuori esercizio.");
        if(!await db.PropertyUnits.AnyAsync(x=>x.Id==input.PropertyUnitId && x.CondominiumId==condo && x.IsActive,ct)) throw new NotFoundException();
        var row=new ConsumptionReading(TenantId,condo,input.PropertyUnitId,input.AccountingPeriodId,input.ConsumptionType,input.MeterCode,input.UnitOfMeasure,input.Date,input.Boundary,input.Value,input.IsEstimated,input.Reason,input.ReplacesReadingId);
        var readings=await db.ConsumptionReadings.Where(x=>x.CondominiumId==condo && x.AccountingPeriodId==period.Id && x.PropertyUnitId==row.PropertyUnitId && x.ConsumptionType==row.ConsumptionType && x.UnitOfMeasure==row.UnitOfMeasure && x.MeterCode==row.MeterCode).ToListAsync(ct);
        var superseded=readings.Where(x=>x.ReplacesReadingId!=null).Select(x=>x.ReplacesReadingId).ToHashSet();var active=readings.Where(x=>!superseded.Contains(x.Id)).ToArray();
        if(row.ReplacesReadingId.HasValue && !active.Any(x=>x.Id==row.ReplacesReadingId && x.Boundary==row.Boundary)) throw new InvalidOperationException("Rettificare solo l'ultima lettura dello stesso contatore, unita, tipo ed esercizio.");
        if(active.Any(x=>x.Boundary==row.Boundary && x.Id!=row.ReplacesReadingId)) throw new InvalidOperationException("Lettura gia presente: usare una rettifica tracciata.");
        var opposite=active.SingleOrDefault(x=>x.Boundary!=row.Boundary);
        if(opposite!=null && (row.Boundary==ReadingBoundary.Initial ? row.Date>=opposite.Date || row.Value>opposite.Value : row.Date<=opposite.Date || row.Value<opposite.Value)) throw new ArgumentException("Letture incoerenti: date o consumo negativo. Per sostituzione usare un nuovo codice contatore.");
        db.Add(row);Audit(row.ReplacesReadingId.HasValue?"Allocation.ReadingCorrected":"Allocation.ReadingAdded",row.Id);return ReadingDtoFor(row,false);
    },ct);
    public async Task<IReadOnlyList<UnitConsumptionDto>> Consumptions(Guid condo,Guid period,string type,string unitOfMeasure,DateOnly from,DateOnly to,CancellationToken ct)
    {
        await Condo(condo,ct);var p=await Period(condo,period,ct);ValidateCompetence(p,from,to);type=Guard.Text(type).ToUpperInvariant();unitOfMeasure=Guard.Text(unitOfMeasure).ToUpperInvariant();
        var units=await db.PropertyUnits.Where(x=>x.CondominiumId==condo && x.IsActive).OrderBy(x=>x.Code).ToListAsync(ct);var readings=await db.ConsumptionReadings.Where(x=>x.CondominiumId==condo && x.AccountingPeriodId==period && x.ConsumptionType==type && x.UnitOfMeasure==unitOfMeasure).ToListAsync(ct);
        return units.Select(u=>new UnitConsumptionDto(u.Id,u.Code,ConsumptionCalculator.Calculate(readings.Where(r=>r.PropertyUnitId==u.Id).ToArray(),from,to))).ToArray();
    }
    public async Task<IReadOnlyList<AssignmentDto>> Assignments(Guid condo,CancellationToken ct) {await Condo(condo,ct);return await db.AllocationAssignments.Where(x=>x.CondominiumId==condo).OrderBy(x=>x.Id).Select(x=>new AssignmentDto(x.Id,x.AccountId,x.ExpenseDocumentId,x.RuleId)).ToListAsync(ct);}
    public Task<AssignmentDto> SaveAssignment(Guid condo,AssignmentInput input,CancellationToken ct)=>Write(condo,async()=> {
        var rule=await Rule(condo,input.RuleId,ct);if(rule.Status!=AllocationRuleStatus.Active) throw new InvalidOperationException("Associare una regola attiva.");
        if(input.AccountId.HasValue && !await db.Accounts.AnyAsync(x=>x.Id==input.AccountId && x.CondominiumId==condo && x.Type==AccountType.Expense,ct)) throw new NotFoundException();
        if(input.ExpenseDocumentId.HasValue && !await db.ExpenseDocuments.AnyAsync(x=>x.Id==input.ExpenseDocumentId && x.CondominiumId==condo && !x.IsCancelled,ct)) throw new NotFoundException();
        var row=await db.AllocationAssignments.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.AccountId==input.AccountId && x.ExpenseDocumentId==input.ExpenseDocumentId,ct);
        if(row==null) {row=new AllocationAssignment(TenantId,condo,input.AccountId,input.ExpenseDocumentId,input.RuleId);db.Add(row);}else row.SetRule(input.RuleId);Audit("Allocation.AssignmentSaved",row.Id);return new AssignmentDto(row.Id,row.AccountId,row.ExpenseDocumentId,row.RuleId);
    },ct);
    private static void ValidateCompetence(AccountingPeriod p,DateOnly from,DateOnly to) {Guard.Dates(from,to);if(from<p.StartsOn || to>p.EndsOn) throw new ArgumentException("Competenza fuori dall'esercizio selezionato.");}
}
