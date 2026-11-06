using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class InstallmentsReceiptsMilestone062
{
    private static void FinanceConstraints(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "Accounts" DROP CONSTRAINT cm_control_account_type;
ALTER TABLE "Accounts" ADD CONSTRAINT cm_control_account_type CHECK (("Purpose" IN (1,2) AND "Type"=1 AND "IsActive") OR ("Purpose" IN (3,5) AND "Type"=2 AND "IsActive") OR ("Purpose"=4 AND "Type"=0 AND "IsActive") OR "Purpose"=0);
CREATE FUNCTION cm_resident_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p record; receipt record; bank record; financial record;
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Resident finance history cannot be deleted' USING ERRCODE='55000'; END IF;
 PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" AND "IsActive" FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'Inactive condominium' USING ERRCODE='23514'; END IF;
 IF TG_TABLE_NAME='InstallmentPlans' THEN
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" ap WHERE ap."Id"=NEW."AccountingPeriodId" AND ap."Status"=1) THEN RAISE EXCEPTION 'Period closed' USING ERRCODE='55000'; END IF;
  IF TG_OP='INSERT' THEN
   NEW."CreationTransactionId":=txid_current();
   IF NEW."Status"<>0 OR NOT EXISTS(SELECT 1 FROM "Budgets" b WHERE b."Id"=NEW."BudgetId" AND b."AccountingPeriodId"=NEW."AccountingPeriodId" AND b."Status"=2) THEN RAISE EXCEPTION 'Approved current budget required' USING ERRCODE='23514'; END IF;
   IF NEW."PreviousPlanId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "InstallmentPlans" prior WHERE prior."Id"=NEW."PreviousPlanId" AND prior."AccountingPeriodId"=NEW."AccountingPeriodId" AND prior."Version"=NEW."Version"-1) THEN RAISE EXCEPTION 'Invalid previous plan' USING ERRCODE='23514'; END IF;
  ELSE
   IF OLD."Status"<>0 OR NEW."Status" NOT IN (1,2) OR (to_jsonb(NEW)-ARRAY['Status','JournalEntryId','ApprovedBy','ApprovedAt','UpdatedAt']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','JournalEntryId','ApprovedBy','ApprovedAt','UpdatedAt']) THEN RAISE EXCEPTION 'Plan immutable; create a new version' USING ERRCODE='55000'; END IF;
   IF NEW."Status"=1 AND NOT EXISTS(SELECT 1 FROM "Budgets" b WHERE b."Id"=NEW."BudgetId" AND b."Status"=2) THEN RAISE EXCEPTION 'Budget superseded' USING ERRCODE='23514'; END IF;
  END IF;
  RETURN NEW;
 END IF;
 IF TG_TABLE_NAME IN ('PlanSchedules','InstallmentDebtors','Installments') THEN
  SELECT * INTO p FROM "InstallmentPlans" WHERE "Id"=NEW."PlanId";
  IF TG_OP<>'INSERT' OR p."CreationTransactionId"<>txid_current() OR p."Status"<>0 THEN RAISE EXCEPTION 'Plan details immutable' USING ERRCODE='55000'; END IF;
  IF TG_TABLE_NAME='PlanSchedules' THEN
   IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" ap WHERE ap."Id"=p."AccountingPeriodId" AND NEW."DueDate" BETWEEN ap."StartsOn" AND ap."EndsOn") THEN RAISE EXCEPTION 'Invalid due date' USING ERRCODE='23514'; END IF;
  END IF;
  RETURN NEW;
 END IF;
 IF TG_TABLE_NAME='ResidentReceipts' THEN
  IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Receipt immutable' USING ERRCODE='55000'; END IF;
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriods" ap WHERE ap."Id"=NEW."AccountingPeriodId" AND ap."Status"=1 AND NEW."PaymentDate" BETWEEN ap."StartsOn" AND ap."EndsOn") THEN RAISE EXCEPTION 'Receipt period closed or invalid date' USING ERRCODE='23514'; END IF;
  RETURN NEW;
 END IF;
 IF TG_TABLE_NAME='ReceiptBankLinks' THEN
  IF TG_OP='UPDATE' AND (OLD."Status"<>0 OR NEW."Status"<>1 OR (to_jsonb(NEW)-ARRAY['Status','UpdatedAt']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','UpdatedAt'])) THEN RAISE EXCEPTION 'Bank link immutable after reconciliation' USING ERRCODE='55000'; END IF;
  SELECT * INTO receipt FROM "ResidentReceipts" WHERE "Id"=NEW."ReceiptId";
  SELECT * INTO bank FROM "JournalEntries" WHERE "Id"=NEW."JournalEntryId";
  SELECT * INTO financial FROM "FinancialAccounts" WHERE "Id"=receipt."FinancialAccountId";
  IF bank."Kind"<>3 OR bank."AccountingPeriodId"<>receipt."AccountingPeriodId" OR bank."Date"<>receipt."PaymentDate" OR EXISTS(SELECT 1 FROM "JournalEntries" r WHERE r."ReversesEntryId"=bank."Id") OR NOT EXISTS(SELECT 1 FROM "AccountingPeriods" ap WHERE ap."Id"=receipt."AccountingPeriodId" AND ap."Status"=1) THEN RAISE EXCEPTION 'Bank link context mismatch' USING ERRCODE='23514'; END IF;
  RETURN NEW;
 END IF;
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Allocation immutable; reverse explicitly' USING ERRCODE='55000'; END IF;
 IF NOT EXISTS(SELECT 1 FROM "JournalEntries" j WHERE j."Id"=NEW."JournalEntryId" AND j."CreationTransactionId"=txid_current()) THEN RAISE EXCEPTION 'Allocation requires new journal in same transaction' USING ERRCODE='55000'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER cm_plan_guard BEFORE INSERT OR UPDATE OR DELETE ON "InstallmentPlans" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_schedule_guard BEFORE INSERT OR UPDATE OR DELETE ON "PlanSchedules" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_debtor_guard BEFORE INSERT OR UPDATE OR DELETE ON "InstallmentDebtors" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_installment_guard BEFORE INSERT OR UPDATE OR DELETE ON "Installments" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_receipt_guard BEFORE INSERT OR UPDATE OR DELETE ON "ResidentReceipts" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_bank_link_guard BEFORE INSERT OR UPDATE OR DELETE ON "ReceiptBankLinks" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();
CREATE TRIGGER cm_allocation_guard BEFORE INSERT OR UPDATE OR DELETE ON "ReceiptAllocations" FOR EACH ROW EXECUTE FUNCTION cm_resident_guard();

CREATE FUNCTION cm_resident_plan_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p record; total numeric; journal record;
BEGIN
 SELECT * INTO p FROM "InstallmentPlans" WHERE "Id"=NEW."Id";
 SELECT COALESCE(sum("Amount"),0) INTO total FROM "Installments" WHERE "PlanId"=p."Id";
 IF total<>p."Total" OR total<>(SELECT COALESCE(sum(bl."Amount"),0) FROM "BudgetLines" bl WHERE bl."BudgetId"=p."BudgetId") OR NOT EXISTS(SELECT 1 FROM "PlanSchedules" WHERE "PlanId"=p."Id") OR NOT EXISTS(SELECT 1 FROM "InstallmentDebtors" WHERE "PlanId"=p."Id") OR EXISTS(SELECT 1 FROM "InstallmentDebtors" d WHERE d."PlanId"=p."Id" AND (SELECT COALESCE(sum(i."Amount"),0) FROM "Installments" i WHERE i."InstallmentDebtorId"=d."Id")<>d."Amount") OR EXISTS(SELECT 1 FROM "InstallmentDebtors" d CROSS JOIN "PlanSchedules" s WHERE d."PlanId"=p."Id" AND s."PlanId"=p."Id" AND NOT EXISTS(SELECT 1 FROM "Installments" i WHERE i."InstallmentDebtorId"=d."Id" AND i."ScheduleId"=s."Id")) THEN RAISE EXCEPTION 'Plan or individual installments unbalanced' USING ERRCODE='23514'; END IF;
 IF p."JournalEntryId" IS NOT NULL THEN
  SELECT * INTO journal FROM "JournalEntries" WHERE "Id"=p."JournalEntryId";
  IF journal."Kind"<>6 OR journal."AccountingPeriodId"<>p."AccountingPeriodId" THEN RAISE EXCEPTION 'Assessment journal context mismatch' USING ERRCODE='23514'; END IF;
 END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_plan_valid AFTER INSERT OR UPDATE ON "InstallmentPlans" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_resident_plan_valid();

CREATE FUNCTION cm_resident_link_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE receipt record; financial record;
BEGIN
 SELECT * INTO receipt FROM "ResidentReceipts" WHERE "Id"=NEW."ReceiptId";
 SELECT * INTO financial FROM "FinancialAccounts" WHERE "Id"=receipt."FinancialAccountId";
 IF (SELECT COALESCE(sum(l."Debit"),0) FROM "JournalEntryLines" l WHERE l."JournalEntryId"=NEW."JournalEntryId")<>receipt."Amount" OR (SELECT COALESCE(sum(l."Debit"-l."Credit"),0) FROM "JournalEntryLines" l WHERE l."JournalEntryId"=NEW."JournalEntryId" AND l."AccountId"=financial."AccountId")<>receipt."Amount" THEN RAISE EXCEPTION 'Bank amount mismatch' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_link_valid AFTER INSERT OR UPDATE ON "ReceiptBankLinks" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_resident_link_valid();

CREATE FUNCTION cm_resident_allocation_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE receipt record; installment record; plan record; debtor record; journal record; original record; total numeric;
BEGIN
 SELECT * INTO receipt FROM "ResidentReceipts" WHERE "Id"=NEW."ReceiptId";
 SELECT * INTO installment FROM "Installments" WHERE "Id"=NEW."InstallmentId";
 SELECT * INTO plan FROM "InstallmentPlans" WHERE "Id"=installment."PlanId";
 SELECT * INTO debtor FROM "InstallmentDebtors" WHERE "Id"=installment."InstallmentDebtorId";
 SELECT * INTO journal FROM "JournalEntries" WHERE "Id"=NEW."JournalEntryId";
 IF plan."JournalEntryId" IS NULL OR EXISTS(SELECT 1 FROM "JournalEntries" assessment WHERE assessment."Id"=plan."JournalEntryId" AND assessment."Date">journal."Date") OR (receipt."DebtorId" IS NOT NULL AND receipt."DebtorId"<>debtor."PersonId") OR NOT EXISTS(SELECT 1 FROM "ReceiptBankLinks" l WHERE l."ReceiptId"=receipt."Id" AND l."Status"=1) OR journal."Date"<receipt."PaymentDate" THEN RAISE EXCEPTION 'Invalid receipt allocation context' USING ERRCODE='23514'; END IF;
 IF NEW."ReversesAllocationId" IS NULL THEN
  IF journal."Kind"<>7 THEN RAISE EXCEPTION 'Attribution journal required' USING ERRCODE='23514'; END IF;
 ELSE
  SELECT * INTO original FROM "ReceiptAllocations" WHERE "Id"=NEW."ReversesAllocationId";
  IF original."ReversesAllocationId" IS NOT NULL OR original."ReceiptId"<>NEW."ReceiptId" OR original."InstallmentId"<>NEW."InstallmentId" OR original."Amount"<>NEW."Amount" OR journal."Kind"<>5 OR journal."ReversesEntryId"<>original."JournalEntryId" THEN RAISE EXCEPTION 'Invalid allocation reversal' USING ERRCODE='23514'; END IF;
 END IF;
 SELECT COALESCE(sum(CASE WHEN a."ReversesAllocationId" IS NULL THEN a."Amount" ELSE -a."Amount" END),0) INTO total FROM "ReceiptAllocations" a WHERE a."ReceiptId"=receipt."Id";
 IF total<0 OR total>receipt."Amount" THEN RAISE EXCEPTION 'Receipt allocation exceeds receipt' USING ERRCODE='23514'; END IF;
 SELECT COALESCE(sum(CASE WHEN a."ReversesAllocationId" IS NULL THEN a."Amount" ELSE -a."Amount" END),0) INTO total FROM "ReceiptAllocations" a WHERE a."InstallmentId"=installment."Id";
 IF total<0 OR total>installment."Amount" THEN RAISE EXCEPTION 'Installment overpayment' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_allocation_valid AFTER INSERT ON "ReceiptAllocations" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_resident_allocation_valid();

CREATE FUNCTION cm_resident_journal_valid() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE total numeric; original record; p record;
BEGIN
 SELECT sum("Debit") INTO total FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id";
 IF NEW."Kind" NOT IN (5,6,7) AND EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose" IN (4,5)) THEN RAISE EXCEPTION 'Resident control accounts require dedicated workflow' USING ERRCODE='23514'; END IF;
 IF NEW."Kind"=6 THEN
  SELECT * INTO p FROM "InstallmentPlans" WHERE "JournalEntryId"=NEW."Id";
  IF NOT FOUND OR p."Total"<>total OR (SELECT count(*) FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id")<>2 OR NOT EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=4 AND l."Debit"=total) OR NOT EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=5 AND l."Credit"=total) THEN RAISE EXCEPTION 'Assessment mismatch' USING ERRCODE='23514'; END IF;
 END IF;
 IF NEW."Kind"=7 THEN
  IF total<>(SELECT COALESCE(sum(a."Amount"),0) FROM "ReceiptAllocations" a WHERE a."JournalEntryId"=NEW."Id" AND a."ReversesAllocationId" IS NULL) OR (SELECT count(DISTINCT "ReceiptId") FROM "ReceiptAllocations" WHERE "JournalEntryId"=NEW."Id")<>1 OR (SELECT count(*) FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id")<>2 OR NOT EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=2 AND l."Debit"=total) OR NOT EXISTS(SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=4 AND l."Credit"=total) THEN RAISE EXCEPTION 'Receipt attribution journal mismatch' USING ERRCODE='23514'; END IF;
 END IF;
 IF NEW."Kind"=5 THEN
  SELECT * INTO original FROM "JournalEntries" WHERE "Id"=NEW."ReversesEntryId";
  IF original."Kind"=6 OR EXISTS(SELECT 1 FROM "ReceiptBankLinks" l WHERE l."JournalEntryId"=original."Id") THEN RAISE EXCEPTION 'Linked bank or assessment cannot be reversed directly' USING ERRCODE='55000'; END IF;
  IF original."Kind"=7 AND EXISTS(SELECT 1 FROM "ReceiptAllocations" a WHERE a."JournalEntryId"=original."Id" AND NOT EXISTS(SELECT 1 FROM "ReceiptAllocations" r WHERE r."ReversesAllocationId"=a."Id" AND r."JournalEntryId"=NEW."Id")) THEN RAISE EXCEPTION 'Reverse all allocation details atomically' USING ERRCODE='23514'; END IF;
 END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_resident_journal_valid AFTER INSERT ON "JournalEntries" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_resident_journal_valid();
""");
}
