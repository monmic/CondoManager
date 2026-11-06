namespace CondoManager.Domain;
public enum FinalStatementStatus { Draft, Proposed, Approved, Superseded }
public enum OpeningMode { VerifiedZero, Import, CarryForward }
public sealed class FinalStatement : CondominiumEntity
{
    private FinalStatement():base(Guid.NewGuid(),Guid.NewGuid()){Description=InputJson=SnapshotJson=SourceFingerprint=null!;}
    public Guid AccountingPeriodId {get;private set;}
    public Guid? BudgetId {get;private set;}
    public int Version {get;private set;}
    public Guid? PreviousVersionId {get;private set;}
    public string Description {get;private set;}
    public string? Notes {get;private set;}
    public Guid CreatedBy {get;private set;}
    public FinalStatementStatus Status {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}=null!;
    public string InputJson {get;private set;}
    public string SnapshotJson {get;private set;}
    public string SourceFingerprint {get;private set;}
    public Guid? ApprovedBy {get;private set;}
    public DateTimeOffset? ApprovedAt {get;private set;}
    public string? DecisionReference {get;private set;}
    public FinalStatement(Guid tenant,Guid condo,Guid period,Guid? budget,int version,Guid? previous,string description,string? notes,Guid actor,Guid request,string hash,string input,string snapshot,string fingerprint):base(tenant,condo)
    {AccountingPeriodId=Guard.Id(period);BudgetId=budget;Version=version>0?version:throw new ArgumentException("Versione non valida.");PreviousVersionId=previous;Description=Guard.Text(description);Notes=Guard.Optional(notes,4000);CreatedBy=Guard.Id(actor);RequestId=Guard.Id(request);RequestHash=Guard.Text(hash,64);InputJson=Guard.Text(input,20000000);SnapshotJson=Guard.Text(snapshot,20000000);SourceFingerprint=Guard.Text(fingerprint,64);}
    public void Propose(){if(Status!=FinalStatementStatus.Draft)throw new InvalidOperationException("Consuntivo non in bozza.");Status=FinalStatementStatus.Proposed;}
    public void Approve(Guid actor,string? reference){if(Status!=FinalStatementStatus.Proposed)throw new InvalidOperationException("Proporre prima il consuntivo.");ApprovedBy=Guard.Id(actor);ApprovedAt=DateTimeOffset.UtcNow;DecisionReference=Guard.Optional(reference);Status=FinalStatementStatus.Approved;}
    public void Supersede(){if(Status!=FinalStatementStatus.Approved)throw new InvalidOperationException("Consuntivo non approvato.");Status=FinalStatementStatus.Superseded;}
}
public sealed class StatementSettlement : CondominiumEntity
{
    public Guid StatementId {get;private set;}
    public Guid AccountingPeriodId {get;private set;}
    public DateOnly Date {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}
    public string SnapshotJson {get;private set;}
    public string Fingerprint {get;private set;}
    public Guid ConfirmedBy {get;private set;}
    public Guid? JournalEntryId {get;private set;}
    public DateTimeOffset? PostedAt {get;private set;}
    public Guid? PostedBy {get;private set;}
    public Guid? ReversedJournalId {get;private set;}
    public StatementSettlement(Guid tenantId,Guid condominiumId,Guid statementId,Guid accountingPeriodId,DateOnly date,Guid requestId,string requestHash,string snapshotJson,string fingerprint,Guid confirmedBy):base(tenantId,condominiumId)
    {StatementId=Guard.Id(statementId);AccountingPeriodId=Guard.Id(accountingPeriodId);Guard.Dates(date,null);Date=date;RequestId=Guard.Id(requestId);RequestHash=Guard.Text(requestHash,64);SnapshotJson=Guard.Text(snapshotJson,20000000);Fingerprint=Guard.Text(fingerprint,64);ConfirmedBy=Guard.Id(confirmedBy);}
    public void Post(Guid? journal,Guid actor){if(PostedAt.HasValue)throw new InvalidOperationException("Conguaglio gia contabilizzato.");JournalEntryId=journal;PostedBy=Guard.Id(actor);PostedAt=DateTimeOffset.UtcNow;}
    public void Reverse(Guid journal){if(!PostedAt.HasValue || JournalEntryId==null || ReversedJournalId!=null)throw new InvalidOperationException("Conguaglio non stornabile.");ReversedJournalId=Guard.Id(journal);}
}
public sealed class SettlementPosition : CondominiumEntity
{
    public Guid SettlementId {get;private set;}
    public Guid PropertyUnitId {get;private set;}
    public Guid PersonId {get;private set;}
    public string UnitCode {get;private set;}
    public string PersonName {get;private set;}
    public decimal Amount {get;private set;}
    public SettlementPosition(Guid tenantId,Guid condominiumId,Guid settlementId,Guid propertyUnitId,Guid personId,string unitCode,string personName,decimal amount):base(tenantId,condominiumId)
    {SettlementId=Guard.Id(settlementId);PropertyUnitId=Guard.Id(propertyUnitId);PersonId=Guard.Id(personId);UnitCode=Guard.Text(unitCode);PersonName=Guard.Text(personName);Amount=Money.Validate(amount,true);}
}
public sealed class ResidentOpening : CondominiumEntity
{
    public Guid AccountingPeriodId {get;private set;}
    public OpeningMode Mode {get;private set;}
    public Guid? SourceClosureId {get;private set;}
    public Guid? JournalEntryId {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}
    public Guid VerifiedBy {get;private set;}
    public string Reason {get;private set;}
    public string SnapshotJson {get;private set;}
    public ResidentOpening(Guid tenantId,Guid condominiumId,Guid accountingPeriodId,OpeningMode mode,Guid? sourceClosureId,Guid? journalEntryId,Guid requestId,string requestHash,Guid verifiedBy,string reason,string snapshotJson):base(tenantId,condominiumId)
    {AccountingPeriodId=Guard.Id(accountingPeriodId);Mode=Guard.EnumValue(mode);SourceClosureId=sourceClosureId;JournalEntryId=journalEntryId;RequestId=Guard.Id(requestId);RequestHash=Guard.Text(requestHash,64);VerifiedBy=Guard.Id(verifiedBy);Reason=Guard.Text(reason);SnapshotJson=Guard.Text(snapshotJson,20000000);}
}
public sealed class OpeningPosition : CondominiumEntity
{
    public Guid OpeningId {get;private set;}
    public Guid PropertyUnitId {get;private set;}
    public Guid PersonId {get;private set;}
    public string UnitCode {get;private set;}
    public string PersonName {get;private set;}
    public decimal Amount {get;private set;}
    public OpeningPosition(Guid tenantId,Guid condominiumId,Guid openingId,Guid propertyUnitId,Guid personId,string unitCode,string personName,decimal amount):base(tenantId,condominiumId)
    {OpeningId=Guard.Id(openingId);PropertyUnitId=Guard.Id(propertyUnitId);PersonId=Guard.Id(personId);UnitCode=Guard.Text(unitCode);PersonName=Guard.Text(personName);Amount=Money.Validate(amount,true);}
}
public sealed class AdministrativeClosure : CondominiumEntity
{
    public Guid AccountingPeriodId {get;private set;}
    public Guid StatementId {get;private set;}
    public Guid RequestId {get;private set;}
    public string RequestHash {get;private set;}
    public Guid ClosedBy {get;private set;}
    public string SnapshotJson {get;private set;}
    public AdministrativeClosure(Guid tenantId,Guid condominiumId,Guid accountingPeriodId,Guid statementId,Guid requestId,string requestHash,Guid closedBy,string snapshotJson):base(tenantId,condominiumId)
    {AccountingPeriodId=Guard.Id(accountingPeriodId);StatementId=Guard.Id(statementId);RequestId=Guard.Id(requestId);RequestHash=Guard.Text(requestHash,64);ClosedBy=Guard.Id(closedBy);SnapshotJson=Guard.Text(snapshotJson,20000000);}
}
