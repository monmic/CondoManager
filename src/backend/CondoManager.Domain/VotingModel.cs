namespace CondoManager.Domain;
public enum VotingPurpose { Constitutive, Deliberative }
public enum VotingDenominator { Eligible, Present, Expressed, Building }
public enum ThresholdComparison { GreaterThan, AtLeast }
public enum VotingRuleStatus { Draft, Active, Retired }
public enum MeetingVoteStatus { Draft, Open, Closed, Finalized, Cancelled }
public enum BallotChoice { InFavor, Against, Abstained }
public enum QuorumOutcome { Satisfied, NotSatisfied, Undetermined }
public enum VoteOutcome { Approved, Rejected, Undetermined }
public enum AgendaDisposition { DiscussedWithoutVote, Deferred }
public sealed record VotingThreshold(long Numerator,long Denominator,VotingDenominator Basis,ThresholdComparison Comparison)
{
 public void Validate(bool heads){if(Numerator<0||Denominator<1||Numerator>Denominator||Denominator>1000000)throw new ArgumentException("Frazione soglia non valida, denominatore da 1 a 1000000.");Guard.EnumValue(Basis);Guard.EnumValue(Comparison);if(heads&&Basis==VotingDenominator.Building)throw new ArgumentException("Il denominatore edificio riguarda il valore, non le teste.");}
}
public sealed record VotingCriteria(VotingPurpose Purpose,MeetingCall Call,AgendaCategory? Category,VotingThreshold Heads,VotingThreshold Value,string LegalReference,string Scope)
{
 public void Validate(){ArgumentNullException.ThrowIfNull(Heads);ArgumentNullException.ThrowIfNull(Value);Guard.EnumValue(Purpose);Guard.EnumValue(Call);if(Category!=null)Guard.EnumValue(Category.Value);Heads.Validate(true);Value.Validate(false);Guard.Text(LegalReference,2000);Guard.Text(Scope,2000);if(Purpose==VotingPurpose.Constitutive&&(Heads.Basis==VotingDenominator.Expressed||Value.Basis==VotingDenominator.Expressed))throw new ArgumentException("Quorum costitutivo senza voti espressi.");}
}
public sealed class VotingRule:CondominiumEntity
{
 public string Code { get; private set; }
 public string Description { get; private set; }
 public int Version { get; private set; }
 public Guid? PreviousVersionId { get; private set; }
 public string CriteriaJson { get; private set; }
 public VotingRuleStatus Status { get; private set; }
 public DateOnly EffectiveFrom { get; private set; }
 public DateOnly? EffectiveTo { get; private set; }
 public Guid CreatedBy { get; private set; }
 public string? ReviewNote { get; private set; }
 public Guid? ReviewedBy { get; private set; }
 public DateTimeOffset? ReviewedAt { get; private set; }
 public VotingRule(Guid tenantId,Guid condominiumId,string code,string description,int version,Guid? previousVersionId,string criteriaJson,DateOnly effectiveFrom,DateOnly? effectiveTo,Guid createdBy):base(tenantId,condominiumId){Code=Guard.Text(code).ToUpperInvariant();Description=Guard.Text(description);Version=version;PreviousVersionId=previousVersionId;CriteriaJson=criteriaJson;Guard.Dates(effectiveFrom,effectiveTo);EffectiveFrom=effectiveFrom;EffectiveTo=effectiveTo;CreatedBy=Guard.Id(createdBy);if(version<1)throw new ArgumentException("Versione non valida.");}
 public void Activate(Guid user,string note){if(Status!=VotingRuleStatus.Draft)throw new InvalidOperationException("Criterio già revisionato.");ReviewNote=Guard.Text(note,2000);ReviewedBy=Guard.Id(user);ReviewedAt=DateTimeOffset.UtcNow;Status=VotingRuleStatus.Active;}
 public void Retire(){if(Status!=VotingRuleStatus.Active)throw new InvalidOperationException("Solo un criterio attivo può essere ritirato.");Status=VotingRuleStatus.Retired;}
}
public sealed class MeetingQuorumCheck:MeetingEntity
{
 public Guid SessionId { get; private set; }
 public Guid RuleId { get; private set; }
 public Guid SnapshotId { get; private set; }
 public DateTimeOffset CheckedAt { get; private set; }
 public Guid CheckedBy { get; private set; }
 public string ContentJson { get; private set; }
 public string Fingerprint { get; private set; }
 public MeetingQuorumCheck(Guid tenantId,Guid condominiumId,Guid meetingId,Guid sessionId,Guid ruleId,Guid snapshotId,DateTimeOffset checkedAt,Guid checkedBy,string contentJson,string fingerprint):base(tenantId,condominiumId,meetingId){SessionId=sessionId;RuleId=ruleId;SnapshotId=snapshotId;CheckedAt=checkedAt;CheckedBy=checkedBy;ContentJson=contentJson;Fingerprint=fingerprint;}
}
public sealed class MeetingVote:MeetingEntity
{
 public Guid SessionId { get; private set; }
 public Guid AgendaItemId { get; private set; }
 public Guid RuleId { get; private set; }
 public int RuleVersion { get; private set; }
 public Guid QuorumCheckId { get; private set; }
 public Guid SnapshotId { get; private set; }
 public MeetingVoteStatus Status { get; private set; }
 public DateTimeOffset OpenedAt { get; private set; }
 public DateTimeOffset? ClosedAt { get; private set; }
 public Guid CreatedBy { get; private set; }
 public Guid? FinalizedBy { get; private set; }
 public string ElectorateJson { get; private set; }
 public string SourceFingerprint { get; private set; }
 public string? ResultJson { get; private set; }
 public MeetingVote(Guid tenantId,Guid condominiumId,Guid meetingId,Guid sessionId,Guid agendaItemId,Guid ruleId,int ruleVersion,Guid quorumCheckId,Guid snapshotId,DateTimeOffset openedAt,Guid createdBy,string electorateJson,string sourceFingerprint):base(tenantId,condominiumId,meetingId){SessionId=sessionId;AgendaItemId=agendaItemId;RuleId=ruleId;RuleVersion=ruleVersion;QuorumCheckId=quorumCheckId;SnapshotId=snapshotId;OpenedAt=openedAt;CreatedBy=createdBy;ElectorateJson=electorateJson;SourceFingerprint=sourceFingerprint;Status=MeetingVoteStatus.Open;}
 public void EnsureOpen(){if(Status!=MeetingVoteStatus.Open)throw new InvalidOperationException("Votazione non aperta.");}
 public void Close(string resultJson){EnsureOpen();ResultJson=resultJson;ClosedAt=DateTimeOffset.UtcNow;Status=MeetingVoteStatus.Closed;}
 public void FinalizeVote(Guid user){if(Status!=MeetingVoteStatus.Closed)throw new InvalidOperationException("Chiudere prima la votazione.");FinalizedBy=Guard.Id(user);Status=MeetingVoteStatus.Finalized;}
 public void Cancel(){EnsureOpen();ClosedAt=DateTimeOffset.UtcNow;Status=MeetingVoteStatus.Cancelled;}
}
public sealed class MeetingBallot:MeetingEntity
{
 public Guid VoteId { get; private set; }
 public string HeadKey { get; private set; }
 public Guid PrincipalId { get; private set; }
 public Guid ExpressedById { get; private set; }
 public decimal Millesimals { get; private set; }
 public BallotChoice Choice { get; private set; }
 public int Revision { get; private set; }=1;
 public Guid RecordedBy { get; private set; }
 public DateTimeOffset RecordedAt { get; private set; }
 public MeetingBallot(Guid tenantId,Guid condominiumId,Guid meetingId,Guid voteId,string headKey,Guid principalId,Guid expressedById,decimal millesimals,BallotChoice choice,Guid recordedBy):base(tenantId,condominiumId,meetingId){VoteId=voteId;HeadKey=Guard.Text(headKey);PrincipalId=principalId;ExpressedById=expressedById;Millesimals=millesimals;Choice=Guard.EnumValue(choice);RecordedBy=recordedBy;RecordedAt=DateTimeOffset.UtcNow;}
 public void Amend(BallotChoice choice,Guid user){Choice=Guard.EnumValue(choice);RecordedBy=user;RecordedAt=DateTimeOffset.UtcNow;Revision++;}
}
public sealed class MeetingBallotRevision:MeetingEntity
{
 public Guid BallotId { get; private set; }
 public Guid VoteId { get; private set; }
 public int Version { get; private set; }
 public BallotChoice Choice { get; private set; }
 public Guid ActorId { get; private set; }
 public MeetingBallotRevision(Guid tenantId,Guid condominiumId,Guid meetingId,Guid ballotId,Guid voteId,int version,BallotChoice choice,Guid actorId):base(tenantId,condominiumId,meetingId){BallotId=ballotId;VoteId=voteId;Version=version;Choice=choice;ActorId=actorId;}
}
public sealed class MeetingResolution:MeetingEntity
{
 public Guid AgendaItemId { get; private set; }
 public Guid VoteId { get; private set; }
 public string Title { get; private set; }
 public string ResolutionText { get; private set; }
 public VoteOutcome Outcome { get; private set; }
 public Guid FinalizedBy { get; private set; }
 public int Version { get; private set; }
 public Guid? PreviousResolutionId { get; private set; }
 public string? CorrectionReason { get; private set; }
 public string ContentJson { get; private set; }
 public Guid RequestId { get; private set; }
 public string RequestHash { get; private set; }
 public MeetingResolution(Guid tenantId,Guid condominiumId,Guid meetingId,Guid agendaItemId,Guid voteId,string title,string resolutionText,VoteOutcome outcome,Guid finalizedBy,int version,Guid? previousResolutionId,string? correctionReason,string contentJson,Guid requestId,string requestHash):base(tenantId,condominiumId,meetingId){AgendaItemId=agendaItemId;VoteId=voteId;Title=Guard.Text(title);ResolutionText=Guard.Text(resolutionText,12000);Outcome=Guard.EnumValue(outcome);FinalizedBy=finalizedBy;Version=version;PreviousResolutionId=previousResolutionId;CorrectionReason=Guard.Optional(correctionReason,2000);if(previousResolutionId!=null&&CorrectionReason==null)throw new ArgumentException("Motivo rettifica obbligatorio.");ContentJson=contentJson;RequestId=Guard.Id(requestId);RequestHash=requestHash;}
}
public sealed class ResolutionApplication:MeetingEntity
{
 public Guid ResolutionId { get; private set; }
 public MeetingReference RelatedType { get; private set; }
 public Guid RelatedId { get; private set; }
 public string TargetFingerprint { get; private set; }
 public bool HistoricalOnly { get; private set; }
 public Guid AppliedBy { get; private set; }
 public Guid RequestId { get; private set; }
 public string ContentJson { get; private set; }
 public ResolutionApplication(Guid tenantId,Guid condominiumId,Guid meetingId,Guid resolutionId,MeetingReference relatedType,Guid relatedId,string targetFingerprint,bool historicalOnly,Guid appliedBy,Guid requestId,string contentJson):base(tenantId,condominiumId,meetingId){ResolutionId=resolutionId;RelatedType=relatedType;RelatedId=relatedId;TargetFingerprint=targetFingerprint;HistoricalOnly=historicalOnly;AppliedBy=appliedBy;RequestId=Guard.Id(requestId);ContentJson=contentJson;}
}
public sealed class MeetingAgendaDisposition:MeetingEntity
{
 public Guid AgendaItemId { get; private set; }
 public AgendaDisposition Disposition { get; private set; }
 public string Reason { get; private set; }
 public Guid RecordedBy { get; private set; }
 public MeetingAgendaDisposition(Guid tenantId,Guid condominiumId,Guid meetingId,Guid agendaItemId,AgendaDisposition disposition,string reason,Guid recordedBy):base(tenantId,condominiumId,meetingId){AgendaItemId=agendaItemId;Disposition=Guard.EnumValue(disposition);Reason=Guard.Text(reason,2000);RecordedBy=recordedBy;}
}
public sealed class MeetingCompletion:MeetingEntity
{
 public Guid SessionId { get; private set; }
 public Guid CompletedBy { get; private set; }
 public Guid RequestId { get; private set; }
 public string ContentJson { get; private set; }
 public string Fingerprint { get; private set; }
 public MeetingCompletion(Guid tenantId,Guid condominiumId,Guid meetingId,Guid sessionId,Guid completedBy,Guid requestId,string contentJson,string fingerprint):base(tenantId,condominiumId,meetingId){SessionId=sessionId;CompletedBy=completedBy;RequestId=Guard.Id(requestId);ContentJson=contentJson;Fingerprint=fingerprint;}
}
public sealed class MeetingDecisionRequest:MeetingEntity
{
 public Guid RequestId { get; private set; }
 public string RequestHash { get; private set; }
 public string Operation { get; private set; }
 public MeetingDecisionRequest(Guid tenantId,Guid condominiumId,Guid meetingId,Guid requestId,string requestHash,string operation):base(tenantId,condominiumId,meetingId){RequestId=Guard.Id(requestId);RequestHash=requestHash;Operation=operation;}
}
