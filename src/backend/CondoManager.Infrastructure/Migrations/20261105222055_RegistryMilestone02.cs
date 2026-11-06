using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegistryMilestone02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwnershipPeriods_TenantId_PropertyUnitId_PersonId_ValidFrom",
                table: "OwnershipPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ownership_Fraction",
                table: "OwnershipPeriods");

            migrationBuilder.AddColumn<string>(
                name: "CadastralData",
                table: "PropertyUnits",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "PropertyUnits",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interior",
                table: "PropertyUnits",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PropertyUnits",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "PropertyUnits",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PropertyUnits",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StaircaseId",
                table: "PropertyUnits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "People",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "People",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "People",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "People",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastNameOrCompany",
                table: "People",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingAddress",
                table: "People",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "People",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "People",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "People",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "People",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Fraction",
                table: "OwnershipPeriods",
                type: "numeric(18,10)",
                precision: 18,
                scale: 10,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,10)",
                oldPrecision: 18,
                oldScale: 10);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "OwnershipPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Denominator",
                table: "MillesimalTables",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1000m);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "MillesimalTables",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "MillesimalTables",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MillesimalTables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ValidFrom",
                table: "MillesimalTables",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Condominiums",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Condominiums",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Buildings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "AccountingPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AccountingPeriods",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AccountingPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Staircases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staircases", x => x.Id);
                    table.UniqueConstraint("AK_Staircases_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Staircases_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Staircases_Buildings_TenantId_CondominiumId_BuildingId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.BuildingId },
                        principalTable: "Buildings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Staircases_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Staircases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnits_TenantId_CondominiumId_StaircaseId",
                table: "PropertyUnits",
                columns: new[] { "TenantId", "CondominiumId", "StaircaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_People_TenantId_TaxCode",
                table: "People",
                columns: new[] { "TenantId", "TaxCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnershipPeriods_TenantId_PropertyUnitId_PersonId_Role_Vali~",
                table: "OwnershipPeriods",
                columns: new[] { "TenantId", "PropertyUnitId", "PersonId", "Role", "ValidFrom" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ownership_Fraction",
                table: "OwnershipPeriods",
                sql: "(\"Role\" IN (0,1) AND \"Fraction\" IS NOT NULL AND \"Fraction\" > 0 AND \"Fraction\" <= 1) OR (\"Role\" IN (2,3,4) AND \"Fraction\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Table_Denominator",
                table: "MillesimalTables",
                sql: "\"Denominator\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Condominiums_TenantId_TaxCode",
                table: "Condominiums",
                columns: new[] { "TenantId", "TaxCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staircases_TenantId_CondominiumId_BuildingId",
                table: "Staircases",
                columns: new[] { "TenantId", "CondominiumId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_Staircases_TenantId_CondominiumId_Code",
                table: "Staircases",
                columns: new[] { "TenantId", "CondominiumId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyUnits_Staircases_TenantId_CondominiumId_StaircaseId",
                table: "PropertyUnits",
                columns: new[] { "TenantId", "CondominiumId", "StaircaseId" },
                principalTable: "Staircases",
                principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                onDelete: ReferentialAction.Restrict);


            AddRegistryConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Migrazione forward-only: il downgrade perderebbe storico e campi anagrafici.");
        }
    }
}