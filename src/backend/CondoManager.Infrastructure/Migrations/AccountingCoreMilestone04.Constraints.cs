using Microsoft.EntityFrameworkCore.Migrations;
namespace CondoManager.Infrastructure.Migrations;
public partial class AccountingCoreMilestone04
{
    private static void AccountingConstraints(MigrationBuilder m) => m.Sql("""
CREATE UNIQUE INDEX cm_expense_registered_once ON "JournalEntries" ("TenantId","CondominiumId","ExpenseDocumentId") WHERE "Kind"=1;
ALTER TABLE "Accounts" ADD CONSTRAINT cm_control_account_type CHECK (("Purpose" IN (1,2) AND "Type"=1 AND "IsActive") OR ("Purpose"=3 AND "Type"=2 AND "IsActive") OR "Purpose"=0);
CREATE FUNCTION cm_ledger_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Posted accounting history is immutable' USING ERRCODE='55000'; END $$;
CREATE TRIGGER cm_journal_immutable BEFORE UPDATE OR DELETE ON "JournalEntries" FOR EACH ROW EXECUTE FUNCTION cm_ledger_immutable();
CREATE TRIGGER cm_line_immutable BEFORE UPDATE OR DELETE ON "JournalEntryLines" FOR EACH ROW EXECUTE FUNCTION cm_ledger_immutable();
CREATE TRIGGER cm_payment_immutable BEFORE UPDATE OR DELETE ON "ExpensePayments" FOR EACH ROW EXECUTE FUNCTION cm_ledger_immutable();

CREATE FUNCTION cm_journal_insert() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p record;
BEGIN
  NEW."CreationTransactionId" := txid_current();
  PERFORM 1 FROM "Condominiums" WHERE "Id"=NEW."CondominiumId" AND "TenantId"=NEW."TenantId" AND "IsActive" FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Inactive or missing condominium' USING ERRCODE='23514'; END IF;
  SELECT * INTO p FROM "AccountingPeriods" WHERE "Id"=NEW."AccountingPeriodId" AND "TenantId"=NEW."TenantId" AND "CondominiumId"=NEW."CondominiumId";
  IF NOT FOUND OR p."Status"<>1 OR NEW."Date"<p."StartsOn" OR NEW."Date">p."EndsOn" THEN
    RAISE EXCEPTION 'Posting requires an open accounting period and valid date' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER cm_journal_insert BEFORE INSERT ON "JournalEntries" FOR EACH ROW EXECUTE FUNCTION cm_journal_insert();

CREATE FUNCTION cm_line_insert() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "JournalEntries" WHERE "Id"=NEW."JournalEntryId" AND "CreationTransactionId"=txid_current()) THEN
    RAISE EXCEPTION 'Lines may only be inserted with their new journal' USING ERRCODE='55000';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM "Accounts" WHERE "Id"=NEW."AccountId" AND "IsActive") THEN
    RAISE EXCEPTION 'Inactive account' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER cm_line_insert BEFORE INSERT ON "JournalEntryLines" FOR EACH ROW EXECUTE FUNCTION cm_line_insert();

CREATE FUNCTION cm_journal_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE n bigint; d numeric; c numeric; original record;
BEGIN
  SELECT count(*),sum("Debit"),sum("Credit") INTO n,d,c FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id";
  IF n<2 OR d<>c THEN RAISE EXCEPTION 'Unbalanced journal' USING ERRCODE='23514'; END IF;
  IF NEW."Kind"=0 AND EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose" IN (1,2)) THEN
    RAISE EXCEPTION 'Control accounts require dedicated workflows' USING ERRCODE='23514';
  END IF;
  IF NEW."Kind"=1 AND NOT EXISTS (
    SELECT 1 FROM "ExpenseDocuments" e WHERE e."Id"=NEW."ExpenseDocumentId" AND e."IsRegistered" AND NOT e."IsCancelled" AND e."AccountingPeriodId"=NEW."AccountingPeriodId" AND e."DocumentDate"=NEW."Date" AND e."GrossAmount"=d
      AND (SELECT count(*) FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id")=2
      AND EXISTS (SELECT 1 FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id" AND "AccountId"=e."AccountId" AND "Debit"=d)
      AND EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=1 AND l."Credit"=d)
  ) THEN RAISE EXCEPTION 'Expense journal mismatch' USING ERRCODE='23514'; END IF;
  IF NEW."Kind"=2 AND NOT EXISTS (SELECT 1 FROM "ExpensePayments" WHERE "JournalEntryId"=NEW."Id") THEN
    RAISE EXCEPTION 'Payment journal requires its payment' USING ERRCODE='23514';
  END IF;
  IF NEW."Kind"=3 AND (n<>2 OR NOT EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "FinancialAccounts" f ON f."AccountId"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND f."IsActive" AND l."Debit"=d) OR NOT EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=2 AND l."Credit"=d)) THEN
    RAISE EXCEPTION 'Receipt journal mismatch' USING ERRCODE='23514';
  END IF;
  IF NEW."Kind"=4 AND (n<>2 OR NOT EXISTS (SELECT 1 FROM "FinancialAccounts" f JOIN "JournalEntryLines" l ON l."AccountId"=f."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND f."CreationRequestId"=NEW."RequestId" AND l."Debit"-l."Credit"=f."OpeningBalance") OR NOT EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=NEW."Id" AND a."Purpose"=3)) THEN
    RAISE EXCEPTION 'Opening journal mismatch' USING ERRCODE='23514';
  END IF;
  IF NEW."Kind"=5 THEN
    SELECT * INTO original FROM "JournalEntries" WHERE "Id"=NEW."ReversesEntryId";
    IF NOT FOUND OR original."Kind" = 5 OR NEW."Date"<original."Date" OR EXISTS (
      SELECT 1 FROM (
        (SELECT "AccountId",sum("Debit") d,sum("Credit") c FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id" GROUP BY "AccountId"
         EXCEPT SELECT "AccountId",sum("Credit"),sum("Debit") FROM "JournalEntryLines" WHERE "JournalEntryId"=original."Id" GROUP BY "AccountId")
        UNION ALL
        (SELECT "AccountId",sum("Credit"),sum("Debit") FROM "JournalEntryLines" WHERE "JournalEntryId"=original."Id" GROUP BY "AccountId"
         EXCEPT SELECT "AccountId",sum("Debit"),sum("Credit") FROM "JournalEntryLines" WHERE "JournalEntryId"=NEW."Id" GROUP BY "AccountId")
      ) mismatch
    ) THEN RAISE EXCEPTION 'Invalid reversal' USING ERRCODE='23514'; END IF;
  END IF;
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_journal_balanced AFTER INSERT ON "JournalEntries" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_journal_validate();

CREATE FUNCTION cm_expense_protect() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP='DELETE' OR OLD."IsCancelled" OR (OLD."IsRegistered" AND (to_jsonb(NEW)-'IsCancelled'-'UpdatedAt')<>(to_jsonb(OLD)-'IsCancelled'-'UpdatedAt')) THEN
    RAISE EXCEPTION 'Expense history is immutable' USING ERRCODE='55000';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER cm_expense_protect BEFORE UPDATE OR DELETE ON "ExpenseDocuments" FOR EACH ROW EXECUTE FUNCTION cm_expense_protect();

CREATE FUNCTION cm_expense_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE doc record; original_id uuid;
BEGIN
  SELECT * INTO doc FROM "ExpenseDocuments" WHERE "Id"=NEW."Id";
  IF doc."IsRegistered" THEN
    SELECT "Id" INTO original_id FROM "JournalEntries" WHERE "ExpenseDocumentId"=doc."Id" AND "Kind"=1;
    IF original_id IS NULL THEN RAISE EXCEPTION 'Registered expense requires its journal' USING ERRCODE='23514'; END IF;
    IF doc."IsCancelled" AND (NOT EXISTS (SELECT 1 FROM "JournalEntries" WHERE "ReversesEntryId"=original_id) OR EXISTS (SELECT 1 FROM "ExpensePayments" p WHERE p."ExpenseDocumentId"=doc."Id" AND NOT EXISTS (SELECT 1 FROM "JournalEntries" r WHERE r."ReversesEntryId"=p."JournalEntryId"))) THEN
      RAISE EXCEPTION 'Cancel expense only by reversal after reversing payments' USING ERRCODE='23514';
    END IF;
  END IF;
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_expense_valid AFTER INSERT OR UPDATE ON "ExpenseDocuments" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_expense_validate();
CREATE FUNCTION cm_account_protect() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP='DELETE' OR NEW."Type"<>OLD."Type" OR NEW."Purpose"<>OLD."Purpose" OR NEW."TenantId"<>OLD."TenantId" OR NEW."CondominiumId"<>OLD."CondominiumId" THEN
    RAISE EXCEPTION 'Account identity and type are immutable' USING ERRCODE='55000';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER cm_account_protect BEFORE UPDATE OR DELETE ON "Accounts" FOR EACH ROW EXECUTE FUNCTION cm_account_protect();
CREATE FUNCTION cm_financial_protect() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP='DELETE' OR (to_jsonb(NEW)-'Name'-'Iban'-'IsActive'-'UpdatedAt')<>(to_jsonb(OLD)-'Name'-'Iban'-'IsActive'-'UpdatedAt') THEN
    RAISE EXCEPTION 'Financial opening and ledger link are immutable' USING ERRCODE='55000';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER cm_financial_protect BEFORE UPDATE OR DELETE ON "FinancialAccounts" FOR EACH ROW EXECUTE FUNCTION cm_financial_protect();

CREATE FUNCTION cm_payment_validate() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE doc record; paid numeric; journal record; financial record;
BEGIN
  SELECT * INTO doc FROM "ExpenseDocuments" WHERE "Id"=NEW."ExpenseDocumentId";
  SELECT * INTO journal FROM "JournalEntries" WHERE "Id"=NEW."JournalEntryId";
  SELECT * INTO financial FROM "FinancialAccounts" WHERE "Id"=NEW."FinancialAccountId";
  IF NOT doc."IsRegistered" OR doc."IsCancelled" OR journal."Kind"<>2 OR journal."ExpenseDocumentId"<>doc."Id" OR NOT financial."IsActive" OR journal."Date"<doc."DocumentDate" THEN
    RAISE EXCEPTION 'Invalid payment context' USING ERRCODE='23514';
  END IF;
  SELECT COALESCE(sum(p."Amount"),0) INTO paid FROM "ExpensePayments" p WHERE p."ExpenseDocumentId"=doc."Id" AND NOT EXISTS (SELECT 1 FROM "JournalEntries" r WHERE r."ReversesEntryId"=p."JournalEntryId");
  IF (SELECT count(*) FROM "JournalEntryLines" WHERE "JournalEntryId"=journal."Id")<>2 OR NOT EXISTS (SELECT 1 FROM "JournalEntryLines" l JOIN "Accounts" a ON a."Id"=l."AccountId" WHERE l."JournalEntryId"=journal."Id" AND a."Purpose"=1 AND l."Debit"=NEW."Amount") OR paid>doc."GrossAmount" OR (SELECT COALESCE(sum("Credit"-"Debit"),0) FROM "JournalEntryLines" WHERE "JournalEntryId"=journal."Id" AND "AccountId"=financial."AccountId")<>NEW."Amount" THEN
    RAISE EXCEPTION 'Overpayment or inconsistent financial journal' USING ERRCODE='23514';
  END IF;
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER cm_payment_valid AFTER INSERT ON "ExpensePayments" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cm_payment_validate();
""");
}
