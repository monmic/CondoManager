using System.Numerics;
using CondoManager.Domain;
namespace CondoManager.Application;
public sealed record ElectoralRight(string HeadKey,Guid PrincipalId,string PrincipalName,Guid? ExpressedById,string? ExpressedByName,IReadOnlyList<Guid> PropertyUnitIds,decimal Millesimals,bool IsPresent);
public sealed record VotingElectorate(Guid SnapshotId,DateTimeOffset At,decimal BuildingValue,IReadOnlyList<ElectoralRight> Rights,IReadOnlyList<ElectorateExclusion> Exclusions,IReadOnlyList<string> Anomalies,string Acknowledgement,string Fingerprint,ElectorateOptions? Options=null,string? EvidenceJson=null);
public sealed record CastBallot(string HeadKey,BallotChoice Choice);
public sealed record VotingTotals(int Eligible,int Present,int InFavor,int Against,int Abstained,int NonVoting,decimal EligibleValue,decimal PresentValue,decimal InFavorValue,decimal AgainstValue,decimal AbstainedValue,decimal NonVotingValue,int Absent,decimal AbsentValue);
public sealed record ThresholdCheck(decimal Actual,decimal Basis,long Numerator,long Denominator,ThresholdComparison Comparison,bool Satisfied);
public sealed record VotingEvaluation(string EngineVersion,VotingTotals Totals,ThresholdCheck Heads,ThresholdCheck Value,VoteOutcome Outcome,string Reason,IReadOnlyList<string> Anomalies);
public interface IVotingEngine{VotingEvaluation Evaluate(VotingCriteria rule,VotingElectorate electorate,IReadOnlyList<CastBallot> ballots);}
public sealed class VotingEngine:IVotingEngine
{
 public const string Version="M07.2-1";
 public static bool Meets(decimal actual,decimal basis,VotingThreshold t){static BigInteger Scaled(decimal n){Guard.Decimal(n,6,0,999999999999999999m);return new BigInteger(n*1000000m);}var left=Scaled(actual)*t.Denominator;var right=Scaled(basis)*t.Numerator;return t.Comparison==ThresholdComparison.AtLeast?left>=right:left>right;}
 public VotingEvaluation Evaluate(VotingCriteria rule,VotingElectorate electorate,IReadOnlyList<CastBallot> ballots)
 {
  rule.Validate();var errors=electorate.Anomalies.ToList();var rights=electorate.Rights;var present=rights.Where(r=>r.IsPresent).ToArray();
  if(rights.Count==0)errors.Add("Platea vuota.");if(rights.GroupBy(r=>r.HeadKey).Any(g=>g.Count()>1))errors.Add("Testa duplicata.");if(rights.SelectMany(r=>r.PropertyUnitIds).GroupBy(id=>id).Any(g=>g.Count()>1))errors.Add("Unità rappresentata più volte.");if(rights.Any(r=>r.Millesimals<0||r.PropertyUnitIds.Count==0||r.PrincipalId==Guid.Empty||(r.IsPresent&&r.ExpressedById==null)))errors.Add("Diritto incoerente.");
  if(ballots.GroupBy(b=>b.HeadKey).Any(g=>g.Count()>1)||ballots.Any(b=>!Enum.IsDefined(b.Choice)||!present.Any(r=>r.HeadKey==b.HeadKey)))errors.Add("Voto duplicato o privo di diritto presente.");
  var valid=ballots.GroupBy(b=>b.HeadKey).Select(g=>g.First()).Where(b=>present.Any(r=>r.HeadKey==b.HeadKey)).ToArray();
  int Count(BallotChoice c)=>valid.Count(b=>b.Choice==c);decimal Value(BallotChoice c)=>present.Where(r=>valid.Any(b=>b.HeadKey==r.HeadKey&&b.Choice==c)).Sum(r=>r.Millesimals);
  var yes=Count(BallotChoice.InFavor);var no=Count(BallotChoice.Against);var abstained=Count(BallotChoice.Abstained);var yv=Value(BallotChoice.InFavor);var nv=Value(BallotChoice.Against);var av=Value(BallotChoice.Abstained);var ev=rights.Sum(r=>r.Millesimals);var pv=present.Sum(r=>r.Millesimals);
  var totals=new VotingTotals(rights.Count,present.Length,yes,no,abstained,present.Length-yes-no-abstained,ev,pv,yv,nv,av,pv-yv-nv-av,rights.Count-present.Length,ev-pv);
  decimal Basis(VotingDenominator d,bool head)=>d switch{VotingDenominator.Eligible=>head?rights.Count:ev,VotingDenominator.Present=>head?present.Length:pv,VotingDenominator.Expressed=>head?yes+no:yv+nv,VotingDenominator.Building=>electorate.BuildingValue,_=>0};
  var hb=Basis(rule.Heads.Basis,true);var vb=Basis(rule.Value.Basis,false);var ha=rule.Purpose==VotingPurpose.Constitutive?present.Length:yes;var va=rule.Purpose==VotingPurpose.Constitutive?pv:yv;
  if(hb<=0||vb<=0)errors.Add("Denominatore nullo o non determinato.");var h=new ThresholdCheck(ha,hb,rule.Heads.Numerator,rule.Heads.Denominator,rule.Heads.Comparison,hb>0&&Meets(ha,hb,rule.Heads));var v=new ThresholdCheck(va,vb,rule.Value.Numerator,rule.Value.Denominator,rule.Value.Comparison,vb>0&&Meets(va,vb,rule.Value));
  var outcome=errors.Count>0?VoteOutcome.Undetermined:h.Satisfied&&v.Satisfied?VoteOutcome.Approved:VoteOutcome.Rejected;return new(Version,totals,h,v,outcome,outcome==VoteOutcome.Undetermined?"Esito non determinabile: risolvere le anomalie.":outcome==VoteOutcome.Approved?"Entrambi i requisiti sono soddisfatti.":"Almeno un requisito non è soddisfatto.",errors.Distinct().ToArray());
 }
}
