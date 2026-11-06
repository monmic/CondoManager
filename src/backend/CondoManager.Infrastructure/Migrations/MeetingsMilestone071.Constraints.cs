using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class MeetingsMilestone071
{
 private static void MeetingConstraints(MigrationBuilder m)=>m.Sql("""
CREATE FUNCTION cm_meeting_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Meeting history immutable' USING ERRCODE='55000'; END $$;
CREATE TRIGGER cm_snapshot_immutable BEFORE UPDATE OR DELETE ON "MeetingSnapshots" FOR EACH ROW EXECUTE FUNCTION cm_meeting_immutable();
CREATE TRIGGER cm_revision_immutable BEFORE UPDATE OR DELETE ON "MeetingRevisions" FOR EACH ROW EXECUTE FUNCTION cm_meeting_immutable();
CREATE TRIGGER cm_notice_event_immutable BEFORE UPDATE OR DELETE ON "MeetingNoticeEvents" FOR EACH ROW EXECUTE FUNCTION cm_meeting_immutable();
CREATE TRIGGER cm_absence_immutable BEFORE UPDATE OR DELETE ON "MeetingAbsences" FOR EACH ROW EXECUTE FUNCTION cm_meeting_immutable();
CREATE FUNCTION cm_meeting_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE h record; j jsonb; mid uuid; refid uuid; typ int;
BEGIN
 j:=CASE WHEN TG_OP='DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;
 IF TG_TABLE_NAME='Meetings' THEN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Meeting deletion forbidden' USING ERRCODE='55000'; END IF;
  IF TG_OP='UPDATE' THEN
   IF NEW."Version"<>OLD."Version"+1 OR NEW."TenantId"<>OLD."TenantId" OR NEW."CondominiumId"<>OLD."CondominiumId" OR OLD."Status" IN (4,5) THEN RAISE EXCEPTION 'Invalid meeting revision' USING ERRCODE='23514'; END IF;
   IF OLD."Status">=2 AND (NEW."Title",NEW."Type",NEW."Location",NEW."RemoteMeetingUrl",NEW."AccountingPeriodId",NEW."Description") IS DISTINCT FROM (OLD."Title",OLD."Type",OLD."Location",OLD."RemoteMeetingUrl",OLD."AccountingPeriodId",OLD."Description") THEN RAISE EXCEPTION 'Convened meeting immutable' USING ERRCODE='55000'; END IF;
  END IF;
  RETURN NEW;
 END IF;
 mid:=(j->>'MeetingId')::uuid;SELECT * INTO h FROM "Meetings" WHERE "Id"=mid AND "TenantId"=(j->>'TenantId')::uuid AND "CondominiumId"=(j->>'CondominiumId')::uuid FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Meeting scope mismatch' USING ERRCODE='23503'; END IF;
 IF h."Status" IN (4,5) AND TG_TABLE_NAME<>'MeetingRevisions' THEN RAISE EXCEPTION 'Meeting closed' USING ERRCODE='55000'; END IF;
 IF TG_TABLE_NAME='MeetingAgendaItems' THEN
  IF h."Status">=2 THEN RAISE EXCEPTION 'Agenda consolidated' USING ERRCODE='55000'; END IF;
  IF TG_OP<>'DELETE' AND NEW."RelatedId" IS NOT NULL THEN
   refid:=NEW."RelatedId";typ:=NEW."RelatedType";
   IF NOT ((typ=0 AND EXISTS(SELECT 1 FROM "Budgets" WHERE "Id"=refid AND "TenantId"=h."TenantId" AND "CondominiumId"=h."CondominiumId")) OR (typ=1 AND EXISTS(SELECT 1 FROM "FinalStatements" WHERE "Id"=refid AND "TenantId"=h."TenantId" AND "CondominiumId"=h."CondominiumId")) OR (typ=2 AND EXISTS(SELECT 1 FROM "AccountingPeriods" WHERE "Id"=refid AND "TenantId"=h."TenantId" AND "CondominiumId"=h."CondominiumId"))) THEN RAISE EXCEPTION 'Agenda reference scope mismatch' USING ERRCODE='23503'; END IF;
  END IF;
 END IF;
 IF TG_TABLE_NAME='MeetingSessions' THEN IF TG_OP='UPDATE' AND h."Status">=2 AND NEW."StartsAt"<>OLD."StartsAt" THEN RAISE EXCEPTION 'Session consolidated' USING ERRCODE='55000'; END IF; END IF;
 IF TG_OP='DELETE' AND TG_TABLE_NAME NOT IN ('MeetingAgendaItems','MeetingSessions') THEN RAISE EXCEPTION 'Meeting history deletion forbidden' USING ERRCODE='55000'; END IF;
 RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
END $$;
DO $$ DECLARE tab text; BEGIN FOREACH tab IN ARRAY ARRAY['Meetings','MeetingSessions','MeetingAgendaItems','MeetingSnapshots','MeetingNotices','MeetingNoticeEvents','MeetingProxies','MeetingProxyUnits','MeetingAttendances','MeetingAttendanceUnits','MeetingRevisions','MeetingAbsences'] LOOP EXECUTE format('CREATE TRIGGER cm_meeting_guard BEFORE INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION cm_meeting_guard()',tab); END LOOP; END $$;
CREATE FUNCTION cm_meeting_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE h record; mid uuid; j jsonb; a record; p record; u record; sn jsonb; rep uuid;
BEGIN
 j:=CASE WHEN TG_OP='DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;mid:=CASE WHEN TG_TABLE_NAME='Meetings' THEN (j->>'Id')::uuid ELSE (j->>'MeetingId')::uuid END;
 SELECT * INTO h FROM "Meetings" WHERE "Id"=mid;IF NOT FOUND THEN RETURN NULL; END IF;
 IF EXISTS(SELECT 1 FROM "MeetingSessions" f JOIN "MeetingSessions" s ON f."MeetingId"=s."MeetingId" WHERE f."MeetingId"=mid AND f."Call"=0 AND s."Call"=1 AND s."StartsAt"<=f."StartsAt") THEN RAISE EXCEPTION 'Second call must follow first' USING ERRCODE='23514'; END IF;
 IF NOT EXISTS(SELECT 1 FROM "MeetingSessions" WHERE "MeetingId"=mid AND "Call"=0) THEN RAISE EXCEPTION 'First call required' USING ERRCODE='23514'; END IF;
 IF (SELECT count(*) FROM "MeetingSessions" WHERE "MeetingId"=mid AND "Status"=1)>1 THEN RAISE EXCEPTION 'Concurrent meeting sessions forbidden' USING ERRCODE='23514'; END IF;
 FOR u IN SELECT * FROM "MeetingAttendanceUnits" WHERE "MeetingId"=mid AND "IsActive" LOOP
  SELECT * INTO a FROM "MeetingAttendances" WHERE "Id"=u."AttendanceId";
  IF a."SessionId"<>u."SessionId" OR a."CheckOutAt" IS NOT NULL OR NOT EXISTS(SELECT 1 FROM "MeetingSessions" WHERE "Id"=a."SessionId" AND "Status"=1) THEN RAISE EXCEPTION 'Attendance session mismatch' USING ERRCODE='23514'; END IF;
  SELECT "ContentJson" INTO sn FROM "MeetingSnapshots" WHERE "Id"=a."SnapshotId";
  SELECT (x->>'RepresentativeId')::uuid INTO rep FROM jsonb_array_elements(sn->'Units') x WHERE (x->>'Id')::uuid=u."PropertyUnitId";
  IF rep IS DISTINCT FROM a."PersonId" OR a."SnapshotId"<>h."CurrentSnapshotId" THEN RAISE EXCEPTION 'Attendance representation mismatch' USING ERRCODE='23514'; END IF;
  IF a."Type"=2 THEN SELECT * INTO p FROM "MeetingProxies" WHERE "Id"=a."ProxyId";
   IF NOT FOUND OR p."Status"<>2 OR p."DelegatorId"<>a."PersonId" OR p."DelegateId"<>a."RepresentedById" OR p."SnapshotId"<>a."SnapshotId" OR NOT EXISTS(SELECT 1 FROM "MeetingProxyUnits" WHERE "ProxyId"=p."Id" AND "PropertyUnitId"=u."PropertyUnitId" AND "IsActive") THEN RAISE EXCEPTION 'Verified proxy required' USING ERRCODE='23514'; END IF;
  ELSIF a."ProxyId" IS NOT NULL OR a."RepresentedById" IS NOT NULL OR EXISTS(SELECT 1 FROM "MeetingProxyUnits" WHERE "MeetingId"=mid AND "PropertyUnitId"=u."PropertyUnitId" AND "IsActive") THEN RAISE EXCEPTION 'Duplicate proxy/direct representation' USING ERRCODE='23514'; END IF;
 END LOOP;
 RETURN NULL;
END $$;
DO $$ DECLARE tab text; BEGIN FOREACH tab IN ARRAY ARRAY['Meetings','MeetingSessions','MeetingSnapshots','MeetingProxies','MeetingProxyUnits','MeetingAttendances','MeetingAttendanceUnits'] LOOP EXECUTE format('CREATE CONSTRAINT TRIGGER cm_meeting_valid AFTER INSERT OR UPDATE OR DELETE ON %I DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_meeting_valid()',tab); END LOOP; END $$;
""");
}
