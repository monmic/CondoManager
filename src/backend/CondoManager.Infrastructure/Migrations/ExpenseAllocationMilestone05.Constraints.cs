using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class ExpenseAllocationMilestone05
{
    private static void AllocationConstraints(MigrationBuilder m)=>m.Sql("""
CREATE FUNCTION cm_allocation_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Allocation history is immutable' USING ERRCODE='55000'; END $$;
CREATE TRIGGER cm_run_immutable BEFORE UPDATE OR DELETE ON "AllocationRuns" FOR EACH ROW EXECUTE FUNCTION cm_allocation_immutable();
CREATE TRIGGER cm_run_source_immutable BEFORE UPDATE OR DELETE ON "AllocationRunSources" FOR EACH ROW EXECUTE FUNCTION cm_allocation_immutable();
CREATE TRIGGER cm_run_line_immutable BEFORE UPDATE OR DELETE ON "AllocationRunLines" FOR EACH ROW EXECUTE FUNCTION cm_allocation_immutable();
CREATE TRIGGER cm_reading_immutable BEFORE UPDATE OR DELETE ON "ConsumptionReadings" FOR EACH ROW EXECUTE FUNCTION cm_allocation_immutable();
CREATE FUNCTION cm_rule_protect() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' OR OLD."Status"=1 THEN RAISE EXCEPTION 'Active rule immutable' USING ERRCODE='55000'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER cm_rule_protect BEFORE UPDATE OR DELETE ON "AllocationRules" FOR EACH ROW EXECUTE FUNCTION cm_rule_protect();
CREATE FUNCTION cm_component_protect() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE rid uuid;
BEGIN
 IF TG_OP='INSERT' THEN rid:=NEW."RuleId"; ELSE rid:=OLD."RuleId"; END IF;
 IF EXISTS (SELECT 1 FROM "AllocationRules" WHERE "Id"=rid AND "Status"=1) THEN RAISE EXCEPTION 'Active components immutable' USING ERRCODE='55000'; END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF; RETURN NEW;
END $$;
CREATE TRIGGER cm_component_protect BEFORE INSERT OR UPDATE OR DELETE ON "AllocationRuleComponents" FOR EACH ROW EXECUTE FUNCTION cm_component_protect();
CREATE FUNCTION cm_rule_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE r record; total numeric; n bigint;
BEGIN
 SELECT * INTO r FROM "AllocationRules" WHERE "Id"=NEW."Id";
 IF r."Status"=1 THEN
  SELECT sum("Percentage"),count(*) INTO total,n FROM "AllocationRuleComponents" WHERE "RuleId"=r."Id";
  IF n<1 OR total<>100 OR (r."Type"=3 AND n<2) OR (r."Type"<>3 AND (n<>1 OR EXISTS (SELECT 1 FROM "AllocationRuleComponents" WHERE "RuleId"=r."Id" AND "Criterion"<>r."Type"))) THEN RAISE EXCEPTION 'Invalid rule components' USING ERRCODE='23514'; END IF;
 END IF; RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_rule_valid AFTER INSERT OR UPDATE ON "AllocationRules" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_rule_validate();

CREATE FUNCTION cm_allocation_run_insert() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p record; previous record;
BEGIN
 NEW."CreationTransactionId":=txid_current();
 PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" AND "IsActive" FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Inactive condominium' USING ERRCODE='23514'; END IF;
 SELECT * INTO p FROM "AccountingPeriods" WHERE "Id"=NEW."AccountingPeriodId";
 IF NOT FOUND OR p."Status"<>1 OR NEW."CompetenceFrom"<p."StartsOn" OR NEW."CompetenceTo">p."EndsOn" THEN RAISE EXCEPTION 'Allocation requires open period' USING ERRCODE='23514'; END IF;
 IF NEW."PreviousRunId" IS NULL THEN
  IF NEW."Version"<>1 THEN RAISE EXCEPTION 'Invalid initial version' USING ERRCODE='23514'; END IF;
 ELSE
  SELECT * INTO previous FROM "AllocationRuns" WHERE "Id"=NEW."PreviousRunId";
  IF NOT FOUND OR previous."AccountingPeriodId"<>NEW."AccountingPeriodId" OR NEW."Version"<>previous."Version"+1 THEN RAISE EXCEPTION 'Invalid replacement version' USING ERRCODE='23514'; END IF;
 END IF; RETURN NEW;
END $$;
CREATE TRIGGER cm_allocation_run_insert BEFORE INSERT ON "AllocationRuns" FOR EACH ROW EXECUTE FUNCTION cm_allocation_run_insert();
CREATE FUNCTION cm_allocation_child_insert() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM "AllocationRuns" WHERE "Id"=NEW."RunId" AND "CreationTransactionId"=txid_current()) THEN RAISE EXCEPTION 'Allocation rows may only be added with their run' USING ERRCODE='55000'; END IF;RETURN NEW;
END $$;
CREATE TRIGGER cm_allocation_source_insert BEFORE INSERT ON "AllocationRunSources" FOR EACH ROW EXECUTE FUNCTION cm_allocation_child_insert();
CREATE TRIGGER cm_allocation_line_insert BEFORE INSERT ON "AllocationRunLines" FOR EACH ROW EXECUTE FUNCTION cm_allocation_child_insert();
CREATE FUNCTION cm_allocation_run_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE source_total numeric; line_total numeric;
BEGIN
 SELECT sum("Amount") INTO source_total FROM "AllocationRunSources" WHERE "RunId"=NEW."Id";
 SELECT sum("Amount") INTO line_total FROM "AllocationRunLines" WHERE "RunId"=NEW."Id";
 IF source_total IS NULL OR line_total IS NULL OR source_total<>NEW."TotalAmount" OR line_total<>NEW."TotalAmount" THEN RAISE EXCEPTION 'Allocation totals do not balance' USING ERRCODE='23514'; END IF;
 IF EXISTS (SELECT 1 FROM "AllocationRunSources" s JOIN "ExpenseDocuments" e ON e."Id"=s."ExpenseDocumentId" JOIN "AllocationRules" r ON r."Id"=s."RuleId" WHERE s."RunId"=NEW."Id" AND (NOT e."IsRegistered" OR e."IsCancelled" OR e."AccountingPeriodId"<>NEW."AccountingPeriodId" OR r."Status"<>1 OR s."Amount"<>(SELECT COALESCE(sum("Amount"),0) FROM "AllocationRunLines" WHERE "SourceId"=s."Id"))) THEN RAISE EXCEPTION 'Invalid allocation source' USING ERRCODE='23514'; END IF;
 IF EXISTS (SELECT 1 FROM "ExpenseDocuments" e WHERE e."Id" IN (SELECT "ExpenseDocumentId" FROM "AllocationRunSources" WHERE "RunId"=NEW."Id") AND e."GrossAmount"<(SELECT COALESCE(sum(s."Amount"),0) FROM "AllocationRunSources" s WHERE s."ExpenseDocumentId"=e."Id" AND NOT EXISTS (SELECT 1 FROM "AllocationRuns" r WHERE r."PreviousRunId"=s."RunId"))) THEN RAISE EXCEPTION 'Expense overallocated' USING ERRCODE='23514'; END IF;
 IF NEW."PreviousRunId" IS NOT NULL AND EXISTS (
   SELECT 1 FROM (
     (SELECT "ExpenseDocumentId",sum("Amount") FROM "AllocationRunSources" WHERE "RunId"=NEW."Id" GROUP BY "ExpenseDocumentId" EXCEPT SELECT "ExpenseDocumentId",sum("Amount") FROM "AllocationRunSources" WHERE "RunId"=NEW."PreviousRunId" GROUP BY "ExpenseDocumentId")
     UNION ALL
     (SELECT "ExpenseDocumentId",sum("Amount") FROM "AllocationRunSources" WHERE "RunId"=NEW."PreviousRunId" GROUP BY "ExpenseDocumentId" EXCEPT SELECT "ExpenseDocumentId",sum("Amount") FROM "AllocationRunSources" WHERE "RunId"=NEW."Id" GROUP BY "ExpenseDocumentId")
   ) differences
 ) THEN RAISE EXCEPTION 'Replacement must preserve sources and amounts' USING ERRCODE='23514'; END IF; RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_allocation_run_valid AFTER INSERT ON "AllocationRuns" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_allocation_run_validate();

CREATE FUNCTION cm_consumption_insert() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE parent record; opposite record;
BEGIN
 PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" FOR UPDATE;
 IF NOT EXISTS (SELECT 1 FROM "AccountingPeriods" WHERE "Id"=NEW."AccountingPeriodId" AND "Status"=1 AND NEW."Date" BETWEEN "StartsOn" AND "EndsOn") THEN RAISE EXCEPTION 'Reading requires an open period' USING ERRCODE='23514'; END IF;
 IF NEW."ReplacesReadingId" IS NOT NULL THEN
  SELECT * INTO parent FROM "ConsumptionReadings" WHERE "Id"=NEW."ReplacesReadingId";
  IF NOT FOUND OR parent."PropertyUnitId"<>NEW."PropertyUnitId" OR parent."AccountingPeriodId"<>NEW."AccountingPeriodId" OR parent."ConsumptionType"<>NEW."ConsumptionType" OR parent."UnitOfMeasure"<>NEW."UnitOfMeasure" OR parent."MeterCode"<>NEW."MeterCode" OR parent."Boundary"<>NEW."Boundary" THEN RAISE EXCEPTION 'Invalid reading correction' USING ERRCODE='23514'; END IF;
 END IF;
 IF EXISTS (SELECT 1 FROM "ConsumptionReadings" r WHERE r."PropertyUnitId"=NEW."PropertyUnitId" AND r."AccountingPeriodId"=NEW."AccountingPeriodId" AND r."ConsumptionType"=NEW."ConsumptionType" AND r."UnitOfMeasure"=NEW."UnitOfMeasure" AND r."MeterCode"=NEW."MeterCode" AND r."Boundary"=NEW."Boundary" AND r."Id" IS DISTINCT FROM NEW."ReplacesReadingId" AND NOT EXISTS (SELECT 1 FROM "ConsumptionReadings" c WHERE c."ReplacesReadingId"=r."Id")) THEN RAISE EXCEPTION 'Use a tracked correction for an existing reading' USING ERRCODE='23514'; END IF;
 SELECT * INTO opposite FROM "ConsumptionReadings" r WHERE r."PropertyUnitId"=NEW."PropertyUnitId" AND r."AccountingPeriodId"=NEW."AccountingPeriodId" AND r."ConsumptionType"=NEW."ConsumptionType" AND r."UnitOfMeasure"=NEW."UnitOfMeasure" AND r."MeterCode"=NEW."MeterCode" AND r."Boundary"<>NEW."Boundary" AND NOT EXISTS (SELECT 1 FROM "ConsumptionReadings" c WHERE c."ReplacesReadingId"=r."Id");
 IF FOUND AND ((NEW."Boundary"=0 AND (NEW."Date">=opposite."Date" OR NEW."Value">opposite."Value")) OR (NEW."Boundary"=1 AND (NEW."Date"<=opposite."Date" OR NEW."Value"<opposite."Value"))) THEN RAISE EXCEPTION 'Inconsistent readings' USING ERRCODE='23514'; END IF; RETURN NEW;
END $$;
CREATE TRIGGER cm_consumption_insert BEFORE INSERT ON "ConsumptionReadings" FOR EACH ROW EXECUTE FUNCTION cm_consumption_insert();
""");
}
