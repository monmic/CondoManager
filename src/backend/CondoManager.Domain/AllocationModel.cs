namespace CondoManager.Domain;
public enum AllocationCriterion { Millesimal, EqualShares, Consumption, Mixed, DirectCharge }
public enum AllocationRuleStatus { Draft, Active }
public enum ReadingBoundary { Initial, Final }
public abstract class CondominiumEntity : TenantEntity
{
    public Guid CondominiumId { get; private set; }
    protected CondominiumEntity(Guid tenantId, Guid condominiumId) : base(tenantId) => CondominiumId=Guard.Id(condominiumId);
}
public sealed class AllocationRule : CondominiumEntity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AllocationCriterion Type { get; private set; }
    public AllocationRuleStatus Status { get; private set; }
    public int Version { get; private set; }
    public Guid? PreviousVersionId { get; private set; }
    public AllocationRule(Guid tenantId,Guid condominiumId,string name,AllocationCriterion type,int version=1,Guid? previousVersionId=null) : base(tenantId,condominiumId)
    { Name=Guard.Text(name);Type=Guard.EnumValue(type);Version=version>0?version:throw new ArgumentException("Versione non valida.");PreviousVersionId=previousVersionId; }
    public void EnsureDraft() { if(Status!=AllocationRuleStatus.Draft) throw new InvalidOperationException("Regola attiva immutabile: creare una nuova versione."); }
    public void Update(string name,string? description,AllocationCriterion type) { EnsureDraft();Name=Guard.Text(name);Description=Guard.Optional(description);Type=Guard.EnumValue(type); }
    public void Validate(IReadOnlyCollection<AllocationRuleComponent> components)
    {
        if(components.Count is <1 or >20 || components.Any(c=>c.RuleId!=Id || c.TenantId!=TenantId || c.CondominiumId!=CondominiumId) || components.Sum(c=>c.Percentage)!=100 || components.Select(c=>c.Position).Distinct().Count()!=components.Count) throw new ArgumentException("Componenti non validi: pesi totali esattamente 100%.");
        if(Type==AllocationCriterion.Mixed ? components.Count<2 || components.Any(c=>c.Criterion==AllocationCriterion.DirectCharge) : components.Count!=1 || components.Single().Criterion!=Type) throw new ArgumentException("Tipologia della regola e componenti non coerenti.");
    }
    public void Activate(IReadOnlyCollection<AllocationRuleComponent> components) { EnsureDraft();Validate(components);Status=AllocationRuleStatus.Active; }
}
public sealed class AllocationRuleComponent : CondominiumEntity
{
    public Guid RuleId { get; private set; }
    public int Position { get; private set; }
    public AllocationCriterion Criterion { get; private set; }
    public decimal Percentage { get; private set; }
    public Guid? TableId { get; private set; }
    public Guid? GroupId { get; private set; }
    public string? ConsumptionType { get; private set; }
    public string? UnitOfMeasure { get; private set; }
    public bool NormalizeAdmitted { get; private set; }
    public AllocationRuleComponent(Guid tenantId,Guid condominiumId,Guid ruleId,int position,AllocationCriterion criterion,decimal percentage,Guid? tableId,Guid? groupId,string? consumptionType,string? unitOfMeasure,bool normalizeAdmitted) : base(tenantId,condominiumId)
    {
        RuleId=Guard.Id(ruleId);Position=position>=0?position:throw new ArgumentException("Posizione non valida.");Criterion=Guard.EnumValue(criterion);
        Percentage=Guard.Decimal(percentage,6,0.000001m,100);TableId=tableId;GroupId=groupId;ConsumptionType=Guard.Optional(consumptionType)?.ToUpperInvariant();UnitOfMeasure=Guard.Optional(unitOfMeasure)?.ToUpperInvariant();NormalizeAdmitted=normalizeAdmitted;
        if(criterion==AllocationCriterion.Mixed || (criterion==AllocationCriterion.Millesimal)!=(tableId!=null) || (criterion==AllocationCriterion.Consumption)!=(ConsumptionType!=null && UnitOfMeasure!=null) || (criterion!=AllocationCriterion.Consumption && (ConsumptionType!=null || UnitOfMeasure!=null)) || (normalizeAdmitted && criterion!=AllocationCriterion.Millesimal)) throw new ArgumentException("Parametri del componente non coerenti con il criterio.");
    }
}
public sealed class AllocationGroup : CondominiumEntity
{
    public string Name { get; private set; }
    public bool IsActive { get; private set; }=true;
    public AllocationGroup(Guid tenantId,Guid condominiumId,string name) : base(tenantId,condominiumId) => Name=Guard.Text(name);
    public void Update(string name,bool isActive) { Name=Guard.Text(name);IsActive=isActive; }
}
public sealed class AllocationGroupMember : CondominiumEntity
{
    public Guid GroupId { get; private set; }
    public Guid PropertyUnitId { get; private set; }
    public AllocationGroupMember(Guid tenantId,Guid condominiumId,Guid groupId,Guid propertyUnitId) : base(tenantId,condominiumId) { GroupId=Guard.Id(groupId);PropertyUnitId=Guard.Id(propertyUnitId); }
}
public sealed class ConsumptionReading : CondominiumEntity
{
    public Guid PropertyUnitId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }
    public string ConsumptionType { get; private set; }
    public string MeterCode { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public DateOnly Date { get; private set; }
    public ReadingBoundary Boundary { get; private set; }
    public decimal Value { get; private set; }
    public bool IsEstimated { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ReplacesReadingId { get; private set; }
    public ConsumptionReading(Guid tenantId,Guid condominiumId,Guid propertyUnitId,Guid accountingPeriodId,string consumptionType,string? meterCode,string unitOfMeasure,DateOnly date,ReadingBoundary boundary,decimal value,bool isEstimated,string? reason,Guid? replacesReadingId) : base(tenantId,condominiumId)
    {
        PropertyUnitId=Guard.Id(propertyUnitId);AccountingPeriodId=Guard.Id(accountingPeriodId);ConsumptionType=Guard.Text(consumptionType).ToUpperInvariant();MeterCode=Guard.Optional(meterCode)?.ToUpperInvariant()??"UNSPECIFIED";UnitOfMeasure=Guard.Text(unitOfMeasure).ToUpperInvariant();Guard.Dates(date,null);Date=date;Boundary=Guard.EnumValue(boundary);Value=Guard.Decimal(value,6,0,999999999999.999999m);IsEstimated=isEstimated;Reason=Guard.Optional(reason);ReplacesReadingId=replacesReadingId;
        if((isEstimated || replacesReadingId.HasValue) && Reason==null) throw new ArgumentException("Motivo obbligatorio per stime e rettifiche.");
    }
}
public sealed class AllocationAssignment : CondominiumEntity
{
    public Guid? AccountId { get; private set; }
    public Guid? ExpenseDocumentId { get; private set; }
    public Guid RuleId { get; private set; }
    public AllocationAssignment(Guid tenantId,Guid condominiumId,Guid? accountId,Guid? expenseDocumentId,Guid ruleId) : base(tenantId,condominiumId)
    { if((accountId!=null)==(expenseDocumentId!=null)) throw new ArgumentException("Indicare categoria oppure documento.");AccountId=accountId;ExpenseDocumentId=expenseDocumentId;RuleId=Guard.Id(ruleId); }
    public void SetRule(Guid ruleId) => RuleId=Guard.Id(ruleId);
}
public sealed class AllocationRun : CondominiumEntity
{
    public Guid AccountingPeriodId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; }
    public string PreviewHash { get; private set; }
    public string EngineVersion { get; private set; }
    public int Version { get; private set; }
    public Guid? PreviousRunId { get; private set; }
    public Guid ConfirmedByUserId { get; private set; }
    public DateOnly CompetenceFrom { get; private set; }
    public DateOnly CompetenceTo { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string InputJson { get; private set; }
    public string SnapshotJson { get; private set; }
    public AllocationRun(Guid tenantId,Guid condominiumId,Guid accountingPeriodId,Guid requestId,string requestHash,string previewHash,string engineVersion,int version,Guid? previousRunId,Guid confirmedByUserId,DateOnly competenceFrom,DateOnly competenceTo,decimal totalAmount,string inputJson,string snapshotJson) : base(tenantId,condominiumId)
    { AccountingPeriodId=Guard.Id(accountingPeriodId);RequestId=Guard.Id(requestId);RequestHash=Guard.Text(requestHash,64);PreviewHash=Guard.Text(previewHash,64);EngineVersion=Guard.Text(engineVersion);Version=version;PreviousRunId=previousRunId;ConfirmedByUserId=Guard.Id(confirmedByUserId);Guard.Dates(competenceFrom,competenceTo);CompetenceFrom=competenceFrom;CompetenceTo=competenceTo;TotalAmount=Money.Positive(totalAmount);InputJson=inputJson;SnapshotJson=snapshotJson; }
}
public sealed class AllocationRunSource : CondominiumEntity
{
    public Guid RunId { get; private set; }
    public Guid ExpenseDocumentId { get; private set; }
    public Guid RuleId { get; private set; }
    public int Position { get; private set; }
    public decimal Amount { get; private set; }
    public AllocationRunSource(Guid tenantId,Guid condominiumId,Guid runId,Guid expenseDocumentId,Guid ruleId,int position,decimal amount) : base(tenantId,condominiumId)
    { RunId=Guard.Id(runId);ExpenseDocumentId=Guard.Id(expenseDocumentId);RuleId=Guard.Id(ruleId);Position=position;Amount=Money.Positive(amount); }
}
public sealed class AllocationRunLine : CondominiumEntity
{
    public Guid RunId { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid PropertyUnitId { get; private set; }
    public Guid? PersonId { get; private set; }
    public int Position { get; private set; }
    public int ComponentPosition { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal UnroundedAmount { get; private set; }
    public decimal RoundingAdjustment { get; private set; }
    public AllocationRunLine(Guid tenantId,Guid condominiumId,Guid runId,Guid sourceId,Guid propertyUnitId,Guid? personId,int position,int componentPosition,decimal amount,decimal baseAmount,decimal unroundedAmount,decimal roundingAdjustment) : base(tenantId,condominiumId)
    { RunId=Guard.Id(runId);SourceId=Guard.Id(sourceId);PropertyUnitId=Guard.Id(propertyUnitId);PersonId=personId;Position=position;ComponentPosition=componentPosition;Amount=Money.Validate(amount);BaseAmount=Money.Validate(baseAmount);UnroundedAmount=unroundedAmount;RoundingAdjustment=roundingAdjustment; }
}
