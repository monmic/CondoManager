using CondoManager.Domain;
namespace CondoManager.Application;
public record RuleComponentInput(AllocationCriterion Criterion,decimal Percentage,Guid? TableId,Guid? GroupId,string? ConsumptionType,string? UnitOfMeasure,bool NormalizeAdmitted=false);
public record RuleInput(string Name,string? Description,AllocationCriterion Type,IReadOnlyList<RuleComponentInput> Components);
public record RuleComponentDto(int Position,AllocationCriterion Criterion,decimal Percentage,Guid? TableId,Guid? GroupId,string? ConsumptionType,string? UnitOfMeasure,bool NormalizeAdmitted);
public record RuleDto(Guid Id,string Name,string? Description,AllocationCriterion Type,AllocationRuleStatus Status,int Version,Guid? PreviousVersionId,IReadOnlyList<RuleComponentDto> Components);
public record GroupInput(string Name,bool IsActive,IReadOnlyList<Guid> UnitIds);
public record GroupDto(Guid Id,string Name,bool IsActive,IReadOnlyList<Guid> UnitIds);
public record ReadingInput(Guid PropertyUnitId,Guid AccountingPeriodId,string ConsumptionType,string? MeterCode,string UnitOfMeasure,DateOnly Date,ReadingBoundary Boundary,decimal Value,bool IsEstimated,string? Reason,Guid? ReplacesReadingId);
public record ReadingDto(Guid Id,Guid PropertyUnitId,Guid AccountingPeriodId,string ConsumptionType,string MeterCode,string UnitOfMeasure,DateOnly Date,ReadingBoundary Boundary,decimal Value,bool IsEstimated,string? Reason,Guid? ReplacesReadingId,bool IsReplaced);
public record UnitConsumptionDto(Guid PropertyUnitId,string UnitCode,ConsumptionResult Result);
public record AssignmentInput(Guid? AccountId,Guid? ExpenseDocumentId,Guid RuleId);
public record AssignmentDto(Guid Id,Guid? AccountId,Guid? ExpenseDocumentId,Guid RuleId);
public record DirectChargeInput(Guid PropertyUnitId,Guid? PersonId,decimal Amount);
public record AllocationSourceInput(Guid ExpenseDocumentId,decimal Amount,Guid? RuleId,IReadOnlyList<Guid>? ExcludedUnitIds=null,IReadOnlyList<DirectChargeInput>? DirectCharges=null);
public record AllocationPreviewInput(Guid AccountingPeriodId,DateOnly CompetenceFrom,DateOnly CompetenceTo,IReadOnlyList<AllocationSourceInput> Sources,Guid? PreviousRunId=null);
public record AllocationPreviewDto(string PreviewHash,AllocationCalculationInput Snapshot,AllocationCalculationResult Result);
public record AllocationConfirmInput(Guid RequestId,string PreviewHash,AllocationPreviewInput Input);
public record AllocationRunDto(Guid Id,Guid AccountingPeriodId,int Version,Guid? PreviousRunId,Guid? ReplacedByRunId,Guid ConfirmedByUserId,DateTimeOffset ConfirmedAt,DateOnly CompetenceFrom,DateOnly CompetenceTo,decimal TotalAmount,string EngineVersion,string PreviewHash,AllocationPreviewInput Input,AllocationCalculationInput Snapshot,AllocationCalculationResult Result,IReadOnlyList<string> SourceWarnings);
public interface IAllocationService
{
    Task<IReadOnlyList<RuleDto>> Rules(Guid condo,CancellationToken ct);
    Task<RuleDto> SaveRule(Guid condo,Guid? id,RuleInput input,CancellationToken ct);
    Task<RuleDto> ActivateRule(Guid condo,Guid id,CancellationToken ct);
    Task<IReadOnlyList<GroupDto>> Groups(Guid condo,CancellationToken ct);
    Task<GroupDto> SaveGroup(Guid condo,Guid? id,GroupInput input,CancellationToken ct);
    Task<IReadOnlyList<ReadingDto>> Readings(Guid condo,Guid? period,CancellationToken ct);
    Task<ReadingDto> AddReading(Guid condo,ReadingInput input,CancellationToken ct);
    Task<IReadOnlyList<UnitConsumptionDto>> Consumptions(Guid condo,Guid period,string type,string unitOfMeasure,DateOnly from,DateOnly to,CancellationToken ct);
    Task<IReadOnlyList<AssignmentDto>> Assignments(Guid condo,CancellationToken ct);
    Task<AssignmentDto> SaveAssignment(Guid condo,AssignmentInput input,CancellationToken ct);
    Task<AllocationPreviewDto> Preview(Guid condo,AllocationPreviewInput input,CancellationToken ct);
    Task<AllocationRunDto> Confirm(Guid condo,AllocationConfirmInput input,CancellationToken ct);
    Task<IReadOnlyList<AllocationRunDto>> Runs(Guid condo,Guid? period,CancellationToken ct);
    Task<AllocationRunDto> Run(Guid condo,Guid id,CancellationToken ct);
}
