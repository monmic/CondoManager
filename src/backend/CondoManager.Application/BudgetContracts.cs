using CondoManager.Domain;
namespace CondoManager.Application;
public record BudgetLineInput(Guid AccountId,string Description,decimal Amount,Guid RuleId,IReadOnlyList<Guid>? ExcludedUnitIds=null,IReadOnlyList<DirectChargeInput>? DirectCharges=null,string? Notes=null);
public record BudgetInput(Guid AccountingPeriodId,string Description,IReadOnlyList<BudgetLineInput> Lines,string? Notes=null,Guid? PreviousVersionId=null);
public record BudgetDto(Guid Id,Guid AccountingPeriodId,int Version,Guid? PreviousVersionId,string Description,string? Notes,BudgetStatus Status,DateTimeOffset CreatedAt,Guid? ApprovedBy,DateTimeOffset? ApprovedAt,string? DecisionReference,decimal Total,decimal? PreviousTotal,decimal? Difference,IReadOnlyList<BudgetLineInput> Lines,AllocationCalculationResult? Allocation);
public record BudgetPreviewDto(string Hash,AllocationCalculationInput Snapshot,AllocationCalculationResult Result);
public record BudgetApprovalInput(string PreviewHash,string? DecisionReference=null);
public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> List(Guid condo,Guid? period,CancellationToken ct);
    Task<BudgetDto> Save(Guid condo,Guid? id,BudgetInput input,CancellationToken ct);
    Task<BudgetDto> Propose(Guid condo,Guid id,CancellationToken ct);
    Task<BudgetPreviewDto> Preview(Guid condo,Guid id,CancellationToken ct);
    Task<BudgetDto> Approve(Guid condo,Guid id,BudgetApprovalInput input,CancellationToken ct);
}
public record InstallmentSchedule(DateOnly DueDate,decimal Weight);
public record IndividualQuota(Guid PropertyUnitId,Guid DebtorId,decimal Amount);
public record CalculatedInstallment(Guid PropertyUnitId,Guid DebtorId,int Position,DateOnly DueDate,decimal Amount);
public static class InstallmentCalculator
{
    public static IReadOnlyList<CalculatedInstallment> Calculate(IReadOnlyList<IndividualQuota> quotas,IReadOnlyList<InstallmentSchedule> schedule,bool percentages=true)
    {
        if(quotas.Count==0 || quotas.Select(q=>(q.PropertyUnitId,q.DebtorId)).Distinct().Count()!=quotas.Count) throw new ArgumentException("Quote assenti o duplicate.");
        if(schedule.Count is <1 or >120 || schedule.Any(s=>s.DueDate==default) || schedule.Select(s=>s.DueDate).Distinct().Count()!=schedule.Count || !schedule.Select(s=>s.DueDate).Order().SequenceEqual(schedule.Select(s=>s.DueDate))) throw new ArgumentException("Scadenze univoche, ordinate e obbligatorie.");
        foreach(var s in schedule) Guard.Decimal(s.Weight,6,0.000001m,999999999999.999999m);
        if(percentages && schedule.Sum(s=>s.Weight)!=100) throw new ArgumentException("Percentuali delle rate diverse da 100%.");
        var result=new List<CalculatedInstallment>();
        foreach(var q in quotas.OrderBy(q=>q.PropertyUnitId).ThenBy(q=>q.DebtorId))
        {
            Guard.Id(q.PropertyUnitId);Guard.Id(q.DebtorId);Money.Validate(q.Amount);
            var amounts=AllocationEngine.SplitCents(q.Amount,schedule.Select(s=>s.Weight).ToArray(),schedule.Select((_,i)=>i.ToString("D4")).ToArray());
            result.AddRange(amounts.Select((p,i)=>new CalculatedInstallment(q.PropertyUnitId,q.DebtorId,i,schedule[i].DueDate,p.Amount)));
        }
        return result;
    }
    public static IReadOnlyList<CalculatedInstallment> CalculateExactAmounts(IReadOnlyList<IndividualQuota> quotas,IReadOnlyList<InstallmentSchedule> schedule)
    {
        if(schedule.Sum(s=>Money.Positive(s.Weight))!=quotas.Sum(q=>Money.Validate(q.Amount))) throw new ArgumentException("Importi scadenze diversi dalle quote individuali.");
        var rows=Calculate(quotas,schedule,false).ToArray();var difference=schedule.Select((s,i)=>s.Weight-rows.Where(r=>r.Position==i).Sum(r=>r.Amount)).ToArray();
        // Transfers within one debtor preserve every individual quota. Stable order: debtor, unit, deadline.
        for(var target=0;target<schedule.Count;target++)while(difference[target]>0)
        {
            var source=Array.FindIndex(difference,d=>d<0);if(source<0)throw new InvalidOperationException("Scadenze non quadrate.");
            var donors=rows.Select((r,i)=>(r,i)).Where(x=>x.r.Position==source && x.r.Amount>0).OrderBy(x=>x.r.PropertyUnitId).ThenBy(x=>x.r.DebtorId).ToArray();
            foreach(var donor in donors) {var receiver=Array.FindIndex(rows,r=>r.Position==target && r.PropertyUnitId==donor.r.PropertyUnitId && r.DebtorId==donor.r.DebtorId);var amount=Math.Min(rows[donor.i].Amount,Math.Min(difference[target],-difference[source]));rows[donor.i]=rows[donor.i] with{Amount=rows[donor.i].Amount-amount};rows[receiver]=rows[receiver] with{Amount=rows[receiver].Amount+amount};difference[target]-=amount;difference[source]+=amount;if(difference[target]==0 || difference[source]==0)break;}
        }
        return rows;
    }
}
public record SettlementAmounts(decimal BudgetQuota,decimal ActualQuota,decimal OpeningDebt,decimal Adjustments,decimal Receipts,decimal RecognizedCredits)
{
    public decimal EconomicAdjustment=>ActualQuota-BudgetQuota;
    public decimal FinancialBalance=>ActualQuota+OpeningDebt+Adjustments-Receipts-RecognizedCredits;
    public void Validate() { Money.Validate(BudgetQuota);Money.Validate(ActualQuota);Money.Validate(OpeningDebt,true);Money.Validate(Adjustments,true);Money.Validate(Receipts);Money.Validate(RecognizedCredits); }
}
