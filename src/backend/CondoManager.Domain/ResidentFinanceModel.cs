namespace CondoManager.Domain;
public enum InstallmentPlanStatus { Draft, Approved, Superseded }
public enum ReceiptMatchStatus { Matched, Reconciled }
public sealed class InstallmentPlan : CondominiumEntity
{
    private InstallmentPlan() : base(Guid.NewGuid(),Guid.NewGuid()) {InputJson=SnapshotJson=RequestHash=null!;}
    public Guid AccountingPeriodId {get;private set;}
    public Guid BudgetId {get;private set;}
    public int Version {get;private set;}
    public Guid? PreviousPlanId {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}
    public string InputJson {get;private set;}
    public string SnapshotJson {get;private set;}
    public decimal Total {get;private set;}
    public InstallmentPlanStatus Status {get;private set;}
    public Guid? JournalEntryId {get;private set;}
    public Guid? ApprovedBy {get;private set;}
    public DateTimeOffset? ApprovedAt {get;private set;}
    public InstallmentPlan(Guid tenant,Guid condo,Guid period,Guid budget,int version,Guid? previous,Guid request,string hash,string input,string snapshot,decimal total) : base(tenant,condo)
    {AccountingPeriodId=Guard.Id(period);BudgetId=Guard.Id(budget);Version=version>0?version:throw new ArgumentException("Versione non valida.");PreviousPlanId=previous;RequestId=Guard.Id(request);RequestHash=Guard.Text(hash,64);InputJson=Guard.Text(input,20000000);SnapshotJson=Guard.Text(snapshot,20000000);Total=Money.Positive(total);}
    public void Approve(Guid journal,Guid actor) {if(Status!=InstallmentPlanStatus.Draft)throw new InvalidOperationException("Piano non in bozza.");JournalEntryId=Guard.Id(journal);ApprovedBy=Guard.Id(actor);ApprovedAt=DateTimeOffset.UtcNow;Status=InstallmentPlanStatus.Approved;}
    public void SupersedeDraft() {if(Status!=InstallmentPlanStatus.Draft)throw new InvalidOperationException("Piano emesso immutabile: rettifica amministrativa necessaria.");Status=InstallmentPlanStatus.Superseded;}
}
public sealed class PlanSchedule : CondominiumEntity
{
    public Guid PlanId {get;private set;}
    public int Position {get;private set;}
    public DateOnly DueDate {get;private set;}
    public decimal Weight {get;private set;}
    public PlanSchedule(Guid tenantId,Guid condominiumId,Guid planId,int position,DateOnly dueDate,decimal weight):base(tenantId,condominiumId)
    {PlanId=Guard.Id(planId);Position=position>=0?position:throw new ArgumentException("Posizione non valida.");Guard.Dates(dueDate,null);DueDate=dueDate;Weight=Guard.Decimal(weight,6,0.000001m,999999999999.999999m);}
}
public sealed class InstallmentDebtor : CondominiumEntity
{
    public Guid PlanId {get;private set;}
    public Guid PropertyUnitId {get;private set;}
    public Guid PersonId {get;private set;}
    public string UnitCode {get;private set;}
    public string PersonName {get;private set;}
    public decimal Amount {get;private set;}
    public InstallmentDebtor(Guid tenantId,Guid condominiumId,Guid planId,Guid propertyUnitId,Guid personId,string unitCode,string personName,decimal amount):base(tenantId,condominiumId)
    {PlanId=Guard.Id(planId);PropertyUnitId=Guard.Id(propertyUnitId);PersonId=Guard.Id(personId);UnitCode=Guard.Text(unitCode);PersonName=Guard.Text(personName);Amount=Money.Validate(amount);}
}
public sealed class Installment : CondominiumEntity
{
    public Guid PlanId {get;private set;}
    public Guid ScheduleId {get;private set;}
    public Guid InstallmentDebtorId {get;private set;}
    public decimal Amount {get;private set;}
    public Installment(Guid tenantId,Guid condominiumId,Guid planId,Guid scheduleId,Guid installmentDebtorId,decimal amount):base(tenantId,condominiumId)
    {PlanId=Guard.Id(planId);ScheduleId=Guard.Id(scheduleId);InstallmentDebtorId=Guard.Id(installmentDebtorId);Amount=Money.Validate(amount);}
}
public sealed class ResidentReceipt : CondominiumEntity
{
    public Guid AccountingPeriodId {get;private set;}
    public Guid FinancialAccountId {get;private set;}
    public Guid? DebtorId {get;private set;}
    public Guid? PayerId {get;private set;}
    public DateOnly PaymentDate {get;private set;}
    public decimal Amount {get;private set;}
    public string? Reference {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}
    public ResidentReceipt(Guid tenantId,Guid condominiumId,Guid accountingPeriodId,Guid financialAccountId,Guid? debtorId,Guid? payerId,DateOnly paymentDate,decimal amount,string? reference,Guid requestId,string requestHash):base(tenantId,condominiumId)
    {AccountingPeriodId=Guard.Id(accountingPeriodId);FinancialAccountId=Guard.Id(financialAccountId);DebtorId=debtorId;PayerId=payerId;Guard.Dates(paymentDate,null);PaymentDate=paymentDate;Amount=Money.Positive(amount);Reference=Guard.Optional(reference);RequestId=Guard.Id(requestId);RequestHash=Guard.Text(requestHash,64);}
}
public sealed class ReceiptBankLink : CondominiumEntity
{
    private ReceiptBankLink() : base(Guid.NewGuid(),Guid.NewGuid()) {}
    public Guid ReceiptId {get;private set;}
    public Guid JournalEntryId {get;private set;}
    public ReceiptMatchStatus Status {get;private set;}
    public ReceiptBankLink(Guid tenantId,Guid condominiumId,Guid receiptId,Guid journalEntryId,bool reconciled=false):base(tenantId,condominiumId)
    {ReceiptId=Guard.Id(receiptId);JournalEntryId=Guard.Id(journalEntryId);Status=reconciled?ReceiptMatchStatus.Reconciled:ReceiptMatchStatus.Matched;}
    public void Reconcile()=>Status=ReceiptMatchStatus.Reconciled;
}
public sealed class ReceiptAllocation : CondominiumEntity
{
    public Guid ReceiptId {get;private set;}
    public Guid InstallmentId {get;private set;}
    public Guid JournalEntryId {get;private set;}
    public Guid? ReversesAllocationId {get;private set;}
    public decimal Amount {get;private set;}
    public ReceiptAllocation(Guid tenantId,Guid condominiumId,Guid receiptId,Guid installmentId,Guid journalEntryId,decimal amount,Guid? reversesAllocationId=null):base(tenantId,condominiumId)
    {ReceiptId=Guard.Id(receiptId);InstallmentId=Guard.Id(installmentId);JournalEntryId=Guard.Id(journalEntryId);Amount=Money.Positive(amount);ReversesAllocationId=reversesAllocationId;}
}
