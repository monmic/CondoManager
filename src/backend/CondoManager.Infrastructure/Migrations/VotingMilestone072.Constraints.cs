using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class VotingMilestone072
{
 private static void VotingConstraints(MigrationBuilder m)=>m.Sql("""
CREATE FUNCTION cm_voting_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE j jsonb; oldj jsonb; h record; v record; rightj jsonb;
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Voting history deletion forbidden' USING ERRCODE='55000'; END IF;
 j:=to_jsonb(NEW);oldj:=CASE WHEN TG_OP='UPDATE' THEN to_jsonb(OLD) ELSE NULL END;
 IF TG_TABLE_NAME='VotingRules' THEN
  IF TG_OP='UPDATE' AND ((j-ARRAY['UpdatedAt','Status','ReviewedBy','ReviewedAt','ReviewNote']) IS DISTINCT FROM (oldj-ARRAY['UpdatedAt','Status','ReviewedBy','ReviewedAt','ReviewNote']) OR NOT ((OLD."Status"=0 AND NEW."Status"=1 AND NEW."ReviewedBy" IS NOT NULL AND NEW."ReviewedAt" IS NOT NULL AND length(NEW."ReviewNote")>0) OR (OLD."Status"=1 AND NEW."Status"=2 AND (j-ARRAY['UpdatedAt','Status'])=(oldj-ARRAY['UpdatedAt','Status'])))) THEN RAISE EXCEPTION 'Voting rule version immutable' USING ERRCODE='55000'; END IF;RETURN NEW;
 END IF;
 SELECT * INTO h FROM "Meetings" WHERE "Id"=(j->>'MeetingId')::uuid AND "TenantId"=(j->>'TenantId')::uuid AND "CondominiumId"=(j->>'CondominiumId')::uuid FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Voting meeting scope mismatch' USING ERRCODE='23503'; END IF;
 IF h."Status" IN (4,5) AND TG_TABLE_NAME NOT IN ('MeetingCompletions','MeetingDecisionRequests') THEN RAISE EXCEPTION 'Meeting completed or cancelled' USING ERRCODE='55000'; END IF;
 IF TG_OP='UPDATE' AND TG_TABLE_NAME NOT IN ('MeetingVotes','MeetingBallots') THEN RAISE EXCEPTION 'Voting snapshot immutable' USING ERRCODE='55000'; END IF;
 IF TG_TABLE_NAME='MeetingVotes' AND TG_OP='UPDATE' THEN
  IF (j-ARRAY['UpdatedAt','Status','ClosedAt','FinalizedBy','ResultJson']) IS DISTINCT FROM (oldj-ARRAY['UpdatedAt','Status','ClosedAt','FinalizedBy','ResultJson']) OR NOT ((OLD."Status"=1 AND NEW."Status" IN (2,4)) OR (OLD."Status"=2 AND NEW."Status"=3)) THEN RAISE EXCEPTION 'Vote frozen or invalid transition' USING ERRCODE='55000'; END IF;
  IF OLD."Status"=2 AND NEW."ResultJson" IS DISTINCT FROM OLD."ResultJson" THEN RAISE EXCEPTION 'Closed result immutable' USING ERRCODE='55000'; END IF;
 END IF;
 IF TG_TABLE_NAME='MeetingBallots' THEN
  SELECT * INTO v FROM "MeetingVotes" WHERE "Id"=NEW."VoteId" AND "MeetingId"=h."Id";
  IF NOT FOUND OR v."Status"<>1 THEN RAISE EXCEPTION 'Ballot requires open vote' USING ERRCODE='23514'; END IF;
  SELECT x INTO rightj FROM jsonb_array_elements(v."ElectorateJson"->'Electorate'->'Rights') x WHERE x->>'HeadKey'=NEW."HeadKey" AND (x->>'IsPresent')::boolean;
  IF rightj IS NULL OR (rightj->>'PrincipalId')::uuid<>NEW."PrincipalId" OR (rightj->>'ExpressedById')::uuid<>NEW."ExpressedById" OR (rightj->>'Millesimals')::numeric<>NEW."Millesimals" THEN RAISE EXCEPTION 'Ballot outside frozen electorate' USING ERRCODE='23514'; END IF;
  IF TG_OP='UPDATE' THEN IF (j-ARRAY['UpdatedAt','Choice','Revision','RecordedBy','RecordedAt']) IS DISTINCT FROM (oldj-ARRAY['UpdatedAt','Choice','Revision','RecordedBy','RecordedAt']) OR NEW."Revision"<>OLD."Revision"+1 THEN RAISE EXCEPTION 'Ballot identity immutable' USING ERRCODE='55000'; END IF;END IF;
 END IF;
 RETURN NEW;
END $$;
DO $$ DECLARE tab text; BEGIN FOREACH tab IN ARRAY ARRAY['VotingRules','MeetingQuorumChecks','MeetingVotes','MeetingBallots','MeetingBallotRevisions','MeetingResolutions','ResolutionApplications','MeetingAgendaDispositions','MeetingCompletions','MeetingDecisionRequests'] LOOP EXECUTE format('CREATE TRIGGER cm_voting_guard BEFORE INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION cm_voting_guard()',tab); END LOOP; END $$;
CREATE FUNCTION cm_voting_threshold(actual numeric,basis numeric,criterion jsonb) RETURNS boolean LANGUAGE sql IMMUTABLE AS $$ SELECT basis>0 AND CASE WHEN (criterion->>'Comparison')::int=1 THEN actual*(criterion->>'Denominator')::numeric>=basis*(criterion->>'Numerator')::numeric ELSE actual*(criterion->>'Denominator')::numeric>basis*(criterion->>'Numerator')::numeric END $$;
CREATE FUNCTION cm_voting_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE j jsonb; mid uuid; v record; r record; s record; q record; a record; e jsonb; c jsonb; totalh numeric; presenth numeric; totalv numeric; presentv numeric; yes numeric; no numeric; abst numeric; yesv numeric; nov numeric; abstv numeric; hb numeric; vb numeric; hs boolean; vs boolean; expected int;
BEGIN
 j:=to_jsonb(NEW);mid:=CASE WHEN TG_TABLE_NAME='Meetings' THEN NEW."Id" ELSE (j->>'MeetingId')::uuid END;
 IF TG_TABLE_NAME='MeetingVotes' THEN
  SELECT * INTO v FROM "MeetingVotes" WHERE "Id"=NEW."Id";
  SELECT * INTO r FROM "VotingRules" WHERE "Id"=v."RuleId";SELECT * INTO s FROM "MeetingSessions" WHERE "Id"=v."SessionId";SELECT * INTO q FROM "MeetingQuorumChecks" WHERE "Id"=v."QuorumCheckId";SELECT * INTO a FROM "MeetingAgendaItems" WHERE "Id"=v."AgendaItemId";
  c:=v."ElectorateJson"->'Criteria';e:=v."ElectorateJson"->'Electorate';
  IF r."Version"<>v."RuleVersion" OR r."CriteriaJson"<>c OR (c->>'Purpose')::int<>1 OR (c->>'Call')::int<>s."Call" OR (c->>'Category' IS NOT NULL AND (c->>'Category')::int<>a."Category") OR q."SessionId"<>v."SessionId" OR q."SnapshotId"<>v."SnapshotId" OR (q."ContentJson"->>'Outcome')::int<>0 THEN RAISE EXCEPTION 'Vote rule/quorum/session mismatch' USING ERRCODE='23514'; END IF;
  IF v."Status" IN (2,3) THEN
   SELECT count(*),count(*) FILTER(WHERE (x->>'IsPresent')::boolean),COALESCE(sum((x->>'Millesimals')::numeric),0),COALESCE(sum((x->>'Millesimals')::numeric) FILTER(WHERE (x->>'IsPresent')::boolean),0) INTO totalh,presenth,totalv,presentv FROM jsonb_array_elements(e->'Rights') x;
   SELECT count(*) FILTER(WHERE "Choice"=0),count(*) FILTER(WHERE "Choice"=1),count(*) FILTER(WHERE "Choice"=2),COALESCE(sum("Millesimals") FILTER(WHERE "Choice"=0),0),COALESCE(sum("Millesimals") FILTER(WHERE "Choice"=1),0),COALESCE(sum("Millesimals") FILTER(WHERE "Choice"=2),0) INTO yes,no,abst,yesv,nov,abstv FROM "MeetingBallots" WHERE "VoteId"=v."Id";
   hb:=CASE (c->'Heads'->>'Basis')::int WHEN 0 THEN totalh WHEN 1 THEN presenth WHEN 2 THEN yes+no ELSE 0 END;vb:=CASE (c->'Value'->>'Basis')::int WHEN 0 THEN totalv WHEN 1 THEN presentv WHEN 2 THEN yesv+nov WHEN 3 THEN (e->>'BuildingValue')::numeric ELSE 0 END;
   hs:=cm_voting_threshold(yes,hb,c->'Heads');vs:=cm_voting_threshold(yesv,vb,c->'Value');expected:=CASE WHEN jsonb_array_length(e->'Anomalies')>0 OR hb<=0 OR vb<=0 THEN 2 WHEN hs AND vs THEN 0 ELSE 1 END;
   IF v."ResultJson" IS NULL OR (v."ResultJson"->>'Outcome')::int<>expected OR (v."ResultJson"->'Totals'->>'InFavor')::numeric<>yes OR (v."ResultJson"->'Totals'->>'InFavorValue')::numeric<>yesv OR (v."ResultJson"->'Totals'->>'Against')::numeric<>no OR (v."ResultJson"->'Totals'->>'Abstained')::numeric<>abst THEN RAISE EXCEPTION 'Vote result not reconciled' USING ERRCODE='23514'; END IF;
  END IF;
 END IF;
 IF TG_TABLE_NAME='MeetingResolutions' THEN
  SELECT * INTO v FROM "MeetingVotes" WHERE "Id"=NEW."VoteId";
  IF v."Status"<>3 OR v."AgendaItemId"<>NEW."AgendaItemId" OR (v."ResultJson"->>'Outcome')::int<>NEW."Outcome" THEN RAISE EXCEPTION 'Resolution inconsistent with closed vote' USING ERRCODE='23514'; END IF;
  IF NEW."PreviousResolutionId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "MeetingResolutions" prior_r WHERE prior_r."Id"=NEW."PreviousResolutionId" AND prior_r."VoteId"=NEW."VoteId" AND prior_r."Version"+1=NEW."Version" AND prior_r."Outcome"=NEW."Outcome") THEN RAISE EXCEPTION 'Invalid textual correction chain' USING ERRCODE='23514'; END IF;
 END IF;
 IF TG_TABLE_NAME='ResolutionApplications' THEN
  SELECT * INTO r FROM "MeetingResolutions" WHERE "Id"=NEW."ResolutionId";SELECT * INTO v FROM "MeetingVotes" WHERE "Id"=r."VoteId";
  IF r."Outcome"<>0 OR NEW."RelatedId" IS DISTINCT FROM (v."ElectorateJson"->'Target'->>'Id')::uuid OR NEW."RelatedType" IS DISTINCT FROM (v."ElectorateJson"->'Target'->>'Type')::int THEN RAISE EXCEPTION 'Application outside approved target' USING ERRCODE='23514'; END IF;
  IF NEW."RelatedType"=0 THEN IF NOT EXISTS(SELECT 1 FROM "Budgets" WHERE "Id"=NEW."RelatedId" AND "TenantId"=NEW."TenantId" AND "CondominiumId"=NEW."CondominiumId" AND "Status"=2) THEN RAISE EXCEPTION 'Budget not approved in scope' USING ERRCODE='23514'; END IF;
  ELSIF NEW."RelatedType"=1 THEN IF NOT EXISTS(SELECT 1 FROM "FinalStatements" WHERE "Id"=NEW."RelatedId" AND "TenantId"=NEW."TenantId" AND "CondominiumId"=NEW."CondominiumId" AND "Status"=2) THEN RAISE EXCEPTION 'Final statement not approved in scope' USING ERRCODE='23514'; END IF;
  ELSIF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" WHERE "Id"=NEW."RelatedId" AND "TenantId"=NEW."TenantId" AND "CondominiumId"=NEW."CondominiumId") THEN RAISE EXCEPTION 'Period application outside scope' USING ERRCODE='23503'; END IF;
 END IF;
 IF EXISTS(SELECT 1 FROM "MeetingVotes" active_vote JOIN "MeetingSessions" active_session ON active_session."Id"=active_vote."SessionId" WHERE active_vote."MeetingId"=mid AND active_vote."Status"=1 AND active_session."Status"<>1) THEN RAISE EXCEPTION 'Open vote requires in-progress session' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM "Meetings" WHERE "Id"=mid AND "Status"=4) THEN
  IF NOT EXISTS(SELECT 1 FROM "MeetingCompletions" WHERE "MeetingId"=mid) OR EXISTS(SELECT 1 FROM "MeetingVotes" WHERE "MeetingId"=mid AND "Status" IN (1,2)) OR EXISTS(SELECT 1 FROM "MeetingAttendances" WHERE "MeetingId"=mid AND "CheckOutAt" IS NULL) OR EXISTS(SELECT 1 FROM "MeetingAgendaItems" agenda_row WHERE agenda_row."MeetingId"=mid AND NOT EXISTS(SELECT 1 FROM "MeetingResolutions" resolution_row WHERE resolution_row."AgendaItemId"=agenda_row."Id" AND resolution_row."Outcome"<>2) AND NOT EXISTS(SELECT 1 FROM "MeetingAgendaDispositions" disposition_row WHERE disposition_row."AgendaItemId"=agenda_row."Id")) THEN RAISE EXCEPTION 'Meeting completion prerequisites missing' USING ERRCODE='23514'; END IF;
 END IF;
 RETURN NULL;
END $$;
DO $$ DECLARE tab text; BEGIN FOREACH tab IN ARRAY ARRAY['Meetings','MeetingSessions','MeetingVotes','MeetingResolutions','ResolutionApplications','MeetingCompletions'] LOOP EXECUTE format('CREATE CONSTRAINT TRIGGER cm_voting_validate AFTER INSERT OR UPDATE ON %I DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_voting_validate()',tab); END LOOP; END $$;
""");
}
