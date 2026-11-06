using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AllocationService
{
    public async Task<AllocationPreviewDto> Preview(Guid condo,AllocationPreviewInput input,CancellationToken ct)
    {
        // A read-only, repeatable snapshot. No previews, journals or balances are persisted.
        current.RequireRead();await Condo(condo,ct);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);
        var result=await BuildPreview(condo,input,ct);await tx.CommitAsync(ct);return result;
    }
    private async Task<AllocationPreviewDto> BuildPreview(Guid condo,AllocationPreviewInput input,CancellationToken ct,bool lockSources=false)
    {
        var period=await Period(condo,input.AccountingPeriodId,ct,true);ValidateCompetence(period,input.CompetenceFrom,input.CompetenceTo);
        if(input.Sources==null || input.Sources.Count is <1 or >100) throw new ArgumentException("Selezionare da 1 a 100 quote di spesa.");
        var total=Money.Validate(input.Sources.Sum(x=>Money.Positive(x.Amount)));if(total==0) throw new ArgumentException("Importo non valido.");
        AllocationRun? previous=null;
        if(input.PreviousRunId.HasValue)
        {
            previous=await db.AllocationRuns.SingleOrDefaultAsync(x=>x.Id==input.PreviousRunId && x.CondominiumId==condo && x.AccountingPeriodId==period.Id,ct)??throw new NotFoundException();
            if(await db.AllocationRuns.AnyAsync(x=>x.PreviousRunId==previous.Id,ct)) throw new InvalidOperationException("Riparto gia sostituito: utilizzare l'ultima versione.");
            var old=await db.AllocationRunSources.Where(x=>x.RunId==previous.Id).ToListAsync(ct);
            var expected=old.GroupBy(x=>x.ExpenseDocumentId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount));var actual=input.Sources.GroupBy(x=>x.ExpenseDocumentId).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Amount));
            if(expected.Count!=actual.Count || expected.Any(x=>!actual.TryGetValue(x.Key,out var a) || a!=x.Value)) throw new ArgumentException("Una sostituzione deve mantenere documenti e importi complessivi originali.");
        }
        var units=await db.PropertyUnits.Where(x=>x.CondominiumId==condo && x.IsActive).OrderBy(x=>x.Code).ThenBy(x=>x.Id).ToListAsync(ct);
        if(units.Count==0 || units.Count>2000) throw new InvalidOperationException("Richieste da 1 a 2000 unita attive.");
        var assignments=await db.AllocationAssignments.Where(x=>x.CondominiumId==condo).ToListAsync(ct);
        var sources=new List<ResolvedAllocationSource>();var recipientCount=0;
        foreach(var (source,position) in input.Sources.Select((s,i)=>(s,i)))
        {
            var expense=await db.ExpenseDocuments.SingleOrDefaultAsync(x=>x.Id==source.ExpenseDocumentId && x.CondominiumId==condo,ct)??throw new NotFoundException();
            if(expense.AccountingPeriodId!=period.Id) throw new ArgumentException("Documento appartenente a un altro esercizio.");
            if(!expense.IsRegistered || expense.IsCancelled) throw new InvalidOperationException("Ripartire solo documenti contabilizzati e non annullati.");
            var allocated=await db.AllocationRunSources.Where(x=>x.ExpenseDocumentId==expense.Id && x.RunId!=input.PreviousRunId && !db.AllocationRuns.Any(r=>r.PreviousRunId==x.RunId)).SumAsync(x=>x.Amount,ct);
            var requested=input.Sources.Where(x=>x.ExpenseDocumentId==expense.Id).Sum(x=>x.Amount);
            if(allocated+requested>expense.GrossAmount) throw new InvalidOperationException("Spesa gia completamente ripartita o quota superiore al residuo da ripartire.");
            var ruleId=source.RuleId??assignments.SingleOrDefault(x=>x.ExpenseDocumentId==expense.Id)?.RuleId??assignments.SingleOrDefault(x=>x.AccountId==expense.AccountId)?.RuleId??throw new ArgumentException("Regola assente: configurare categoria, documento oppure quota.");
            var (rule,resolved)=await ResolveCriteria(condo,period,ruleId,input.CompetenceFrom,input.CompetenceTo,units,source.ExcludedUnitIds,source.DirectCharges,lockSources,ct);
            recipientCount+=resolved.Sum(c=>c.Recipients.Count);if(recipientCount>20000) throw new ArgumentException("Massimo 20000 righe per calcolo.");
            sources.Add(new(position,TenantId,condo,expense.Id,expense.DocumentNumber,source.Amount,allocated,rule.Id,rule.Name,rule.Version,resolved));
        }
        var snapshot=new AllocationCalculationInput(TenantId,condo,period.Id,input.CompetenceFrom,input.CompetenceTo,sources);
        var result=engine.Calculate(snapshot);var fingerprint=Hash(new{Engine=AllocationEngine.Version,Actor=current.UserId,Previous=previous?.Id,Snapshot=snapshot});return new(fingerprint,snapshot,result);
    }
    public Task<AllocationRunDto> Confirm(Guid condo,AllocationConfirmInput input,CancellationToken ct)=>Write(condo,async()=> {
        if(input.Input==null) throw new ArgumentException("Input della conferma obbligatorio.");
        Guard.Id(input.RequestId);var requestHash=Hash(new{input.PreviewHash,input.Input});
        var existing=await db.AllocationRuns.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.RequestId==input.RequestId,ct);
        if(existing!=null) {if(existing.RequestHash!=requestHash) throw new InvalidOperationException("Chiave di conferma gia utilizzata con dati diversi.");return await RunDtoFor(existing,ct);}
        if(input.Input.PreviousRunId.HasValue) current.RequireAdmin();
        var preview=await BuildPreview(condo,input.Input,ct,true);
        if(!string.Equals(preview.PreviewHash,input.PreviewHash,StringComparison.Ordinal)) throw new InvalidOperationException("Fonti modificate tra preview e conferma: ripetere la simulazione.");
        if(!preview.Result.IsValid) throw new ArgumentException(string.Join(" ",preview.Result.Errors));
        var previous=input.Input.PreviousRunId.HasValue?await db.AllocationRuns.SingleAsync(x=>x.Id==input.Input.PreviousRunId,ct):null;
        var row=new AllocationRun(TenantId,condo,input.Input.AccountingPeriodId,input.RequestId,requestHash,preview.PreviewHash,preview.Result.EngineVersion,(previous?.Version??0)+1,previous?.Id,current.UserId!.Value,input.Input.CompetenceFrom,input.Input.CompetenceTo,preview.Result.OriginalAmount,Serialize(input.Input),Serialize(preview));db.Add(row);
        var sources=preview.Snapshot.Sources.Select(s=>new AllocationRunSource(TenantId,condo,row.Id,s.ExpenseDocumentId,s.RuleId,s.Position,s.Amount)).ToArray();db.AddRange(sources);
        db.AddRange(preview.Result.Lines.Select((l,i)=>new AllocationRunLine(TenantId,condo,row.Id,sources.Single(s=>s.Position==l.SourcePosition).Id,l.PropertyUnitId,l.PersonId,i,l.ComponentPosition,l.Amount,l.BaseAmount,l.UnroundedAmount,l.RoundingAdjustment)));
        Audit(previous==null?"Allocation.RunConfirmed":"Allocation.RunReplaced",row.Id);await db.SaveChangesAsync(ct);return await RunDtoFor(row,ct);
    },ct);
    private async Task<AllocationRunDto> RunDtoFor(AllocationRun row,CancellationToken ct)
    {
        var preview=Deserialize<AllocationPreviewDto>(row.SnapshotJson);var replacedBy=await db.AllocationRuns.Where(x=>x.PreviousRunId==row.Id).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);
        var expenseIds=preview.Snapshot.Sources.Select(x=>x.ExpenseDocumentId).Distinct().ToArray();
        var cancelled=await db.ExpenseDocuments.Where(x=>expenseIds.Contains(x.Id) && x.IsCancelled).OrderBy(x=>x.DocumentNumber).Select(x=>x.DocumentNumber).ToListAsync(ct);
        var warnings=cancelled.Select(n=>"Fonte annullata dopo la conferma: documento "+n+". Il risultato rimane uno storico e non genera crediti.").ToArray();
        return new(row.Id,row.AccountingPeriodId,row.Version,row.PreviousRunId,replacedBy,row.ConfirmedByUserId,row.CreatedAt,row.CompetenceFrom,row.CompetenceTo,row.TotalAmount,row.EngineVersion,row.PreviewHash,Deserialize<AllocationPreviewInput>(row.InputJson),preview.Snapshot,preview.Result,warnings);
    }
    public async Task<IReadOnlyList<AllocationRunDto>> Runs(Guid condo,Guid? period,CancellationToken ct)
    {await Condo(condo,ct);var query=db.AllocationRuns.Where(x=>x.CondominiumId==condo);if(period.HasValue) query=query.Where(x=>x.AccountingPeriodId==period);var result=new List<AllocationRunDto>();foreach(var row in await query.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct)) result.Add(await RunDtoFor(row,ct));return result;}
    public async Task<AllocationRunDto> Run(Guid condo,Guid id,CancellationToken ct) {await Condo(condo,ct);return await RunDtoFor(await db.AllocationRuns.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct)??throw new NotFoundException(),ct);}
    internal async Task<(AllocationRule Rule,List<ResolvedAllocationComponent> Components)> ResolveCriteria(Guid condo,AccountingPeriod period,Guid ruleId,DateOnly from,DateOnly to,List<PropertyUnit> units,IReadOnlyList<Guid>? excluded,IReadOnlyList<DirectChargeInput>? directCharges,bool lockSources,CancellationToken ct)
    {
            var rule=await Rule(condo,ruleId,ct);if(rule.Status!=AllocationRuleStatus.Active) throw new InvalidOperationException("Regola non attiva.");
            var components=await Components(rule.Id,ct);rule.Validate(components);
            if(excluded?.Distinct().Count()!=excluded?.Count) throw new ArgumentException("Unita escluse duplicate.");
            if(excluded?.Any(id=>!units.Any(u=>u.Id==id))==true) throw new NotFoundException();
            if(rule.Type!=AllocationCriterion.DirectCharge && directCharges?.Count>0) throw new ArgumentException("Addebiti diretti ammessi solo con la relativa regola.");
            var resolved=new List<ResolvedAllocationComponent>();
            foreach(var component in components)
            {
                await ValidateComponentReferences(condo,component,ct,true);
                var selected=units.Where(u=>excluded?.Contains(u.Id)!=true).ToList();
                object? groupEvidence=null;object? tableEvidence=null;object? consumptionEvidence=null;object? directEvidence=null;
                if(component.GroupId.HasValue)
                {
                    var group=await db.AllocationGroups.SingleAsync(x=>x.Id==component.GroupId,ct);var members=await db.AllocationGroupMembers.Where(x=>x.GroupId==group.Id).OrderBy(x=>x.PropertyUnitId).ToListAsync(ct);
                    if(members.Any(m=>!units.Any(u=>u.Id==m.PropertyUnitId))) throw new InvalidOperationException("Gruppo contiene unita inattive: aggiornare esplicitamente il gruppo.");
                    selected=selected.Where(u=>members.Any(m=>m.PropertyUnitId==u.Id)).ToList();groupEvidence=new{group.Id,group.Name,group.IsActive,Members=members.Select(m=>m.PropertyUnitId).ToArray()};
                }
                if(selected.Count==0) throw new InvalidOperationException("Gruppo senza unita ammesse.");
                var recipients=new List<AllocationRecipient>();decimal? nominal=null;
                if(component.Criterion==AllocationCriterion.Millesimal)
                {
                    var table=await db.AllocationTables.SingleAsync(x=>x.Id==component.TableId,ct);nominal=table.Denominator;
                    if(table.ValidFrom>from) throw new InvalidOperationException("Tabella non valida all'inizio della competenza.");
                    var entries=await db.AllocationTableEntries.Where(x=>x.AllocationTableId==table.Id && x.ValidFrom<=from && (x.ValidTo==null || x.ValidTo>=to)).OrderBy(x=>x.PropertyUnitId).ThenBy(x=>x.Id).ToListAsync(ct);
                    if(entries.Select(x=>x.PropertyUnitId).Distinct().Count()!=entries.Count || entries.Sum(x=>x.Value)!=table.Denominator || entries.Any(e=>!units.Any(u=>u.Id==e.PropertyUnitId)) || units.Any(u=>!entries.Any(e=>e.PropertyUnitId==u.Id))) throw new InvalidOperationException("Tabella parziale o totale quote non valido per le unita e la competenza correnti.");
                    recipients=selected.Select(u=>new AllocationRecipient(TenantId,condo,u.Id,u.Code,null,null,entries.Single(e=>e.PropertyUnitId==u.Id).Value)).ToList();
                    tableEvidence=new{table.Id,table.Code,table.Name,table.Status,table.ValidFrom,table.Denominator,Entries=entries.Select(e=>new{e.Id,e.PropertyUnitId,e.Value,e.ValidFrom,e.ValidTo}).ToArray()};
                }
                else if(component.Criterion==AllocationCriterion.Consumption)
                {
                    var readings=await db.ConsumptionReadings.Where(x=>x.CondominiumId==condo && x.AccountingPeriodId==period.Id && x.ConsumptionType==component.ConsumptionType && x.UnitOfMeasure==component.UnitOfMeasure).OrderBy(x=>x.Id).ToListAsync(ct);
                    var evidence=new List<object>();
                    foreach(var unit in selected)
                    {
                        var used=readings.Where(r=>r.PropertyUnitId==unit.Id).ToArray();var calculated=ConsumptionCalculator.Calculate(used,from,to);
                        if(calculated.Errors.Count>0) throw new InvalidOperationException("Consumi mancanti o incoerenti per "+unit.Code+": "+string.Join(" ",calculated.Errors));
                        recipients.Add(new(TenantId,condo,unit.Id,unit.Code,null,null,calculated.Consumption,calculated.IsEstimated));
                        evidence.Add(new{unit.Id,Readings=used.Select(r=>ReadingDtoFor(r,used.Any(n=>n.ReplacesReadingId==r.Id))).ToArray(),Result=calculated});
                    }
                    consumptionEvidence=evidence;
                }
                else if(component.Criterion==AllocationCriterion.DirectCharge)
                {
                    if(directCharges==null || directCharges.Count==0) throw new ArgumentException("Specificare gli addebiti diretti.");
                    var evidence=new List<object>();
                    foreach(var charge in directCharges.OrderBy(x=>x.PropertyUnitId).ThenBy(x=>x.PersonId))
                    {
                        Money.Positive(charge.Amount);var unit=selected.SingleOrDefault(u=>u.Id==charge.PropertyUnitId)??throw new NotFoundException();string? name=null;
                        if(charge.PersonId.HasValue)
                        {
                            if(lockSources) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"People\" WHERE \"Id\"={charge.PersonId.Value} AND \"TenantId\"={TenantId} FOR SHARE",ct);
                            var person=await db.People.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==charge.PersonId,ct)??throw new NotFoundException();name=person.DisplayName;
                            var associations=await db.OwnershipPeriods.Where(x=>x.PersonId==person.Id && x.PropertyUnitId==unit.Id && x.ValidFrom<=from && (x.ValidTo==null || x.ValidTo>=to)).OrderBy(x=>x.Id).ToListAsync(ct);
                            if(associations.Count==0) throw new InvalidOperationException("Soggetto senza associazione valida all'unita per l'intera competenza: non usare automaticamente il proprietario attuale.");
                            evidence.Add(new{UnitId=unit.Id,PersonId=person.Id,person.DisplayName,Associations=associations.Select(a=>new{a.Id,a.Role,a.ValidFrom,a.ValidTo,a.Fraction}).ToArray()});
                        }
                        recipients.Add(new(TenantId,condo,unit.Id,unit.Code,charge.PersonId,name,charge.Amount));
                    }
                    directEvidence=evidence;
                }
                else recipients=selected.Select(u=>new AllocationRecipient(TenantId,condo,u.Id,u.Code,null,null,1)).ToList();
                
                var evidenceJson=Serialize(new{RuleDescription=rule.Description,Group=groupEvidence,Table=tableEvidence,Consumption=consumptionEvidence,Direct=directEvidence,Excluded=(excluded??[]).Order().ToArray()});
                resolved.Add(new(component.Position,component.Criterion,component.Percentage,component.TableId,component.GroupId,component.ConsumptionType,component.UnitOfMeasure,nominal,true,component.NormalizeAdmitted,recipients,evidenceJson));
            }

        return (rule,resolved);
    }
}
