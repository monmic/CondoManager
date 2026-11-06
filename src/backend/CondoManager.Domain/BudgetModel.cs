namespace CondoManager.Domain;
public enum BudgetStatus { Draft, Proposed, Approved, Superseded }
public sealed class Budget : CondominiumEntity
{
    private Budget() : base(Guid.NewGuid(),Guid.NewGuid()) { Description=null!; }
    public Guid AccountingPeriodId { get; private set; }
    public int Version { get; private set; }
    public Guid? PreviousVersionId { get; private set; }
    public string Description { get; private set; }
    public string? Notes { get; private set; }
    public BudgetStatus Status { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? DecisionReference { get; private set; }
    public string? AllocationSnapshotJson { get; private set; }
    public Budget(Guid tenant,Guid condo,AccountingPeriod period,string description,int version=1,Guid? previous=null,string? notes=null) : base(tenant,condo)
    {
        if(period.TenantId!=tenant || period.CondominiumId!=condo) throw new ArgumentException("Esercizio di un altro studio o condominio.");
        if(period.Status!=AccountingPeriodStatus.Open) throw new InvalidOperationException("Esercizio non aperto.");
        AccountingPeriodId=period.Id;Version=version>0?version:throw new ArgumentException("Versione non valida.");PreviousVersionId=previous;
        Description=Guard.Text(description);Notes=Guard.Optional(notes,4000);
    }
    public void EnsureDraft() { if(Status!=BudgetStatus.Draft) throw new InvalidOperationException("Creare una nuova versione del preventivo."); }
    public void Update(string description,string? notes) {EnsureDraft();Description=Guard.Text(description);Notes=Guard.Optional(notes,4000);}
    public void Propose() {EnsureDraft();Status=BudgetStatus.Proposed;}
    public void Approve(Guid user,string snapshot,string? reference)
    {
        if(Status!=BudgetStatus.Proposed) throw new InvalidOperationException("Approvabile soltanto un preventivo proposto.");
        ApprovedBy=Guard.Id(user);AllocationSnapshotJson=Guard.Text(snapshot,20000000);DecisionReference=Guard.Optional(reference);ApprovedAt=DateTimeOffset.UtcNow;Status=BudgetStatus.Approved;
    }
    public void Supersede() {if(Status!=BudgetStatus.Approved) throw new InvalidOperationException("Preventivo non approvato.");Status=BudgetStatus.Superseded;}
}
public sealed class BudgetLine : CondominiumEntity
{
    public Guid BudgetId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid RuleId { get; private set; }
    public int Position { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }
    public string ParametersJson { get; private set; }
    private BudgetLine() : base(Guid.NewGuid(),Guid.NewGuid()) {Description=ParametersJson=null!;}
    public BudgetLine(Guid tenant,Guid condo,Guid budget,Guid account,Guid rule,int position,string description,decimal amount,string parameters,string? notes) : base(tenant,condo)
    { BudgetId=Guard.Id(budget);AccountId=Guard.Id(account);RuleId=Guard.Id(rule);Position=position>=0?position:throw new ArgumentException("Posizione non valida.");Description=Guard.Text(description);Amount=Money.Positive(amount);ParametersJson=Guard.Text(parameters,1000000);Notes=Guard.Optional(notes,4000); }
}
