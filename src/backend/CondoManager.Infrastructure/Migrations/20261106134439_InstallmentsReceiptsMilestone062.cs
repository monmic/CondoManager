using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InstallmentsReceiptsMilestone062 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Journal_Posted",
                table: "JournalEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Account_Type",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "InstallmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreationTransactionId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "txid_current()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentPlans", x => x.Id);
                    table.UniqueConstraint("AK_InstallmentPlans_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_InstallmentPlans_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Plan_State", "\"Total\">0 AND \"Version\">0 AND \"Status\" BETWEEN 0 AND 2 AND ((\"Status\"=1 AND \"JournalEntryId\" IS NOT NULL AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedBy\" IS NOT NULL) OR (\"Status\"<>1 AND \"JournalEntryId\" IS NULL AND \"ApprovedAt\" IS NULL AND \"ApprovedBy\" IS NULL))");
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_AccountingPeriods_TenantId_CondominiumId_A~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_Budgets_TenantId_CondominiumId_BudgetId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.BudgetId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_InstallmentPlans_TenantId_CondominiumId_Pr~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousPlanId },
                        principalTable: "InstallmentPlans",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_JournalEntries_TenantId_CondominiumId_Jour~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentPlans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResidentReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebtorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidentReceipts", x => x.Id);
                    table.UniqueConstraint("AK_ResidentReceipts_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ResidentReceipts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Receipt_Amount", "\"Amount\">0");
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_AccountingPeriods_TenantId_CondominiumId_A~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_FinancialAccounts_TenantId_CondominiumId_F~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.FinancialAccountId },
                        principalTable: "FinancialAccounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_People_TenantId_DebtorId",
                        columns: x => new { x.TenantId, x.DebtorId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_People_TenantId_PayerId",
                        columns: x => new { x.TenantId, x.PayerId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentReceipts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentDebtors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_InstallmentDebtors", x => x.Id);
                    table.UniqueConstraint("AK_InstallmentDebtors_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_InstallmentDebtors_TenantId_CondominiumId_PlanId_Id", x => new { x.TenantId, x.CondominiumId, x.PlanId, x.Id });
                    table.UniqueConstraint("AK_InstallmentDebtors_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Debtor_Amount", "\"Amount\">=0");
                    table.ForeignKey(
                        name: "FK_InstallmentDebtors_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentDebtors_InstallmentPlans_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PlanId },
                        principalTable: "InstallmentPlans",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentDebtors_People_TenantId_PersonId",
                        columns: x => new { x.TenantId, x.PersonId },
                        principalTable: "People",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentDebtors_PropertyUnits_TenantId_CondominiumId_Pro~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId },
                        principalTable: "PropertyUnits",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstallmentDebtors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSchedules", x => x.Id);
                    table.UniqueConstraint("AK_PlanSchedules_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_PlanSchedules_TenantId_CondominiumId_PlanId_Id", x => new { x.TenantId, x.CondominiumId, x.PlanId, x.Id });
                    table.UniqueConstraint("AK_PlanSchedules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Schedule", "\"Weight\">0 AND \"Position\">=0");
                    table.ForeignKey(
                        name: "FK_PlanSchedules_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanSchedules_InstallmentPlans_TenantId_CondominiumId_PlanId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PlanId },
                        principalTable: "InstallmentPlans",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanSchedules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptBankLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptBankLinks", x => x.Id);
                    table.UniqueConstraint("AK_ReceiptBankLinks_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ReceiptBankLinks_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Link_Status", "\"Status\" IN (0,1)");
                    table.ForeignKey(
                        name: "FK_ReceiptBankLinks_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptBankLinks_JournalEntries_TenantId_CondominiumId_Jour~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptBankLinks_ResidentReceipts_TenantId_CondominiumId_Re~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReceiptId },
                        principalTable: "ResidentReceipts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptBankLinks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentDebtorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.UniqueConstraint("AK_Installments_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Installments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Installment_Amount", "\"Amount\">=0");
                    table.ForeignKey(
                        name: "FK_Installments_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Installments_InstallmentDebtors_TenantId_CondominiumId_Plan~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PlanId, x.InstallmentDebtorId },
                        principalTable: "InstallmentDebtors",
                        principalColumns: new[] { "TenantId", "CondominiumId", "PlanId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Installments_InstallmentPlans_TenantId_CondominiumId_PlanId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PlanId },
                        principalTable: "InstallmentPlans",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Installments_PlanSchedules_TenantId_CondominiumId_PlanId_Sc~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PlanId, x.ScheduleId },
                        principalTable: "PlanSchedules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "PlanId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Installments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversesAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptAllocations", x => x.Id);
                    table.UniqueConstraint("AK_ReceiptAllocations_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ReceiptAllocations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Attribution_Amount", "\"Amount\">0");
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_Installments_TenantId_CondominiumId_Inst~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.InstallmentId },
                        principalTable: "Installments",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_JournalEntries_TenantId_CondominiumId_Jo~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_ReceiptAllocations_TenantId_CondominiumI~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReversesAllocationId },
                        principalTable: "ReceiptAllocations",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_ResidentReceipts_TenantId_CondominiumId_~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReceiptId },
                        principalTable: "ResidentReceipts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Journal_Posted",
                table: "JournalEntries",
                sql: "\"IsPosted\" AND \"Kind\" BETWEEN 0 AND 7 AND ((\"Kind\" = 5) = (\"ReversesEntryId\" IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Account_Type",
                table: "Accounts",
                sql: "\"Type\" BETWEEN 0 AND 4 AND \"Purpose\" BETWEEN 0 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentDebtors_PlanId_PropertyUnitId_PersonId",
                table: "InstallmentDebtors",
                columns: new[] { "PlanId", "PropertyUnitId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentDebtors_TenantId_CondominiumId",
                table: "InstallmentDebtors",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentDebtors_TenantId_CondominiumId_PropertyUnitId",
                table: "InstallmentDebtors",
                columns: new[] { "TenantId", "CondominiumId", "PropertyUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentDebtors_TenantId_PersonId",
                table: "InstallmentDebtors",
                columns: new[] { "TenantId", "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_JournalEntryId",
                table: "InstallmentPlans",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId_AccountingPeriodId_~",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId_BudgetId",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "BudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId_JournalEntryId",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId_PreviousPlanId",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "PreviousPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPlans_TenantId_CondominiumId_RequestId",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Plan_IssuedPeriod",
                table: "InstallmentPlans",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true,
                filter: "\"Status\"=1");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_PlanId_ScheduleId_InstallmentDebtorId",
                table: "Installments",
                columns: new[] { "PlanId", "ScheduleId", "InstallmentDebtorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installments_TenantId_CondominiumId",
                table: "Installments",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Installments_TenantId_CondominiumId_PlanId_InstallmentDebto~",
                table: "Installments",
                columns: new[] { "TenantId", "CondominiumId", "PlanId", "InstallmentDebtorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Installments_TenantId_CondominiumId_PlanId_ScheduleId",
                table: "Installments",
                columns: new[] { "TenantId", "CondominiumId", "PlanId", "ScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanSchedules_PlanId_DueDate",
                table: "PlanSchedules",
                columns: new[] { "PlanId", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanSchedules_PlanId_Position",
                table: "PlanSchedules",
                columns: new[] { "PlanId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanSchedules_TenantId_CondominiumId",
                table: "PlanSchedules",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_ReceiptId_JournalEntryId_InstallmentId",
                table: "ReceiptAllocations",
                columns: new[] { "ReceiptId", "JournalEntryId", "InstallmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_ReversesAllocationId",
                table: "ReceiptAllocations",
                column: "ReversesAllocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_TenantId_CondominiumId",
                table: "ReceiptAllocations",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_TenantId_CondominiumId_InstallmentId",
                table: "ReceiptAllocations",
                columns: new[] { "TenantId", "CondominiumId", "InstallmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_TenantId_CondominiumId_JournalEntryId",
                table: "ReceiptAllocations",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_TenantId_CondominiumId_ReceiptId",
                table: "ReceiptAllocations",
                columns: new[] { "TenantId", "CondominiumId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_TenantId_CondominiumId_ReversesAllocatio~",
                table: "ReceiptAllocations",
                columns: new[] { "TenantId", "CondominiumId", "ReversesAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptBankLinks_JournalEntryId",
                table: "ReceiptBankLinks",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptBankLinks_ReceiptId",
                table: "ReceiptBankLinks",
                column: "ReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptBankLinks_TenantId_CondominiumId",
                table: "ReceiptBankLinks",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptBankLinks_TenantId_CondominiumId_JournalEntryId",
                table: "ReceiptBankLinks",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptBankLinks_TenantId_CondominiumId_ReceiptId",
                table: "ReceiptBankLinks",
                columns: new[] { "TenantId", "CondominiumId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_CondominiumId",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_CondominiumId_AccountingPeriodId_~",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_CondominiumId_FinancialAccountId",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "CondominiumId", "FinancialAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_CondominiumId_RequestId",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_DebtorId",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "DebtorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentReceipts_TenantId_PayerId",
                table: "ResidentReceipts",
                columns: new[] { "TenantId", "PayerId" });
            FinanceConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Forward-only: preserve finance history.");
    }
}