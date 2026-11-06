using CondoManager.Domain;
namespace CondoManager.Application;
public record VotingRuleInput(string Code,string Description,VotingCriteria Criteria,DateOnly EffectiveFrom,DateOnly? EffectiveTo,Guid? PreviousVersionId=null);
public record RuleReviewInput(string ReviewNote);
public record VotingRuleDto(Guid Id,string Code,string Description,int Version,Guid? PreviousVersionId,VotingRuleStatus Status,DateOnly EffectiveFrom,DateOnly? EffectiveTo,VotingCriteria Criteria,string? ReviewNote,Guid? ReviewedBy,DateTimeOffset? ReviewedAt);
public record ElectorateExclusion(Guid PropertyUnitId,string Reason);
public record ElectorateAssignment(Guid PropertyUnitId,Guid PersonId,string Reason);
public record ElectorateOptions(string Acknowledgement,IReadOnlyList<ElectorateExclusion> Exclusions,IReadOnlyList<ElectorateAssignment>? Assignments=null,Guid? AdministratorPersonId=null);
public record QuorumInput(int ExpectedVersion,Guid SessionId,Guid RuleId,ElectorateOptions Options,DateTimeOffset? At=null);
public record QuorumDto(Guid Id,Guid SessionId,Guid RuleId,DateTimeOffset At,QuorumOutcome Outcome,VotingElectorate Electorate,VotingEvaluation Evaluation);
public record OpenVoteInput(int ExpectedVersion,Guid RequestId,Guid SessionId,Guid AgendaItemId,Guid RuleId,Guid QuorumCheckId,ElectorateOptions Options);
public record BallotInput(int ExpectedVersion,string HeadKey,BallotChoice Choice,Guid? BallotId=null,int? BallotRevision=null);
public record VoteCommand(int ExpectedVersion);
public record BallotDto(Guid Id,string HeadKey,Guid PrincipalId,Guid ExpressedById,decimal Millesimals,BallotChoice Choice,int Revision,Guid RecordedBy,DateTimeOffset RecordedAt);
public record TargetEvidence(MeetingReference Type,Guid Id,int Version,string Status,string Fingerprint,string ContentJson);
public record VoteEvidence(VotingCriteria Criteria,VotingElectorate Electorate,TargetEvidence? Target,QuorumDto? OpeningQuorum=null);
public record VoteDto(Guid Id,Guid SessionId,Guid AgendaItemId,Guid RuleId,int RuleVersion,Guid QuorumCheckId,MeetingVoteStatus Status,DateTimeOffset OpenedAt,DateTimeOffset? ClosedAt,string SourceFingerprint,VoteEvidence Evidence,IReadOnlyList<BallotDto> Ballots,VotingEvaluation Evaluation);
public record ResolutionInput(int ExpectedVersion,Guid RequestId,Guid VoteId,string Title,string ResolutionText,Guid? PreviousResolutionId=null,string? CorrectionReason=null);
public record ResolutionDto(Guid Id,Guid VoteId,Guid AgendaItemId,int Version,Guid? PreviousResolutionId,string? CorrectionReason,string Title,string ResolutionText,VoteOutcome Outcome,DateTimeOffset FinalizedAt,Guid FinalizedBy,string ContentJson);
public record ApplyResolutionInput(int ExpectedVersion,Guid RequestId,bool HistoricalOnly=false);
public record ApplicationDto(Guid Id,Guid ResolutionId,MeetingReference RelatedType,Guid RelatedId,bool HistoricalOnly,DateTimeOffset AppliedAt,Guid AppliedBy,string ContentJson);
public record DispositionInput(int ExpectedVersion,Guid AgendaItemId,AgendaDisposition Disposition,string Reason);
public record DispositionDto(Guid AgendaItemId,AgendaDisposition Disposition,string Reason);
public record CompletionCheck(string Code,string Message,bool Blocking);
public record CompletionReview(Guid SessionId,bool CanComplete,IReadOnlyList<CompletionCheck> Checks,string Fingerprint);
public record CompleteMeetingInput(int ExpectedVersion,Guid RequestId,Guid SessionId,string Fingerprint,string Reason);
public record CompletionDto(Guid Id,Guid SessionId,DateTimeOffset CompletedAt,Guid CompletedBy,string ContentJson);
public record BallotHistoryDto(Guid BallotId,int Version,BallotChoice Choice,Guid ActorId,DateTimeOffset At);
public record DecisionsDto(int MeetingVersion,IReadOnlyList<QuorumDto> Quorums,IReadOnlyList<VoteDto> Votes,IReadOnlyList<ResolutionDto> Resolutions,IReadOnlyList<ApplicationDto> Applications,IReadOnlyList<DispositionDto> Dispositions,CompletionDto? Completion,IReadOnlyList<BallotHistoryDto>? BallotHistory=null);
public interface IMeetingDecisionService
{
 Task<IReadOnlyList<VotingRuleDto>> Rules(Guid condo,CancellationToken ct);
 Task<VotingRuleDto> SaveRule(Guid condo,VotingRuleInput input,CancellationToken ct);
 Task<VotingRuleDto> ReviewRule(Guid condo,Guid id,RuleReviewInput? input,CancellationToken ct);
 Task<DecisionsDto> Decisions(Guid condo,Guid meeting,CancellationToken ct);
 Task<DecisionsDto> CheckQuorum(Guid condo,Guid meeting,QuorumInput input,CancellationToken ct);
 Task<DecisionsDto> OpenVote(Guid condo,Guid meeting,OpenVoteInput input,CancellationToken ct);
 Task<DecisionsDto> Ballot(Guid condo,Guid meeting,Guid vote,BallotInput input,CancellationToken ct);
 Task<DecisionsDto> CloseVote(Guid condo,Guid meeting,Guid vote,VoteCommand input,bool cancel,CancellationToken ct);
 Task<DecisionsDto> Resolve(Guid condo,Guid meeting,ResolutionInput input,CancellationToken ct);
 Task<DecisionsDto> ApplyResolution(Guid condo,Guid meeting,Guid resolution,ApplyResolutionInput input,CancellationToken ct);
 Task<DecisionsDto> DisposeAgenda(Guid condo,Guid meeting,DispositionInput input,CancellationToken ct);
 Task<CompletionReview> ReviewCompletion(Guid condo,Guid meeting,Guid session,CancellationToken ct);
 Task<DecisionsDto> CompleteMeeting(Guid condo,Guid meeting,CompleteMeetingInput input,CancellationToken ct);
}
public static class VotingRuleTemplates
{
 public record Template(string Code,string Description,VotingCriteria Criteria);
 public static IReadOnlyList<Template> All()=>[
 new("CC1136-CONST-1","Costitutivo prima — riferimento art.1136 c.1",new(VotingPurpose.Constitutive,MeetingCall.First,null,new(1,2,VotingDenominator.Eligible,ThresholdComparison.GreaterThan),new(2,3,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.1 c.c.; L.220/2012 art.14","Ambito generale da verificare; non attestazione di conformità.")),
 new("CC1136-CONST-2","Costitutivo seconda — riferimento art.1136 c.3",new(VotingPurpose.Constitutive,MeetingCall.Second,null,new(1,3,VotingDenominator.Eligible,ThresholdComparison.AtLeast),new(1,3,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.3 c.c.; L.220/2012 art.14","Ambito generale da verificare; non attestazione di conformità.")),
 new("CC1136-ORD-1","Deliberativo ordinario prima",new(VotingPurpose.Deliberative,MeetingCall.First,AgendaCategory.General,new(1,2,VotingDenominator.Present,ThresholdComparison.GreaterThan),new(1,2,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.2 c.c.; L.220/2012 art.14","Solo materia ordinaria verificata; non applicare automaticamente a categorie speciali.")),
 new("CC1136-ORD-2","Deliberativo ordinario seconda",new(VotingPurpose.Deliberative,MeetingCall.Second,AgendaCategory.General,new(1,2,VotingDenominator.Present,ThresholdComparison.GreaterThan),new(1,3,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.3 c.c.; L.220/2012 art.14","Solo materia ordinaria verificata; non applicare automaticamente a categorie speciali.")),
 new("CC1136-SPECIAL-4","Modello c.4 / art.1138 da classificare",new(VotingPurpose.Deliberative,MeetingCall.Second,null,new(1,2,VotingDenominator.Present,ThresholdComparison.GreaterThan),new(1,2,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.4 e art.1138 c.c.; L.220/2012 artt.14,16","Classificazione e convocazione da verificare; nessuna selezione automatica per materia.")),
 new("CC1136-SPECIAL-5","Modello c.5 da classificare",new(VotingPurpose.Deliberative,MeetingCall.Second,null,new(1,2,VotingDenominator.Present,ThresholdComparison.GreaterThan),new(2,3,VotingDenominator.Building,ThresholdComparison.AtLeast),"Art.1136 c.5 c.c.; L.220/2012 art.14","Solo ambito espressamente verificato, maggioranze derogatorie non presunte."))];
}
