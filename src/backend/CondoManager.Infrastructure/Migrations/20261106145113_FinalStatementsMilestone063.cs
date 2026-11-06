using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalStatementsMilestone063 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Journal_Posted",
                table: "JournalEntries");

            migrationBuilder.CreateTable(
                name: "FinalStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalStatements", x => x.Id);
                    table.UniqueConstraint("AK_FinalStatements_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_FinalStatements_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Final_Status", "\"Version\">0 AND \"Status\" BETWEEN 0 AND 3 AND ((\"Status\" IN (0,1) AND \"ApprovedAt\" IS NULL AND \"ApprovedBy\" IS NULL) OR (\"Status\" IN (2,3) AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedBy\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_FinalStatements_AccountingPeriods_TenantId_CondominiumId_Ac~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalStatements_Budgets_TenantId_CondominiumId_BudgetId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.BudgetId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalStatements_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalStatements_FinalStatements_TenantId_CondominiumId_Prev~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousVersionId },
                        principalTable: "FinalStatements",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalStatements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdministrativeClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeClosures", x => x.Id);
                    table.UniqueConstraint("AK_AdministrativeClosures_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_AdministrativeClosures_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AdministrativeClosures_AccountingPeriods_TenantId_Condomini~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdministrativeClosures_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdministrativeClosures_FinalStatements_TenantId_Condominium~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.StatementId },
                        principalTable: "FinalStatements",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdministrativeClosures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatementSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementSettlements", x => x.Id);
                    table.UniqueConstraint("AK_StatementSettlements_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_StatementSettlements_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Settlement_State", "(\"PostedAt\" IS NULL AND \"PostedBy\" IS NULL AND \"JournalEntryId\" IS NULL AND \"ReversedJournalId\" IS NULL) OR (\"PostedAt\" IS NOT NULL AND \"PostedBy\" IS NOT NULL AND (\"ReversedJournalId\" IS NULL OR \"JournalEntryId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_StatementSettlements_AccountingPeriods_TenantId_Condominium~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementSettlements_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementSettlements_FinalStatements_TenantId_CondominiumId~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.StatementId },
                        principalTable: "FinalStatements",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementSettlements_JournalEntries_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementSettlements_JournalEntries_TenantId_CondominiumId~1",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReversedJournalId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementSettlements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResidentOpenings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    SourceClosureId = table.Column<Guid>(type: "uuid", nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidentOpenings", x => x.Id);
                    table.UniqueConstraint("AK_ResidentOpenings_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ResidentOpenings_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Opening_Mode", "\"Mode\" BETWEEN 0 AND 2 AND ((\"Mode\"=2 AND \"SourceClosureId\" IS NOT NULL AND \"JournalEntryId\" IS NULL) OR (\"Mode\"<>2 AND \"SourceClosureId\" IS NULL))");
                    table.ForeignKey(
                        name: "FK_ResidentOpenings_AccountingPeriods_TenantId_CondominiumId_A~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentOpenings_AdministrativeClosures_TenantId_Condominiu~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.SourceClosureId },
                        principalTable: "AdministrativeClosures",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentOpenings_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentOpenings_JournalEntries_TenantId_CondominiumId_Jour~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentOpenings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SettlementPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PersonName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementPositions", x => x.Id);
                    table.UniqueConstraint("AK_SettlementPositions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_SettlementPositions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_SettlementPositions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementPositions_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementPositions_PropertyUnits_TenantId_CondominiumId_Pr~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementPositions_StatementSettlements_TenantId_Condomini~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.SettlementId },
                        principalTable: "StatementSettlements",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementPositions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpeningPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpeningId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PersonName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpeningPositions", x => x.Id);
                    table.UniqueConstraint("AK_OpeningPositions_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_OpeningPositions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_OpeningPositions_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpeningPositions_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpeningPositions_PropertyUnits_TenantId_CondominiumId_Prope~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpeningPositions_ResidentOpenings_TenantId_CondominiumId_Op~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.OpeningId },
                        principalTable: "ResidentOpenings",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpeningPositions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Journal_Posted",
                table: "JournalEntries",
                sql: "\"IsPosted\" AND \"Kind\" BETWEEN 0 AND 9 AND ((\"Kind\" = 5) = (\"ReversesEntryId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeClosures_TenantId_CondominiumId",
                table: "AdministrativeClosures",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeClosures_TenantId_CondominiumId_AccountingPer~",
                table: "AdministrativeClosures",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeClosures_TenantId_CondominiumId_RequestId",
                table: "AdministrativeClosures",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeClosures_TenantId_CondominiumId_StatementId",
                table: "AdministrativeClosures",
                columns: new[] { "TenantId", "CondominiumId", "StatementId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinalStatements_TenantId_CondominiumId",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinalStatements_TenantId_CondominiumId_AccountingPeriodId_V~",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalStatements_TenantId_CondominiumId_BudgetId",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinalStatements_TenantId_CondominiumId_PreviousVersionId",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId", "PreviousVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinalStatements_TenantId_CondominiumId_RequestId",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Final_Current",
                table: "FinalStatements",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true,
                filter: "\"Status\"=2");

            migrationBuilder.CreateIndex(
                name: "IX_OpeningPositions_OpeningId_PropertyUnitId_PersonId",
                table: "OpeningPositions",
                columns: new[] { "OpeningId", "PropertyUnitId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpeningPositions_TenantId_CondominiumId",
                table: "OpeningPositions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpeningPositions_TenantId_CondominiumId_OpeningId",
                table: "OpeningPositions",
                columns: new[] { "TenantId", "CondominiumId", "OpeningId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpeningPositions_TenantId_CondominiumId_PropertyUnitId",
                table: "OpeningPositions",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpeningPositions_TenantId_PersonId",
                table: "OpeningPositions",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_JournalEntryId",
                table: "ResidentOpenings",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_SourceClosureId",
                table: "ResidentOpenings",
                column: "SourceClosureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_TenantId_CondominiumId",
                table: "ResidentOpenings",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_TenantId_CondominiumId_AccountingPeriodId",
                table: "ResidentOpenings",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_TenantId_CondominiumId_JournalEntryId",
                table: "ResidentOpenings",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_TenantId_CondominiumId_RequestId",
                table: "ResidentOpenings",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidentOpenings_TenantId_CondominiumId_SourceClosureId",
                table: "ResidentOpenings",
                columns: new[] { "TenantId", "CondominiumId", "SourceClosureId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementPositions_SettlementId_PropertyUnitId_PersonId",
                table: "SettlementPositions",
                columns: new[] { "SettlementId", "PropertyUnitId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementPositions_TenantId_CondominiumId",
                table: "SettlementPositions",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementPositions_TenantId_CondominiumId_PropertyUnitId",
                table: "SettlementPositions",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementPositions_TenantId_CondominiumId_SettlementId",
                table: "SettlementPositions",
                columns: new[] { "TenantId", "CondominiumId", "SettlementId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementPositions_TenantId_PersonId",
                table: "SettlementPositions",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_JournalEntryId",
                table: "StatementSettlements",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_ReversedJournalId",
                table: "StatementSettlements",
                column: "ReversedJournalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_TenantId_CondominiumId",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_TenantId_CondominiumId_JournalEntryId",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_TenantId_CondominiumId_RequestId",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_TenantId_CondominiumId_ReversedJournal~",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId", "ReversedJournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementSettlements_TenantId_CondominiumId_StatementId",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId", "StatementId" });

            migrationBuilder.CreateIndex(
                name: "UX_Settlement_CurrentPosting",
                table: "StatementSettlements",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true,
                filter: "\"PostedAt\" IS NOT NULL AND \"ReversedJournalId\" IS NULL");
            FinalConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Forward-only: preserve final snapshots.");
    }
}