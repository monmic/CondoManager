using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class DocumentsMilestone073
{
 static void DocumentConstraints(MigrationBuilder m)=>m.Sql("""
CREATE FUNCTION cm_document_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE target_table text; valid boolean;
BEGIN
 IF TG_OP='DELETE' OR (TG_TABLE_NAME IN ('Documents','DocumentLinks') AND TG_OP='UPDATE') THEN
   RAISE EXCEPTION 'Document history is immutable' USING ERRCODE='55000';
 END IF;
 IF TG_TABLE_NAME='Documents' AND TG_OP='INSERT' THEN
  IF NEW."PreviousDocumentId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "Documents" d WHERE d."Id"=NEW."PreviousDocumentId" AND d."TenantId"=NEW."TenantId" AND d."CondominiumId"=NEW."CondominiumId" AND d."Type"=NEW."Type" AND d."Version"+1=NEW."Version") THEN
   RAISE EXCEPTION 'Invalid document version chain' USING ERRCODE='23514';
  END IF;
 END IF;
 IF TG_TABLE_NAME='MeetingMinutes' THEN
  IF TG_OP='UPDATE' THEN
   IF OLD."Status" IN (2,3) THEN RAISE EXCEPTION 'Final minutes immutable' USING ERRCODE='55000'; END IF;
   IF NEW."Revision"<>OLD."Revision"+1 OR NEW."TenantId"<>OLD."TenantId" OR NEW."CondominiumId"<>OLD."CondominiumId" OR NEW."MeetingId"<>OLD."MeetingId" OR NEW."Version"<>OLD."Version" OR NEW."SourceJson"<>OLD."SourceJson" OR NEW."SourceFingerprint"<>OLD."SourceFingerprint" OR NEW."PreviousMinutesId" IS DISTINCT FROM OLD."PreviousMinutesId" THEN
    RAISE EXCEPTION 'Minutes source/version immutable' USING ERRCODE='55000';
   END IF;
  END IF;
  IF NOT EXISTS(SELECT 1 FROM "Meetings" a JOIN "MeetingCompletions" c ON c."MeetingId"=a."Id" AND c."TenantId"=a."TenantId" WHERE a."Id"=NEW."MeetingId" AND a."CondominiumId"=NEW."CondominiumId" AND a."TenantId"=NEW."TenantId" AND a."Status"=4) THEN
   RAISE EXCEPTION 'Completed meeting required' USING ERRCODE='23514';
  END IF;
 END IF;
 IF TG_TABLE_NAME='DocumentLinks' THEN
  target_table=CASE NEW."TargetType" WHEN 0 THEN 'AccountingPeriods' WHEN 1 THEN 'Meetings' WHEN 2 THEN 'MeetingAgendaItems' WHEN 3 THEN 'MeetingNotices' WHEN 4 THEN 'MeetingProxies' WHEN 5 THEN 'MeetingResolutions' WHEN 6 THEN 'Budgets' WHEN 7 THEN 'FinalStatements' ELSE NULL END;
  IF target_table IS NULL THEN RAISE EXCEPTION 'Invalid document target' USING ERRCODE='23514'; END IF;
  EXECUTE format('SELECT EXISTS(SELECT 1 FROM %I WHERE "TenantId"=$1 AND "CondominiumId"=$2 AND "Id"=$3)',target_table) INTO valid USING NEW."TenantId",NEW."CondominiumId",NEW."TargetId";
  IF NOT valid THEN RAISE EXCEPTION 'Invalid document target scope' USING ERRCODE='23503'; END IF;
 END IF;
 RETURN NEW;
END $$;
CREATE UNIQUE INDEX "UX_Document_Predecessor" ON "Documents" ("TenantId","CondominiumId","PreviousDocumentId") WHERE "PreviousDocumentId" IS NOT NULL;
CREATE TRIGGER cm_document_immutable BEFORE INSERT OR UPDATE OR DELETE ON "Documents" FOR EACH ROW EXECUTE FUNCTION cm_document_guard();
CREATE TRIGGER cm_document_links BEFORE INSERT OR UPDATE OR DELETE ON "DocumentLinks" FOR EACH ROW EXECUTE FUNCTION cm_document_guard();
CREATE TRIGGER cm_minutes_guard BEFORE INSERT OR UPDATE OR DELETE ON "MeetingMinutes" FOR EACH ROW EXECUTE FUNCTION cm_document_guard();
CREATE FUNCTION cm_document_target_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS(SELECT 1 FROM "DocumentLinks" l WHERE l."TenantId"=OLD."TenantId" AND l."CondominiumId"=OLD."CondominiumId" AND l."TargetId"=OLD."Id" AND l."TargetType"=TG_ARGV[0]::integer) THEN
  RAISE EXCEPTION 'Document historical reference must be preserved' USING ERRCODE='23503';
 END IF;
 RETURN OLD;
END $$;
CREATE TRIGGER cm_document_target BEFORE DELETE ON "AccountingPeriods" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('0');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "Meetings" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('1');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "MeetingAgendaItems" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('2');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "MeetingNotices" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('3');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "MeetingProxies" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('4');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "MeetingResolutions" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('5');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "Budgets" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('6');
CREATE TRIGGER cm_document_target BEFORE DELETE ON "FinalStatements" FOR EACH ROW EXECUTE FUNCTION cm_document_target_guard('7');
""");
}
