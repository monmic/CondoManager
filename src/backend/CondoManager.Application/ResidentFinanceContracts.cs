using CondoManager.Domain;
namespace CondoManager.Application;
public enum ScheduleMode { Equal, Percentages, Amounts }
public record DebtorSelection(Guid PropertyUnitId,Guid PersonId,decimal Share);
public record PlanInput(Guid BudgetId,DateOnly DebtorDate,ScheduleMode Mode,IReadOnlyList<InstallmentSchedule> Schedule,IReadOnlyList<DebtorSelection> Debtors,Guid RequestId,Guid? PreviousPlanId=null);
public record PlanDifference(Guid PropertyUnitId,Guid DebtorId,decimal PreviousAmount,decimal NewAmount,decimal Difference);
public record PlanPreview(string Hash,decimal Total,IReadOnlyList<IndividualQuota> Quotas,IReadOnlyList<CalculatedInstallment> Installments,IReadOnlyList<PlanDifference> Differences,string EvidenceJson);
public record PlanDto(Guid Id,Guid AccountingPeriodId,Guid BudgetId,int Version,Guid? PreviousPlanId,InstallmentPlanStatus Status,decimal Total,Guid? JournalEntryId,Guid? ApprovedBy,DateTimeOffset? ApprovedAt,PlanInput Input,PlanPreview Snapshot);
public record PlanApproval(Guid RequestId,DateOnly Date,string PreviewHash);
public record CreditDto(Guid Id,Guid PlanId,Guid AccountingPeriodId,Guid PropertyUnitId,string UnitCode,Guid DebtorId,string DebtorName,DateOnly DueDate,decimal Amount,decimal Paid,decimal Residual,string Status,Guid? JournalEntryId);
public record ResidentReceiptInput(Guid RequestId,Guid AccountingPeriodId,Guid FinancialAccountId,DateOnly PaymentDate,decimal Amount,string? Reference,Guid? DebtorId=null,Guid? PayerId=null,bool RecordOnly=false,Guid? ExistingJournalId=null);
public record ReceiptAllocationInput(Guid InstallmentId,decimal Amount);
public record AllocateReceiptInput(Guid RequestId,Guid AccountingPeriodId,DateOnly Date,IReadOnlyList<ReceiptAllocationInput> Allocations);
public record ReceiptAllocationDto(Guid Id,Guid InstallmentId,decimal Amount,Guid JournalEntryId,Guid? ReversesAllocationId,bool IsReversed);
public record ResidentReceiptDto(Guid Id,Guid AccountingPeriodId,Guid FinancialAccountId,DateOnly PaymentDate,decimal Amount,string? Reference,Guid? DebtorId,Guid? PayerId,string Status,Guid? BankJournalId,decimal Allocated,decimal Unallocated,IReadOnlyList<ReceiptAllocationDto> Allocations);
public record BankMatchInput(Guid JournalEntryId);
public record ResidentBalance(Guid PropertyUnitId,string UnitCode,Guid DebtorId,string DebtorName,decimal Issued,decimal Allocated,decimal Residual,decimal Overdue,decimal NotDue,decimal Opening=0,decimal DefinitiveAdjustment=0);
public record FinanceBalances(DateOnly AsOf,IReadOnlyList<ResidentBalance> Positions,decimal Issued,decimal Allocated,decimal Residual,decimal Unattributed,decimal Unmatched);
public interface IResidentFinanceService
{
    Task<PlanPreview> PreviewPlan(Guid condo,PlanInput input,CancellationToken ct);
    Task<PlanDto> CreatePlan(Guid condo,PlanInput input,CancellationToken ct);
    Task<IReadOnlyList<PlanDto>> Plans(Guid condo,Guid? period,CancellationToken ct);
    Task<PlanDto> ApprovePlan(Guid condo,Guid id,PlanApproval input,CancellationToken ct);
    Task<IReadOnlyList<CreditDto>> Credits(Guid condo,Guid? period,DateOnly? asOf,CancellationToken ct);
    Task<ResidentReceiptDto> RecordReceipt(Guid condo,ResidentReceiptInput input,CancellationToken ct);
    Task<IReadOnlyList<ResidentReceiptDto>> ResidentReceipts(Guid condo,Guid? period,CancellationToken ct);
    Task<ResidentReceiptDto> AllocateReceipt(Guid condo,Guid id,AllocateReceiptInput input,CancellationToken ct);
    Task<ResidentReceiptDto> ReverseAllocation(Guid condo,Guid journalId,ReversalInput input,CancellationToken ct);
    Task<IReadOnlyList<JournalDto>> AvailableBankMovements(Guid condo,CancellationToken ct);
    Task<ResidentReceiptDto> MatchReceipt(Guid condo,Guid id,BankMatchInput input,bool confirm,CancellationToken ct);
    Task<FinanceBalances> ResidentBalances(Guid condo,Guid? period,DateOnly? asOf,CancellationToken ct);
}
