using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class FinalStatementsMilestone063
{
 private static void FinalConstraints(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "StatementSettlements" ADD COLUMN "CreationTransactionId" bigint NOT NULL DEFAULT txid_current();
ALTER TABLE "ResidentOpenings" ADD COLUMN "CreationTransactionId" bigint NOT NULL DEFAULT txid_current();
DO $$ DECLARE definition text; BEGIN
 definition:=pg_get_functiondef('cm_resident_journal_valid()'::regprocedure);
 IF position('NOT IN (5,6,7)' in definition)=0 THEN RAISE EXCEPTION 'Unexpected prior ledger validator'; END IF;
 EXECUTE replace(definition,'NOT IN (5,6,7)','NOT IN (5,6,7,8,9)');
END $$;
CREATE FUNCTION cm_final_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE period_id uuid; parent record;
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Final history cannot be deleted' USING ERRCODE='55000'; END IF;
 PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" AND "IsActive" FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Inactive condominium' USING ERRCODE='23514'; END IF;
 IF TG_TABLE_NAME='SettlementPositions' THEN
  SELECT * INTO parent FROM "StatementSettlements" WHERE "Id"=NEW."SettlementId";
  IF TG_OP<>'INSERT' OR parent."CreationTransactionId"<>txid_current() OR parent."PostedAt" IS NOT NULL THEN RAISE EXCEPTION 'Confirmed positions immutable' USING ERRCODE='55000'; END IF;RETURN NEW;
 END IF;
 IF TG_TABLE_NAME='OpeningPositions' THEN
  SELECT * INTO parent FROM "ResidentOpenings" WHERE "Id"=NEW."OpeningId";
  IF TG_OP<>'INSERT' OR parent."CreationTransactionId"<>txid_current() THEN RAISE EXCEPTION 'Opening positions immutable' USING ERRCODE='55000'; END IF;RETURN NEW;
 END IF;
 period_id:=NEW."AccountingPeriodId";
 IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" p WHERE p."Id"=period_id AND p."Status"=1) AND TG_TABLE_NAME<>'AdministrativeClosures' THEN RAISE EXCEPTION 'Period must be open' USING ERRCODE='55000'; END IF;
 IF TG_TABLE_NAME='FinalStatements' THEN
  IF TG_OP='INSERT' THEN
   IF NEW."Status"<>0 THEN RAISE EXCEPTION 'New statement must be draft' USING ERRCODE='23514'; END IF;
   IF NEW."BudgetId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "Budgets" b WHERE b."Id"=NEW."BudgetId" AND b."AccountingPeriodId"=period_id AND b."Status"=2) THEN RAISE EXCEPTION 'Invalid budget reference' USING ERRCODE='23514'; END IF;
   IF NEW."PreviousVersionId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "FinalStatements" s WHERE s."Id"=NEW."PreviousVersionId" AND s."AccountingPeriodId"=period_id AND s."Version"=NEW."Version"-1) THEN RAISE EXCEPTION 'Invalid prior statement' USING ERRCODE='23514'; END IF;
  ELSE
   IF (to_jsonb(NEW)-ARRAY['Status','ApprovedAt','ApprovedBy','DecisionReference','UpdatedAt']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','ApprovedAt','ApprovedBy','DecisionReference','UpdatedAt']) OR NOT ((OLD."Status"=0 AND NEW."Status"=1) OR (OLD."Status"=1 AND NEW."Status"=2) OR (OLD."Status"=2 AND NEW."Status"=3)) THEN RAISE EXCEPTION 'Statement immutable; create a version' USING ERRCODE='55000'; END IF;
   IF NEW."Status" IN (2,3) AND EXISTS(SELECT 1 FROM "StatementSettlements" b WHERE b."AccountingPeriodId"=period_id AND b."CondominiumId"=NEW."CondominiumId" AND b."PostedAt" IS NOT NULL AND b."ReversedJournalId" IS NULL) THEN RAISE EXCEPTION 'Reverse posted settlement before replacement' USING ERRCODE='55000'; END IF;
   IF NEW."Status"=2 AND ((NEW."SnapshotJson"->>'difference')::numeric<>0 OR EXISTS(SELECT 1 FROM jsonb_array_elements(NEW."SnapshotJson"->'anomalies') a WHERE (a->>'blocking')::boolean)) THEN RAISE EXCEPTION 'Blocking anomalies' USING ERRCODE='23514'; END IF;
  END IF;
 ELSIF TG_TABLE_NAME='StatementSettlements' THEN
  IF NOT EXISTS(SELECT 1 FROM "FinalStatements" s WHERE s."Id"=NEW."StatementId" AND s."AccountingPeriodId"=period_id AND s."Status"=2) THEN RAISE EXCEPTION 'Approved statement required' USING ERRCODE='23514'; END IF;
  IF TG_OP='INSERT' THEN NEW."CreationTransactionId":=txid_current();IF NEW."PostedAt" IS NOT NULL THEN RAISE EXCEPTION 'Confirm before posting' USING ERRCODE='23514'; END IF;
  ELSE
   IF (to_jsonb(NEW)-ARRAY['JournalEntryId','PostedAt','PostedBy','ReversedJournalId','UpdatedAt']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['JournalEntryId','PostedAt','PostedBy','ReversedJournalId','UpdatedAt']) OR NOT ((OLD."PostedAt" IS NULL AND NEW."PostedAt" IS NOT NULL AND NEW."ReversedJournalId" IS NULL) OR (OLD."PostedAt" IS NOT NULL AND OLD."ReversedJournalId" IS NULL AND NEW."ReversedJournalId" IS NOT NULL AND NEW."JournalEntryId"=OLD."JournalEntryId" AND NEW."PostedAt"=OLD."PostedAt" AND NEW."PostedBy"=OLD."PostedBy")) THEN RAISE EXCEPTION 'Settlement immutable' USING ERRCODE='55000'; END IF;
  END IF;
 ELSE
  IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Opening or closure immutable' USING ERRCODE='55000'; END IF;
  IF TG_TABLE_NAME='ResidentOpenings' THEN
   NEW."CreationTransactionId":=txid_current();
   IF NEW."Mode"<>2 AND EXISTS(SELECT 1 FROM "AccountingPeriods" prior JOIN "AccountingPeriods" target ON target."Id"=period_id WHERE prior."CondominiumId"=NEW."CondominiumId" AND prior."EndsOn"<target."StartsOn") THEN RAISE EXCEPTION 'Carry previous closure instead of new balances' USING ERRCODE='23514'; END IF;
   IF NEW."Mode"=2 AND NOT EXISTS(SELECT 1 FROM "AdministrativeClosures" c JOIN "AccountingPeriods" prior ON prior."Id"=c."AccountingPeriodId" JOIN "AccountingPeriods" target ON target."Id"=period_id WHERE c."Id"=NEW."SourceClosureId" AND prior."Status"=2 AND prior."EndsOn"<target."StartsOn" AND NOT EXISTS(SELECT 1 FROM "AccountingPeriods" newer WHERE newer."CondominiumId"=NEW."CondominiumId" AND newer."EndsOn"<target."StartsOn" AND newer."EndsOn">prior."EndsOn")) THEN RAISE EXCEPTION 'Carry latest closed exercise' USING ERRCODE='23514'; END IF;
  END IF;
 END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER cm_final_guard BEFORE INSERT OR UPDATE OR DELETE ON "FinalStatements" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE TRIGGER cm_settlement_guard BEFORE INSERT OR UPDATE OR DELETE ON "StatementSettlements" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE TRIGGER cm_settlement_position_guard BEFORE INSERT OR UPDATE OR DELETE ON "SettlementPositions" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE TRIGGER cm_opening_guard BEFORE INSERT OR UPDATE OR DELETE ON "ResidentOpenings" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE TRIGGER cm_opening_position_guard BEFORE INSERT OR UPDATE OR DELETE ON "OpeningPositions" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE TRIGGER cm_closure_guard BEFORE INSERT OR UPDATE OR DELETE ON "AdministrativeClosures" FOR EACH ROW EXECUTE FUNCTION cm_final_guard();
CREATE FUNCTION cm_final_post_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE header record; positive numeric; negative numeric; expected_period uuid; offset_purpose integer;
BEGIN
 IF NEW."Kind" IN (8,9) THEN
  IF NEW."Kind"=8 THEN
   SELECT * INTO header FROM "StatementSettlements" WHERE "JournalEntryId"=NEW."Id" AND "PostedAt" IS NOT NULL;
   IF NOT FOUND THEN RAISE EXCEPTION 'Settlement header required' USING ERRCODE='23514'; END IF;
   SELECT COALESCE(sum(GREATEST("Amount",0)),0),COALESCE(sum(GREATEST(-"Amount",0)),0) INTO positive,negative FROM "SettlementPositions" WHERE "SettlementId"=header."Id";offset_purpose:=5;
  ELSE
   SELECT * INTO header FROM "ResidentOpenings" WHERE "JournalEntryId"=NEW."Id" AND "Mode"=1;
   IF NOT FOUND THEN RAISE EXCEPTION 'Import header required' USING ERRCODE='23514'; END IF;
   SELECT COALESCE(sum(GREATEST("Amount",0)),0),COALESCE(sum(GREATEST(-"Amount",0)),0) INTO positive,negative FROM "OpeningPositions" WHERE "OpeningId"=header."Id";offset_purpose:=3;
  END IF;
  IF header."AccountingPeriodId"<>NEW."AccountingPeriodId" OR positive+negative=0 OR EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose" NOT IN (4,offset_purpose)) OR (SELECT COALESCE(sum(l."Debit"),0) FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=4)<>positive OR (SELECT COALESCE(sum(l."Credit"),0) FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=4)<>negative OR (SELECT COALESCE(sum(l."Debit"),0) FROM "JournalEntryLines" l WHERE l."JournalEntryId"=NEW."Id")<>positive+negative THEN RAISE EXCEPTION 'Signed resident journal mismatch' USING ERRCODE='23514'; END IF;
 END IF;
 IF NEW."Kind"=5 AND EXISTS(SELECT 1 FROM "JournalEntries" j WHERE j."Id"=NEW."ReversesEntryId" AND j."Kind" IN (8,9)) AND NOT EXISTS(SELECT 1 FROM "StatementSettlements" b WHERE b."JournalEntryId"=NEW."ReversesEntryId" AND b."ReversedJournalId"=NEW."Id" AND b."AccountingPeriodId"=NEW."AccountingPeriodId") THEN RAISE EXCEPTION 'Managed reversal required' USING ERRCODE='55000'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_final_post_valid AFTER INSERT ON "JournalEntries" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_final_post_valid();
CREATE FUNCTION cm_final_header_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE header record; amount numeric;
BEGIN
 IF TG_TABLE_NAME='StatementSettlements' THEN
  SELECT * INTO header FROM "StatementSettlements" WHERE "Id"=NEW."Id";
  SELECT COALESCE(sum(abs("Amount")),0) INTO amount FROM "SettlementPositions" WHERE "SettlementId"=header."Id";
  IF header."PostedAt" IS NOT NULL AND ((amount>0 AND header."JournalEntryId" IS NULL) OR (amount=0 AND header."JournalEntryId" IS NOT NULL)) THEN RAISE EXCEPTION 'Settlement posting missing or artificial zero journal' USING ERRCODE='23514'; END IF;
  IF (SELECT COALESCE(sum("Amount"),0) FROM "SettlementPositions" WHERE "SettlementId"=header."Id")<>(header."SnapshotJson"->>'delta')::numeric THEN RAISE EXCEPTION 'Settlement positions unbalanced' USING ERRCODE='23514'; END IF;
 ELSE
  SELECT * INTO header FROM "ResidentOpenings" WHERE "Id"=NEW."Id";
  SELECT COALESCE(sum(abs("Amount")),0) INTO amount FROM "OpeningPositions" WHERE "OpeningId"=header."Id";
  IF (header."Mode"=0 AND amount<>0) OR (header."Mode"=1 AND ((amount>0 AND header."JournalEntryId" IS NULL) OR (amount=0 AND header."JournalEntryId" IS NOT NULL))) THEN RAISE EXCEPTION 'Invalid opening journal' USING ERRCODE='23514'; END IF;
  IF header."Mode"=2 AND EXISTS(
   (SELECT "PropertyUnitId","PersonId","Amount" FROM "OpeningPositions" WHERE "OpeningId"=header."Id" EXCEPT SELECT (x->>'propertyUnitId')::uuid,(x->>'personId')::uuid,(x->>'balance')::numeric FROM "AdministrativeClosures" c CROSS JOIN LATERAL jsonb_array_elements(c."SnapshotJson"->'positions') x WHERE c."Id"=header."SourceClosureId")
   UNION ALL
   (SELECT (x->>'propertyUnitId')::uuid,(x->>'personId')::uuid,(x->>'balance')::numeric FROM "AdministrativeClosures" c CROSS JOIN LATERAL jsonb_array_elements(c."SnapshotJson"->'positions') x WHERE c."Id"=header."SourceClosureId" EXCEPT SELECT "PropertyUnitId","PersonId","Amount" FROM "OpeningPositions" WHERE "OpeningId"=header."Id")
  ) THEN RAISE EXCEPTION 'Carry differs from source closure' USING ERRCODE='23514'; END IF;
 END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_settlement_header_valid AFTER INSERT OR UPDATE ON "StatementSettlements" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_final_header_valid();
CREATE CONSTRAINT TRIGGER cm_opening_header_valid AFTER INSERT ON "ResidentOpenings" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_final_header_valid();
CREATE VIEW cm_resident_positions AS
 SELECT d."TenantId",d."CondominiumId",d."PropertyUnitId",d."PersonId",d."Amount" FROM "InstallmentDebtors" d JOIN "InstallmentPlans" p ON p."Id"=d."PlanId" WHERE p."JournalEntryId" IS NOT NULL
 UNION ALL SELECT a."TenantId",a."CondominiumId",d."PropertyUnitId",d."PersonId",CASE WHEN a."ReversesAllocationId" IS NULL THEN -a."Amount" ELSE a."Amount" END FROM "ReceiptAllocations" a JOIN "Installments" i ON i."Id"=a."InstallmentId" JOIN "InstallmentDebtors" d ON d."Id"=i."InstallmentDebtorId"
 UNION ALL SELECT l."TenantId",l."CondominiumId",l."PropertyUnitId",l."PersonId",l."Amount" FROM "SettlementPositions" l JOIN "StatementSettlements" b ON b."Id"=l."SettlementId" WHERE b."PostedAt" IS NOT NULL AND b."JournalEntryId" IS NOT NULL
 UNION ALL SELECT l."TenantId",l."CondominiumId",l."PropertyUnitId",l."PersonId",-l."Amount" FROM "SettlementPositions" l JOIN "StatementSettlements" b ON b."Id"=l."SettlementId" WHERE b."ReversedJournalId" IS NOT NULL
 UNION ALL SELECT l."TenantId",l."CondominiumId",l."PropertyUnitId",l."PersonId",l."Amount" FROM "OpeningPositions" l JOIN "ResidentOpenings" o ON o."Id"=l."OpeningId" WHERE o."Mode"=1 AND o."JournalEntryId" IS NOT NULL;
CREATE FUNCTION cm_closure_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE period_id uuid; condo uuid; closure record; actual numeric; ledger numeric;
BEGIN
 IF TG_TABLE_NAME='AccountingPeriods' THEN period_id:=NEW."Id";condo:=NEW."CondominiumId";IF NEW."Status"<>2 THEN RETURN NULL; END IF;
 ELSE period_id:=NEW."AccountingPeriodId";condo:=NEW."CondominiumId";END IF;
 SELECT * INTO closure FROM "AdministrativeClosures" WHERE "AccountingPeriodId"=period_id AND "CondominiumId"=condo;
 IF NOT FOUND OR NOT EXISTS(SELECT 1 FROM "AccountingPeriods" WHERE "Id"=period_id AND "Status"=2) OR NOT EXISTS(SELECT 1 FROM "FinalStatements" s WHERE s."Id"=closure."StatementId" AND s."AccountingPeriodId"=period_id AND s."Status"=2) OR NOT EXISTS(SELECT 1 FROM "StatementSettlements" b WHERE b."StatementId"=closure."StatementId" AND b."PostedAt" IS NOT NULL AND b."ReversedJournalId" IS NULL) OR NOT EXISTS(SELECT 1 FROM "ResidentOpenings" o WHERE o."AccountingPeriodId"=period_id AND o."CondominiumId"=condo) THEN RAISE EXCEPTION 'Administrative closure prerequisites missing' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM jsonb_array_elements(closure."SnapshotJson"->'checks') a WHERE (a->>'blocking')::boolean) OR NOT (closure."SnapshotJson"->>'canClose')::boolean THEN RAISE EXCEPTION 'Blocking closure checklist' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM "ExpenseDocuments" e WHERE e."AccountingPeriodId"=period_id AND NOT e."IsCancelled" AND (NOT e."IsRegistered" OR e."GrossAmount"<>(SELECT COALESCE(sum(s."Amount"),0) FROM "AllocationRunSources" s WHERE s."ExpenseDocumentId"=e."Id" AND NOT EXISTS(SELECT 1 FROM "AllocationRuns" r WHERE r."PreviousRunId"=s."RunId")))) THEN RAISE EXCEPTION 'Unallocated or draft expenses' USING ERRCODE='23514'; END IF;
 SELECT COALESCE(sum(e."GrossAmount"),0) INTO actual FROM "ExpenseDocuments" e WHERE e."AccountingPeriodId"=period_id AND e."IsRegistered" AND NOT e."IsCancelled";
 SELECT COALESCE(sum(l."Debit"-l."Credit"),0) INTO ledger FROM "JournalEntryLines" l JOIN "JournalEntries" j ON j."Id"=l."JournalEntryId" JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE j."AccountingPeriodId"=period_id AND a."Type"=4;
 IF actual<>ledger THEN RAISE EXCEPTION 'Costs not reconciled' USING ERRCODE='23514'; END IF;
 SELECT COALESCE(sum(l."Debit"-l."Credit"),0) INTO ledger FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE a."CondominiumId"=condo AND a."Purpose"=4;
 IF EXISTS(
  (SELECT "PropertyUnitId","PersonId",sum("Amount") FROM cm_resident_positions WHERE "CondominiumId"=condo GROUP BY "PropertyUnitId","PersonId" HAVING sum("Amount")<>0 EXCEPT SELECT (x->>'propertyUnitId')::uuid,(x->>'personId')::uuid,(x->>'balance')::numeric FROM jsonb_array_elements(closure."SnapshotJson"->'positions') x WHERE (x->>'balance')::numeric<>0)
  UNION ALL
  (SELECT (x->>'propertyUnitId')::uuid,(x->>'personId')::uuid,(x->>'balance')::numeric FROM jsonb_array_elements(closure."SnapshotJson"->'positions') x WHERE (x->>'balance')::numeric<>0 EXCEPT SELECT "PropertyUnitId","PersonId",sum("Amount") FROM cm_resident_positions WHERE "CondominiumId"=condo GROUP BY "PropertyUnitId","PersonId" HAVING sum("Amount")<>0)
 ) THEN RAISE EXCEPTION 'Individual closure position mismatch' USING ERRCODE='23514'; END IF;
 IF ledger<>(SELECT COALESCE(sum("Amount"),0) FROM cm_resident_positions WHERE "CondominiumId"=condo) OR ledger<>(closure."SnapshotJson"->>'positionBalance')::numeric THEN RAISE EXCEPTION 'Resident balances not reconciled' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_closed_period_valid AFTER INSERT OR UPDATE ON "AccountingPeriods" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_closure_valid();
CREATE CONSTRAINT TRIGGER cm_closure_snapshot_valid AFTER INSERT ON "AdministrativeClosures" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_closure_valid();
CREATE FUNCTION cm_period_transition_lock() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF NEW."Status"<>OLD."Status" THEN PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" FOR UPDATE; END IF;RETURN NEW;
END $$;
CREATE TRIGGER cm_period_transition_lock BEFORE UPDATE ON "AccountingPeriods" FOR EACH ROW EXECUTE FUNCTION cm_period_transition_lock();
CREATE FUNCTION cm_closed_cost_guard() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN
 IF OLD."IsRegistered" AND NEW."IsCancelled" IS DISTINCT FROM OLD."IsCancelled" AND EXISTS(SELECT 1 FROM "AdministrativeClosures" c WHERE c."AccountingPeriodId"=OLD."AccountingPeriodId") THEN RAISE EXCEPTION 'Closed cost requires controlled subsequent adjustment' USING ERRCODE='55000'; END IF;RETURN NEW;
END $$;
CREATE TRIGGER cm_closed_cost_guard BEFORE UPDATE ON "ExpenseDocuments" FOR EACH ROW EXECUTE FUNCTION cm_closed_cost_guard();
""");
}
