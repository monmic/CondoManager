using Microsoft.EntityFrameworkCore.Migrations;

namespace CondoManager.Infrastructure.Migrations;

public partial class RegistryMilestone02
{
    private static void AddRegistryConstraints(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE "Buildings" SET "Name" = "Code" WHERE "Name" = '';
        UPDATE "AccountingPeriods" SET "Name" = "Code" WHERE "Name" = '';
        UPDATE "People" SET "LastNameOrCompany" = "DisplayName" WHERE "LastNameOrCompany" IS NULL;
        UPDATE "MillesimalTables" t SET "Name" = t."Code",
            "ValidFrom" = COALESCE((SELECT min(s."ValidFrom") FROM "MillesimalShares" s WHERE s."MillesimalTableId" = t."Id"), DATE '0001-01-01');
        CREATE EXTENSION IF NOT EXISTS btree_gist;
        ALTER TABLE "AccountingPeriods" ADD CONSTRAINT "EX_AccountingPeriods_NoOverlap"
            EXCLUDE USING gist ("TenantId" WITH =, "CondominiumId" WITH =, daterange("StartsOn", "EndsOn", '[]') WITH &&);
        ALTER TABLE "OwnershipPeriods" ADD CONSTRAINT "EX_OwnershipPeriods_NoOverlap"
            EXCLUDE USING gist ("TenantId" WITH =, "PropertyUnitId" WITH =, "PersonId" WITH =, "Role" WITH =,
                daterange("ValidFrom", "ValidTo", '[]') WITH &&);

        CREATE FUNCTION cm_protect_closed_period() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF OLD."Status" = 2 THEN
                RAISE EXCEPTION 'Closed accounting period is immutable' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_AccountingPeriods_Closed" BEFORE UPDATE OR DELETE ON "AccountingPeriods"
            FOR EACH ROW EXECUTE FUNCTION cm_protect_closed_period();

        CREATE FUNCTION cm_protect_active_table() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF OLD."Status" = 1 THEN
                RAISE EXCEPTION 'Active allocation table is immutable' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_AllocationTables_Active" BEFORE UPDATE OR DELETE ON "MillesimalTables"
            FOR EACH ROW EXECUTE FUNCTION cm_protect_active_table();

        CREATE FUNCTION cm_protect_active_entry() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF TG_OP <> 'INSERT' AND EXISTS (SELECT 1 FROM "MillesimalTables" WHERE "Id" = OLD."MillesimalTableId" AND "Status" = 1) THEN
                RAISE EXCEPTION 'Active allocation entries are immutable' USING ERRCODE = '55000';
            END IF;
            IF TG_OP <> 'DELETE' AND EXISTS (SELECT 1 FROM "MillesimalTables" WHERE "Id" = NEW."MillesimalTableId" AND "Status" = 1) THEN
                RAISE EXCEPTION 'Active allocation entries are immutable' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_AllocationEntries_Active" BEFORE INSERT OR UPDATE OR DELETE ON "MillesimalShares"
            FOR EACH ROW EXECUTE FUNCTION cm_protect_active_entry();

        CREATE FUNCTION cm_check_unit_staircase() RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE staircase_building uuid;
        BEGIN
            IF NEW."StaircaseId" IS NOT NULL THEN
                SELECT "BuildingId" INTO staircase_building FROM "Staircases"
                WHERE "Id" = NEW."StaircaseId" AND "TenantId" = NEW."TenantId" AND "CondominiumId" = NEW."CondominiumId";
                IF staircase_building IS NOT NULL AND NEW."BuildingOrStaircaseId" IS DISTINCT FROM staircase_building THEN
                    RAISE EXCEPTION 'Staircase and building mismatch' USING ERRCODE = '23514';
                END IF;
            END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_PropertyUnits_Staircase" BEFORE INSERT OR UPDATE ON "PropertyUnits"
            FOR EACH ROW EXECUTE FUNCTION cm_check_unit_staircase();
        """);
}