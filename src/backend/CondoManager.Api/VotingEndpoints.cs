using CondoManager.Application;
namespace CondoManager.Api;
public static class VotingEndpoints
{
 public static void MapVoting(this WebApplication app)
 {
  var r=app.MapGroup("/api/meetings/condominiums/{condo:guid}/voting-rules").RequireAuthorization("TenantMember");
  r.MapGet("/",(Guid condo,IMeetingDecisionService s,CancellationToken ct)=>s.Rules(condo,ct));
  r.MapGet("/templates",()=>VotingRuleTemplates.All());
  r.MapPost("/",(Guid condo,VotingRuleInput i,IMeetingDecisionService s,CancellationToken ct)=>s.SaveRule(condo,i,ct));
  r.MapPost("/{id:guid}/activate",(Guid condo,Guid id,RuleReviewInput i,IMeetingDecisionService s,CancellationToken ct)=>s.ReviewRule(condo,id,i,ct));
  r.MapPost("/{id:guid}/retire",(Guid condo,Guid id,IMeetingDecisionService s,CancellationToken ct)=>s.ReviewRule(condo,id,null,ct));
  var g=app.MapGroup("/api/meetings/condominiums/{condo:guid}/{meeting:guid}").RequireAuthorization("TenantMember");
  g.MapGet("/decisions",(Guid condo,Guid meeting,IMeetingDecisionService s,CancellationToken ct)=>s.Decisions(condo,meeting,ct));
  g.MapPost("/quorum",(Guid condo,Guid meeting,QuorumInput i,IMeetingDecisionService s,CancellationToken ct)=>s.CheckQuorum(condo,meeting,i,ct));
  g.MapPost("/votes",(Guid condo,Guid meeting,OpenVoteInput i,IMeetingDecisionService s,CancellationToken ct)=>s.OpenVote(condo,meeting,i,ct));
  g.MapPost("/votes/{vote:guid}/ballots",(Guid condo,Guid meeting,Guid vote,BallotInput i,IMeetingDecisionService s,CancellationToken ct)=>s.Ballot(condo,meeting,vote,i,ct));
  g.MapPost("/votes/{vote:guid}/close",(Guid condo,Guid meeting,Guid vote,VoteCommand i,IMeetingDecisionService s,CancellationToken ct)=>s.CloseVote(condo,meeting,vote,i,false,ct));
  g.MapPost("/votes/{vote:guid}/cancel",(Guid condo,Guid meeting,Guid vote,VoteCommand i,IMeetingDecisionService s,CancellationToken ct)=>s.CloseVote(condo,meeting,vote,i,true,ct));
  g.MapPost("/resolutions",(Guid condo,Guid meeting,ResolutionInput i,IMeetingDecisionService s,CancellationToken ct)=>s.Resolve(condo,meeting,i,ct));
  g.MapPost("/resolutions/{resolution:guid}/apply",(Guid condo,Guid meeting,Guid resolution,ApplyResolutionInput i,IMeetingDecisionService s,CancellationToken ct)=>s.ApplyResolution(condo,meeting,resolution,i,ct));
  g.MapPost("/agenda-dispositions",(Guid condo,Guid meeting,DispositionInput i,IMeetingDecisionService s,CancellationToken ct)=>s.DisposeAgenda(condo,meeting,i,ct));
  g.MapGet("/completion-review",(Guid condo,Guid meeting,Guid session,IMeetingDecisionService s,CancellationToken ct)=>s.ReviewCompletion(condo,meeting,session,ct));
  g.MapPost("/complete",(Guid condo,Guid meeting,CompleteMeetingInput i,IMeetingDecisionService s,CancellationToken ct)=>s.CompleteMeeting(condo,meeting,i,ct));
 }
}
