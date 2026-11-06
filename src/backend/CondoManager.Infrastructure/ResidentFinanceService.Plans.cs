using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
// Shares the M04 transaction, period, ledger posting and audit helpers, not a second ledger.
public sealed partial class AccountingService : IResidentFinanceService
{
    private static readonly JsonSerializerOptions FinanceJson=new(JsonSerializerDefaults.Web){NumberHandling=JsonNumberHandling.AllowReadingFromString|JsonNumberHandling.WriteAsString,Converters={new JsonStringEnumConverter()}};
    private static string FJson<T>(T v)=>JsonSerializer.Serialize(v,FinanceJson);
    private static T FRead<T>(string v)=>JsonSerializer.Deserialize<T>(v,FinanceJson)??throw new InvalidOperationException("Snapshot non valido.");
    private static string FHash<T>(T v)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(v,FinanceJson)));
    private async Task<InstallmentPlan> Plan(Guid condo,Guid id,CancellationToken ct)=>await db.InstallmentPlans.SingleOrDefaultAsync(x=>x.Id==id && x.CondominiumId==condo,ct)??throw new NotFoundException();
    private static PlanDto PlanResult(InstallmentPlan p)=>new(p.Id,p.AccountingPeriodId,p.BudgetId,p.Version,p.PreviousPlanId,p.Status,p.Total,p.JournalEntryId,p.ApprovedBy,p.ApprovedAt,FRead<PlanInput>(p.InputJson),FRead<PlanPreview>(p.SnapshotJson));
    private async Task<PlanPreview> BuildPlan(Guid condo,PlanInput input,CancellationToken ct,bool lockPeople=false)
    {
        Guard.Id(input.RequestId);Guard.EnumValue(input.Mode);var budget=await db.Budgets.SingleOrDefaultAsync(x=>x.Id==input.BudgetId && x.CondominiumId==condo,ct)??throw new NotFoundException();
        if(budget.Status!=BudgetStatus.Approved)throw new InvalidOperationException("Selezionare la versione corrente approvata del preventivo.");
        var period=await db.AccountingPeriods.SingleAsync(x=>x.Id==budget.AccountingPeriodId && x.CondominiumId==condo,ct);JournalEntry.EnsureOpen(period,input.DebtorDate);
        if(input.Schedule==null || input.Schedule.Count is <1 or >120 || input.Debtors==null)throw new ArgumentException("Scadenze e debitori obbligatori.");
        if(input.Schedule.Any(s=>s.DueDate<period.StartsOn || s.DueDate>period.EndsOn))throw new ArgumentException("Scadenze entro l'esercizio del piano.");
        if(input.Debtors.Select(x=>(x.PropertyUnitId,x.PersonId)).Distinct().Count()!=input.Debtors.Count)throw new ArgumentException("Designazioni duplicate.");
        var allocation=FRead<BudgetPreviewDto>(budget.AllocationSnapshotJson!);if(!allocation.Result.IsValid)throw new InvalidOperationException("Snapshot preventivo non quadrato.");
        var unitIds=allocation.Result.Lines.Select(l=>l.PropertyUnitId).Distinct().ToArray();var units=await db.PropertyUnits.Where(u=>u.CondominiumId==condo && unitIds.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,ct);if(units.Count!=unitIds.Length)throw new NotFoundException();
        if(lockPeople) foreach(var personId in input.Debtors.Select(d=>d.PersonId).Concat(allocation.Result.Lines.Where(l=>l.PersonId.HasValue).Select(l=>l.PersonId!.Value)).Distinct().Order()) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"People\" WHERE \"Id\"={personId} AND \"TenantId\"={TenantId} FOR SHARE",ct);
        var people=await db.People.Where(p=>input.Debtors.Select(d=>d.PersonId).Contains(p.Id) || allocation.Result.Lines.Select(l=>l.PersonId).Contains(p.Id)).ToDictionaryAsync(p=>p.Id,ct);
        var associations=await db.OwnershipPeriods.Where(a=>unitIds.Contains(a.PropertyUnitId) && a.ValidFrom<=input.DebtorDate && (a.ValidTo==null || a.ValidTo>=input.DebtorDate)).OrderBy(a=>a.Id).ToListAsync(ct);
        var quotas=new Dictionary<(Guid Unit,Guid Person),decimal>();var evidence=new List<object>();
        void Add(Guid u,Guid p,decimal amount) {if(!people.ContainsKey(p))throw new NotFoundException();if(!associations.Any(a=>a.PropertyUnitId==u && a.PersonId==p))throw new ArgumentException("Debitore senza associazione storica valida alla data designata.");quotas[(u,p)]=quotas.GetValueOrDefault((u,p))+amount;}
        foreach(var group in allocation.Result.Lines.GroupBy(l=>l.PropertyUnitId))
        {
            foreach(var direct in group.Where(l=>l.PersonId.HasValue))Add(group.Key,direct.PersonId!.Value,direct.Amount);
            var amount=group.Where(l=>!l.PersonId.HasValue).Sum(l=>l.Amount);var choices=input.Debtors.Where(d=>d.PropertyUnitId==group.Key).OrderBy(d=>d.PersonId).ToArray();
            if(amount>0) {
                if(choices.Length==0 || choices.Sum(c=>c.Share)!=1)throw new ArgumentException("Designare esplicitamente i debitori di ogni unita: somma quote esattamente 1.");
                foreach(var c in choices) {Guard.Decimal(c.Share,6,0.000001m,1);var a=associations.FirstOrDefault(a=>a.PersonId==c.PersonId && a.PropertyUnitId==c.PropertyUnitId)??throw new ArgumentException("Associazione storica del debitore assente.");if(a.Fraction.HasValue && a.Fraction!=c.Share)throw new ArgumentException("Quota designata diversa dalla quota storica: rettifica esplicita necessaria.");}
                var split=AllocationEngine.SplitCents(amount,choices.Select(c=>c.Share).ToArray(),choices.Select(c=>c.PersonId.ToString("D")).ToArray());for(var i=0;i<choices.Length;i++)Add(group.Key,choices[i].PersonId,split[i].Amount);
            } else if(choices.Length>0)throw new ArgumentException("Designazioni superflue per una quota gia personale.");
        }
        if(input.Debtors.Any(d=>!unitIds.Contains(d.PropertyUnitId)))throw new ArgumentException("Designazione di unita estranea al riparto.");
        foreach(var q in quotas.OrderBy(q=>q.Key.Unit).ThenBy(q=>q.Key.Person)) evidence.Add(new{Unit=q.Key.Unit,units[q.Key.Unit].Code,Person=q.Key.Person,people[q.Key.Person].DisplayName,Associations=associations.Where(a=>a.PropertyUnitId==q.Key.Unit && a.PersonId==q.Key.Person).Select(a=>new{a.Id,a.ValidFrom,a.ValidTo,a.Fraction,a.Role}).ToArray()});
        var total=allocation.Result.OriginalAmount;var schedule=input.Mode==ScheduleMode.Equal?input.Schedule.Select(s=>s with{Weight=1}).ToArray():input.Schedule.ToArray();
        if(input.Mode==ScheduleMode.Amounts && schedule.Sum(s=>Money.Positive(s.Weight))!=total)throw new ArgumentException("Gli importi delle scadenze devono sommare il totale del preventivo.");
        var individual=quotas.OrderBy(q=>q.Key.Unit).ThenBy(q=>q.Key.Person).Select(q=>new IndividualQuota(q.Key.Unit,q.Key.Person,q.Value)).ToArray();
        var installments=InstallmentCalculator.Calculate(individual,schedule,input.Mode==ScheduleMode.Percentages);
        if(individual.Sum(q=>q.Amount)!=total || installments.Sum(r=>r.Amount)!=total)throw new InvalidOperationException("Rate non quadrate.");
        // Amounts are exact aggregate deadlines, with deterministic transportation of rounding cents.
        if(input.Mode==ScheduleMode.Amounts)installments=InstallmentCalculator.CalculateExactAmounts(individual,schedule);
        InstallmentPlan? previous=null;
        if(input.PreviousPlanId.HasValue){previous=await Plan(condo,input.PreviousPlanId.Value,ct);if(previous.AccountingPeriodId!=period.Id)throw new ArgumentException("Piano precedente di un altro esercizio.");}
        var old=previous==null?Array.Empty<IndividualQuota>():FRead<PlanPreview>(previous.SnapshotJson).Quotas.ToArray();
        var differences=old.Select(q=>(q.PropertyUnitId,q.DebtorId)).Union(individual.Select(q=>(q.PropertyUnitId,q.DebtorId))).OrderBy(k=>k.PropertyUnitId).ThenBy(k=>k.DebtorId).Select(k=>{var a=old.Where(q=>q.PropertyUnitId==k.PropertyUnitId && q.DebtorId==k.DebtorId).Sum(q=>q.Amount);var b=individual.Where(q=>q.PropertyUnitId==k.PropertyUnitId && q.DebtorId==k.DebtorId).Sum(q=>q.Amount);return new PlanDifference(k.PropertyUnitId,k.DebtorId,a,b,b-a);}).ToArray();
        var ej=FJson(new{Budget=budget.Id,budget.Version,Allocation=allocation,Debtors=evidence});return new(FHash(new{Input=input,Evidence=ej,Engine=AllocationEngine.Version}),total,individual,installments,differences,ej);
    }
    public async Task<PlanPreview> PreviewPlan(Guid condo,PlanInput input,CancellationToken ct) {await Condo(condo,ct);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);var r=await BuildPlan(condo,input,ct);await tx.CommitAsync(ct);return r;}
    public Task<PlanDto> CreatePlan(Guid condo,PlanInput input,CancellationToken ct)=>Write(condo,async()=>{
        Guard.Id(input.RequestId);var hash=FHash(input);var existing=await db.InstallmentPlans.SingleOrDefaultAsync(p=>p.CondominiumId==condo && p.RequestId==input.RequestId,ct);if(existing!=null){if(existing.RequestHash!=hash)throw new InvalidOperationException("Chiave riutilizzata con parametri diversi.");return PlanResult(existing);}
        var preview=await BuildPlan(condo,input,ct,true);var budget=await db.Budgets.SingleAsync(b=>b.Id==input.BudgetId,ct);var latest=await db.InstallmentPlans.Where(p=>p.CondominiumId==condo && p.AccountingPeriodId==budget.AccountingPeriodId).OrderByDescending(p=>p.Version).FirstOrDefaultAsync(ct);
        if(latest?.Id!=input.PreviousPlanId)throw new InvalidOperationException("Indicare l'ultima versione come piano precedente.");
        var plan=new InstallmentPlan(TenantId,condo,budget.AccountingPeriodId,budget.Id,(latest?.Version??0)+1,latest?.Id,input.RequestId,hash,FJson(input),FJson(preview),preview.Total);db.Add(plan);
        var schedules=input.Schedule.Select((s,i)=>new PlanSchedule(TenantId,condo,plan.Id,i,s.DueDate,input.Mode==ScheduleMode.Equal?1:s.Weight)).ToArray();db.AddRange(schedules);
        var debtors=new List<InstallmentDebtor>();foreach(var q in preview.Quotas){var unit=await db.PropertyUnits.SingleAsync(u=>u.Id==q.PropertyUnitId,ct);var person=await db.People.SingleAsync(p=>p.Id==q.DebtorId,ct);debtors.Add(new(TenantId,condo,plan.Id,unit.Id,person.Id,unit.Code,person.DisplayName,q.Amount));}db.AddRange(debtors);
        db.AddRange(preview.Installments.Select(r=>new Installment(TenantId,condo,plan.Id,schedules[r.Position].Id,debtors.Single(d=>d.PropertyUnitId==r.PropertyUnitId && d.PersonId==r.DebtorId).Id,r.Amount)));
        Audit("Resident.PlanDraftCreated",plan.Id);return PlanResult(plan);
    },ct);
    public async Task<IReadOnlyList<PlanDto>> Plans(Guid condo,Guid? period,CancellationToken ct) {await Condo(condo,ct);return (await db.InstallmentPlans.Where(p=>p.CondominiumId==condo && (!period.HasValue || p.AccountingPeriodId==period)).OrderByDescending(p=>p.Version).ToListAsync(ct)).Select(PlanResult).ToArray();}
    private async Task EnsureResidentControls(Guid condo,CancellationToken ct)
    {
        foreach(var definition in new[]{(AccountPurpose.ResidentReceivables,"110-RATE","Crediti rate emesse",AccountType.Asset),(AccountPurpose.BudgetContributions,"320-CONTRIBUTI","Contributi preventivi emessi",AccountType.Equity)})
        {if(await db.Accounts.AnyAsync(a=>a.CondominiumId==condo && a.Purpose==definition.Item1,ct))continue;if(await db.Accounts.AnyAsync(a=>a.CondominiumId==condo && a.Code==definition.Item2,ct))throw new InvalidOperationException("Codice riservato occupato: "+definition.Item2);db.Add(new Account(TenantId,condo,definition.Item2,definition.Item3,definition.Item4,definition.Item1));}await db.SaveChangesAsync(ct);
    }
    public Task<PlanDto> ApprovePlan(Guid condo,Guid id,PlanApproval input,CancellationToken ct)=>Write(condo,async()=>{
        current.RequireAdmin();var plan=await Plan(condo,id,ct);var hash=FHash(new{Operation="IssuePlan",Plan=id,Input=input});var retry=await Retry(condo,input.RequestId,hash,ct);if(retry!=null){if(plan.JournalEntryId!=retry.Id)throw new InvalidOperationException("Operazione diversa.");return PlanResult(plan);}
        if(plan.Status!=InstallmentPlanStatus.Draft)throw new InvalidOperationException("Piano gia emesso o sostituito.");
        if(await db.InstallmentPlans.AnyAsync(p=>p.CondominiumId==condo && p.AccountingPeriodId==plan.AccountingPeriodId && p.JournalEntryId!=null,ct))throw new InvalidOperationException("Esiste un piano emesso: differenze consultabili, ma sostituzione e compensazioni richiedono una rettifica amministrativa esplicita non automatizzata.");
        if(await db.InstallmentPlans.AnyAsync(p=>p.CondominiumId==condo && p.AccountingPeriodId==plan.AccountingPeriodId && p.Version>plan.Version,ct))throw new InvalidOperationException("Approvare l'ultima versione del piano.");
        var preview=await BuildPlan(condo,FRead<PlanInput>(plan.InputJson),ct,true);if(preview.Hash!=input.PreviewHash || preview.Hash!=FRead<PlanPreview>(plan.SnapshotJson).Hash)throw new InvalidOperationException("Fonti cambiate: creare una nuova versione e risimulare.");
        var period=await Period(condo,plan.AccountingPeriodId,input.Date,ct);await EnsureResidentControls(condo,ct);
        var journal=new JournalEntry(TenantId,condo,period,input.Date,"Emissione piano rate v"+plan.Version,null,input.RequestId,hash,JournalKind.Assessment);journal.AddLine(await Control(condo,AccountPurpose.ResidentReceivables,ct),plan.Total,0);journal.AddLine(await Control(condo,AccountPurpose.BudgetContributions,ct),0,plan.Total);plan.Approve(journal.Id,current.UserId!.Value);
        foreach(var draft in await db.InstallmentPlans.Where(p=>p.CondominiumId==condo && p.AccountingPeriodId==plan.AccountingPeriodId && p.Id!=plan.Id && p.Status==InstallmentPlanStatus.Draft).ToListAsync(ct))draft.SupersedeDraft();
        await Persist(journal,ct);Audit("Resident.PlanIssued",plan.Id);return PlanResult(plan);
    },ct);
}
