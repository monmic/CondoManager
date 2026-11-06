using CondoManager.Domain;
namespace CondoManager.Application;
public record FinalInput(Guid AccountingPeriodId,string Description,Guid RequestId,bool UseIssuedDebtors=true,DateOnly? DebtorDate=null,IReadOnlyList<DebtorSelection>? Debtors=null,string? NoBudgetReason=null,string? Notes=null,Guid? PreviousVersionId=null);
public record StatementAnomaly(string Code,string Message,bool Blocking,Guid? SourceId=null);
public record ExpenseEvidence(Guid Id,Guid AccountId,string Category,string DocumentNumber,DateOnly DocumentDate,DateOnly PostingDate,DateOnly CompetenceFrom,DateOnly CompetenceTo,decimal Amount,decimal Allocated,IReadOnlyList<Guid> RunIds,IReadOnlyList<PaymentDto> Payments,DateTimeOffset? RegisteredAt=null);
public record BudgetComparison(Guid AccountId,string Category,decimal Budget,decimal Actual,decimal Difference,decimal? Percentage);
public record FinalPosition(Guid PropertyUnitId,string UnitCode,Guid PersonId,string PersonName,decimal Budget,decimal Actual,decimal CompetenceCorrections,decimal EconomicAdjustment,decimal Issued,decimal Allocated,decimal Opening,decimal PriorPeriodPayments,decimal PostingDelta,decimal FinancialBalance);
public record FinancialPosition(Guid PropertyUnitId,string UnitCode,Guid PersonId,string PersonName,decimal Balance);
public record FinalPreview(string Fingerprint,Guid? BudgetId,int? BudgetVersion,decimal BudgetTotal,decimal ActualTotal,decimal AllocatedTotal,decimal Difference,IReadOnlyList<ExpenseEvidence> Expenses,IReadOnlyList<BudgetComparison> Categories,IReadOnlyList<FinalPosition> Positions,IReadOnlyList<StatementAnomaly> Anomalies,string EvidenceJson);
public record FinalDto(Guid Id,Guid AccountingPeriodId,int Version,Guid? PreviousVersionId,string Description,FinalStatementStatus Status,Guid CreatedBy,DateTimeOffset CreatedAt,Guid? ApprovedBy,DateTimeOffset? ApprovedAt,string? DecisionReference,FinalInput Input,FinalPreview Snapshot);
public record FinalApproval(string Fingerprint,string? DecisionReference=null);
public record SettlementPreviewInput(DateOnly Date);
public record SettlementPreview(Guid StatementId,DateOnly Date,string Fingerprint,decimal Delta,decimal ProjectedBalance,decimal CurrentLedgerBalance,IReadOnlyList<FinalPosition> Positions,IReadOnlyList<StatementAnomaly> Anomalies);
public record SettlementConfirm(Guid RequestId,DateOnly Date,string Fingerprint);
public record SettlementDto(Guid Id,Guid StatementId,Guid AccountingPeriodId,DateOnly Date,Guid? JournalEntryId,DateTimeOffset? PostedAt,Guid? ReversedJournalId,SettlementPreview Snapshot);
public record OpeningLineInput(Guid PropertyUnitId,Guid PersonId,decimal Amount);
public record OpeningInput(Guid RequestId,OpeningMode Mode,string Reason,IReadOnlyList<OpeningLineInput>? Positions=null,Guid? SourceClosureId=null);
public record OpeningDto(Guid Id,Guid AccountingPeriodId,OpeningMode Mode,Guid? SourceClosureId,Guid? JournalEntryId,string Reason,IReadOnlyList<FinancialPosition> Positions);
public record ClosureReview(Guid AccountingPeriodId,Guid? StatementId,string Fingerprint,bool CanClose,IReadOnlyList<StatementAnomaly> Checks,IReadOnlyList<FinancialPosition> Positions,decimal LedgerBalance,decimal PositionBalance,string EvidenceJson);
public record CloseInput(Guid RequestId,string Fingerprint);
public record ClosureDto(Guid Id,Guid AccountingPeriodId,Guid StatementId,Guid ClosedBy,DateTimeOffset ClosedAt,ClosureReview Snapshot);
public interface IFinalStatementService
{
 Task<FinalPreview> PreviewFinal(Guid condo,FinalInput input,CancellationToken ct);
 Task<FinalDto> CreateFinal(Guid condo,FinalInput input,CancellationToken ct);
 Task<IReadOnlyList<FinalDto>> FinalStatements(Guid condo,Guid? period,CancellationToken ct);
 Task<FinalDto> ProposeFinal(Guid condo,Guid id,CancellationToken ct);
 Task<FinalDto> ApproveFinal(Guid condo,Guid id,FinalApproval input,CancellationToken ct);
 Task<SettlementPreview> PreviewSettlement(Guid condo,Guid id,DateOnly date,CancellationToken ct);
 Task<SettlementDto> ConfirmSettlement(Guid condo,Guid id,SettlementConfirm input,CancellationToken ct);
 Task<SettlementDto> PostSettlement(Guid condo,Guid id,CancellationToken ct);
 Task<SettlementDto> ReverseSettlement(Guid condo,Guid id,ReversalInput input,CancellationToken ct);
 Task<IReadOnlyList<SettlementDto>> Settlements(Guid condo,Guid? period,CancellationToken ct);
 Task<OpeningDto> SetOpening(Guid condo,Guid period,OpeningInput input,CancellationToken ct);
 Task<OpeningDto?> GetOpening(Guid condo,Guid period,CancellationToken ct);
 Task<ClosureReview> ReviewClosure(Guid condo,Guid period,CancellationToken ct);
 Task<ClosureDto> CloseAdministration(Guid condo,Guid period,CloseInput input,CancellationToken ct);
 Task<IReadOnlyList<ClosureDto>> Closures(Guid condo,CancellationToken ct);
}
public static class FinalCalculations
{
 public static BudgetComparison Compare(Guid id,string name,decimal budget,decimal actual)=>new(id,name,budget,actual,actual-budget,budget==0?null:decimal.Round((actual-budget)/budget*100,6,MidpointRounding.ToEven));
 public static decimal Economic(decimal budget,decimal actual,decimal correction=0)=>new SettlementAmounts(budget,actual,0,0,0,0).EconomicAdjustment+correction;
 public static decimal DeltaToPost(decimal actual,decimal issued,decimal correction=0)=>Money.Validate(actual,true)-Money.Validate(issued,true)+Money.Validate(correction,true);
 public static decimal Financial(decimal opening,decimal issued,decimal delta,decimal receipts,decimal previousReceipts=0)=>Money.Validate(opening,true)+Money.Validate(issued,true)+Money.Validate(delta,true)-Money.Validate(receipts,true)-Money.Validate(previousReceipts,true);
}
