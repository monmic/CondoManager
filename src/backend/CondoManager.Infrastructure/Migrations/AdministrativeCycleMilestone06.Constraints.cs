using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class AdministrativeCycleMilestone06
{
    private static void AddBudgetGuards(MigrationBuilder m)=>m.Sql("""
CREATE FUNCTION cm_budget_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE b record;
BEGIN
 IF TG_TABLE_NAME='Budgets' THEN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Budget history cannot be deleted' USING ERRCODE='55000'; END IF;
  PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" FOR UPDATE;
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" p WHERE p."Id"=NEW."AccountingPeriodId" AND p."TenantId"=NEW."TenantId" AND p."CondominiumId"=NEW."CondominiumId" AND p."Status"=1) THEN RAISE EXCEPTION 'Accounting period must be open' USING ERRCODE='55000'; END IF;
  IF TG_OP='INSERT' THEN
   IF NEW."Status"<>0 THEN RAISE EXCEPTION 'New budget must be draft' USING ERRCODE='55000'; END IF;
   IF NEW."PreviousVersionId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "Budgets" prior_budget WHERE prior_budget."Id"=NEW."PreviousVersionId" AND prior_budget."AccountingPeriodId"=NEW."AccountingPeriodId" AND prior_budget."Version"=NEW."Version"-1) THEN RAISE EXCEPTION 'Invalid previous version' USING ERRCODE='23514'; END IF;
  ELSE
   IF (NEW."TenantId",NEW."CondominiumId",NEW."AccountingPeriodId",NEW."Version",NEW."PreviousVersionId") IS DISTINCT FROM (OLD."TenantId",OLD."CondominiumId",OLD."AccountingPeriodId",OLD."Version",OLD."PreviousVersionId") THEN RAISE EXCEPTION 'Budget context immutable' USING ERRCODE='55000'; END IF;
   IF OLD."Status"=3 OR (OLD."Status"=2 AND (NEW."Status"<>3 OR (to_jsonb(NEW)-'Status'-'UpdatedAt') IS DISTINCT FROM (to_jsonb(OLD)-'Status'-'UpdatedAt'))) THEN RAISE EXCEPTION 'Approved budget immutable' USING ERRCODE='55000'; END IF;
   IF OLD."Status"=1 AND (NEW."Status"<>2 OR (to_jsonb(NEW)-ARRAY['Status','UpdatedAt','ApprovedAt','ApprovedBy','AllocationSnapshotJson','DecisionReference']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','UpdatedAt','ApprovedAt','ApprovedBy','AllocationSnapshotJson','DecisionReference'])) THEN RAISE EXCEPTION 'Proposed budget immutable' USING ERRCODE='55000'; END IF;
   IF OLD."Status"=0 AND NEW."Status" NOT IN (0,1) THEN RAISE EXCEPTION 'Propose before approval' USING ERRCODE='55000'; END IF;
   IF NEW."Status" IN (1,2) AND NOT EXISTS(SELECT 1 FROM "BudgetLines" l WHERE l."BudgetId"=NEW."Id") THEN RAISE EXCEPTION 'Empty budget' USING ERRCODE='23514'; END IF;
  END IF;
  RETURN NEW;
 END IF;
 IF TG_OP='UPDATE' AND EXISTS(SELECT 1 FROM "Budgets" prior_budget WHERE prior_budget."Id"=OLD."BudgetId" AND prior_budget."Status"<>0) THEN RAISE EXCEPTION 'Original budget immutable' USING ERRCODE='55000'; END IF;
 SELECT * INTO b FROM "Budgets" WHERE "Id"=CASE WHEN TG_OP='DELETE' THEN OLD."BudgetId" ELSE NEW."BudgetId" END FOR UPDATE;
 IF b."Status"<>0 THEN RAISE EXCEPTION 'Budget lines immutable after proposal' USING ERRCODE='55000'; END IF;
 IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" p WHERE p."Id"=b."AccountingPeriodId" AND p."Status"=1) THEN RAISE EXCEPTION 'Accounting period must be open' USING ERRCODE='55000'; END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;
 IF NOT EXISTS(SELECT 1 FROM "Accounts" a WHERE a."Id"=NEW."AccountId" AND a."Type"=4 AND a."IsActive") OR NOT EXISTS(SELECT 1 FROM "AllocationRules" r WHERE r."Id"=NEW."RuleId" AND r."Status"=1) THEN RAISE EXCEPTION 'Invalid budget category or rule' USING ERRCODE='23514'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER budget_guard BEFORE INSERT OR UPDATE OR DELETE ON "Budgets" FOR EACH ROW EXECUTE FUNCTION cm_budget_guard();
CREATE TRIGGER budget_line_guard BEFORE INSERT OR UPDATE OR DELETE ON "BudgetLines" FOR EACH ROW EXECUTE FUNCTION cm_budget_guard();
""");
}
