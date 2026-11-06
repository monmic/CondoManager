using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseAllocationMilestone05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllocationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationGroups", x => x.Id);
                    table.UniqueConstraint("AK_AllocationGroups_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationGroups_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AllocationGroups_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationGroups_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRules", x => x.Id);
                    table.UniqueConstraint("AK_AllocationRules_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationRules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Rule_Status", "\"Status\" IN (0,1) AND \"Type\" BETWEEN 0 AND 4 AND \"Version\">0");
                    table.ForeignKey(
                        name: "FK_AllocationRules_AllocationRules_TenantId_CondominiumId_Prev~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousVersionId },
                        principalTable: "AllocationRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRules_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PreviewHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetenceFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CompetenceTo = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreationTransactionId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "txid_current()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRuns", x => x.Id);
                    table.UniqueConstraint("AK_AllocationRuns_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationRuns_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Run_Total", "\"TotalAmount\">0 AND \"Version\">0 AND \"CompetenceTo\">=\"CompetenceFrom\"");
                    table.ForeignKey(
                        name: "FK_AllocationRuns_AccountingPeriods_TenantId_CondominiumId_Acc~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuns_AllocationRuns_TenantId_CondominiumId_Previo~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousRunId },
                        principalTable: "AllocationRuns",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuns_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumptionReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumptionType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MeterCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Boundary = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IsEstimated = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ReplacesReadingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionReadings", x => x.Id);
                    table.UniqueConstraint("AK_ConsumptionReadings_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ConsumptionReadings_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Reading_Value", "\"Value\">=0 AND \"Boundary\" IN (0,1) AND ((NOT \"IsEstimated\" AND \"ReplacesReadingId\" IS NULL) OR \"Reason\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ConsumptionReadings_AccountingPeriods_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumptionReadings_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumptionReadings_ConsumptionReadings_TenantId_Condominiu~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReplacesReadingId },
                        principalTable: "ConsumptionReadings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumptionReadings_PropertyUnits_TenantId_CondominiumId_Pr~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumptionReadings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationGroupMembers", x => x.Id);
                    table.UniqueConstraint("AK_AllocationGroupMembers_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationGroupMembers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AllocationGroupMembers_AllocationGroups_TenantId_Condominiu~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.GroupId },
                        principalTable: "AllocationGroups",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationGroupMembers_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationGroupMembers_PropertyUnits_TenantId_CondominiumId~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationGroupMembers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpenseDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationAssignments", x => x.Id);
                    table.UniqueConstraint("AK_AllocationAssignments_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationAssignments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Assignment_Target", "(\"AccountId\" IS NULL) <> (\"ExpenseDocumentId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_AllocationAssignments_Accounts_TenantId_CondominiumId_Accou~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationAssignments_AllocationRules_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "AllocationRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationAssignments_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationAssignments_ExpenseDocuments_TenantId_Condominium~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ExpenseDocumentId },
                        principalTable: "ExpenseDocuments",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRuleComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Criterion = table.Column<int>(type: "integer", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    TableId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumptionType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    NormalizeAdmitted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRuleComponents", x => x.Id);
                    table.UniqueConstraint("AK_AllocationRuleComponents_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationRuleComponents_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Component_Percentage", "\"Percentage\">0 AND \"Percentage\"<=100 AND \"Position\">=0 AND \"Criterion\" IN (0,1,2,4)");
                    table.ForeignKey(
                        name: "FK_AllocationRuleComponents_AllocationGroups_TenantId_Condomin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.GroupId },
                        principalTable: "AllocationGroups",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuleComponents_AllocationRules_TenantId_Condomini~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "AllocationRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuleComponents_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuleComponents_MillesimalTables_TenantId_Condomin~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.TableId },
                        principalTable: "MillesimalTables",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRuleComponents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRunSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRunSources", x => x.Id);
                    table.UniqueConstraint("AK_AllocationRunSources_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationRunSources_TenantId_CondominiumId_RunId_Id", x => new { x.TenantId, x.CondominiumId, x.RunId, x.Id });
                    table.UniqueConstraint("AK_AllocationRunSources_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_RunSource_Amount", "\"Amount\">0 AND \"Position\">=0");
                    table.ForeignKey(
                        name: "FK_AllocationRunSources_AllocationRules_TenantId_CondominiumId~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "AllocationRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunSources_AllocationRuns_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RunId },
                        principalTable: "AllocationRuns",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunSources_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunSources_ExpenseDocuments_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ExpenseDocumentId },
                        principalTable: "ExpenseDocuments",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunSources_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllocationRunLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ComponentPosition = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnroundedAmount = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    RoundingAdjustment = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationRunLines", x => x.Id);
                    table.UniqueConstraint("AK_AllocationRunLines_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AllocationRunLines_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_RunLine_Amount", "\"Amount\">=0 AND \"BaseAmount\">=0 AND \"Position\">=0 AND \"Amount\"-\"UnroundedAmount\"=\"RoundingAdjustment\"");
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_AllocationRunSources_TenantId_Condominiu~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RunId, x.SourceId },
                        principalTable: "AllocationRunSources",
                        principalColumns: new[] { "TenantId", "CondominiumId", "RunId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_AllocationRuns_TenantId_CondominiumId_Ru~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RunId },
                        principalTable: "AllocationRuns",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_PropertyUnits_TenantId_CondominiumId_Pro~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationRunLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationAssignments_TenantId_CondominiumId",
                table: "AllocationAssignments",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationAssignments_TenantId_CondominiumId_AccountId",
                table: "AllocationAssignments",
                columns: new[] { "TenantId", "CondominiumId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationAssignments_TenantId_CondominiumId_ExpenseDocumen~",
                table: "AllocationAssignments",
                columns: new[] { "TenantId", "CondominiumId", "ExpenseDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationAssignments_TenantId_CondominiumId_RuleId",
                table: "AllocationAssignments",
                columns: new[] { "TenantId", "CondominiumId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationGroupMembers_TenantId_CondominiumId",
                table: "AllocationGroupMembers",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationGroupMembers_TenantId_CondominiumId_GroupId_Prope~",
                table: "AllocationGroupMembers",
                columns: new[] { "TenantId", "CondominiumId", "GroupId", "PropertyUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationGroupMembers_TenantId_CondominiumId_PropertyUnitId",
                table: "AllocationGroupMembers",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationGroups_TenantId_CondominiumId",
                table: "AllocationGroups",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationGroups_TenantId_CondominiumId_Name",
                table: "AllocationGroups",
                columns: new[] { "TenantId", "CondominiumId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleComponents_TenantId_CondominiumId",
                table: "AllocationRuleComponents",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleComponents_TenantId_CondominiumId_GroupId",
                table: "AllocationRuleComponents",
                columns: new[] { "TenantId", "CondominiumId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleComponents_TenantId_CondominiumId_RuleId_Posi~",
                table: "AllocationRuleComponents",
                columns: new[] { "TenantId", "CondominiumId", "RuleId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuleComponents_TenantId_CondominiumId_TableId",
                table: "AllocationRuleComponents",
                columns: new[] { "TenantId", "CondominiumId", "TableId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_PreviousVersionId",
                table: "AllocationRules",
                column: "PreviousVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_TenantId_CondominiumId",
                table: "AllocationRules",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_TenantId_CondominiumId_Name_Version",
                table: "AllocationRules",
                columns: new[] { "TenantId", "CondominiumId", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRules_TenantId_CondominiumId_PreviousVersionId",
                table: "AllocationRules",
                columns: new[] { "TenantId", "CondominiumId", "PreviousVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunLines_TenantId_CondominiumId",
                table: "AllocationRunLines",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunLines_TenantId_CondominiumId_PropertyUnitId",
                table: "AllocationRunLines",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunLines_TenantId_CondominiumId_RunId_Position",
                table: "AllocationRunLines",
                columns: new[] { "TenantId", "CondominiumId", "RunId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunLines_TenantId_CondominiumId_RunId_SourceId",
                table: "AllocationRunLines",
                columns: new[] { "TenantId", "CondominiumId", "RunId", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunLines_TenantId_PersonId",
                table: "AllocationRunLines",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuns_PreviousRunId",
                table: "AllocationRuns",
                column: "PreviousRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuns_TenantId_CondominiumId",
                table: "AllocationRuns",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuns_TenantId_CondominiumId_AccountingPeriodId_Cr~",
                table: "AllocationRuns",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuns_TenantId_CondominiumId_PreviousRunId",
                table: "AllocationRuns",
                columns: new[] { "TenantId", "CondominiumId", "PreviousRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRuns_TenantId_CondominiumId_RequestId",
                table: "AllocationRuns",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunSources_TenantId_CondominiumId",
                table: "AllocationRunSources",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunSources_TenantId_CondominiumId_ExpenseDocument~",
                table: "AllocationRunSources",
                columns: new[] { "TenantId", "CondominiumId", "ExpenseDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunSources_TenantId_CondominiumId_RuleId",
                table: "AllocationRunSources",
                columns: new[] { "TenantId", "CondominiumId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationRunSources_TenantId_CondominiumId_RunId_Position",
                table: "AllocationRunSources",
                columns: new[] { "TenantId", "CondominiumId", "RunId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_ReplacesReadingId",
                table: "ConsumptionReadings",
                column: "ReplacesReadingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_TenantId_CondominiumId",
                table: "ConsumptionReadings",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_TenantId_CondominiumId_AccountingPeriod~",
                table: "ConsumptionReadings",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "PropertyUnitId", "ConsumptionType", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_TenantId_CondominiumId_PropertyUnitId",
                table: "ConsumptionReadings",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionReadings_TenantId_CondominiumId_ReplacesReadingId",
                table: "ConsumptionReadings",
                columns: new[] { "TenantId", "CondominiumId", "ReplacesReadingId" });
            AllocationConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Allocation history must not be deleted by downgrade.");
    }
}