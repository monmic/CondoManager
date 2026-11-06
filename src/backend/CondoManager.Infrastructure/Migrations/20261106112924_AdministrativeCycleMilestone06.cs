using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdministrativeCycleMilestone06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    AllocationSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.UniqueConstraint("AK_Budgets_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Budgets_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Budget_State", "\"Version\">0 AND \"Status\" BETWEEN 0 AND 3 AND ((\"Status\" IN (0,1) AND \"ApprovedAt\" IS NULL AND \"ApprovedBy\" IS NULL AND \"AllocationSnapshotJson\" IS NULL) OR (\"Status\" IN (2,3) AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedBy\" IS NOT NULL AND \"AllocationSnapshotJson\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Budgets_AccountingPeriods_TenantId_CondominiumId_Accounting~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Budgets_TenantId_CondominiumId_PreviousVersionId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.PreviousVersionId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.UniqueConstraint("AK_BudgetLines_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_BudgetLines_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_BudgetLine_Amount", "\"Amount\">0 AND \"Position\">=0");
                    table.ForeignKey(
                        name: "FK_BudgetLines_Accounts_TenantId_CondominiumId_AccountId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_AllocationRules_TenantId_CondominiumId_RuleId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.RuleId },
                        principalTable: "AllocationRules",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_TenantId_CondominiumId_BudgetId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.BudgetId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CondominiumId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CondominiumId_AccountId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CondominiumId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CondominiumId_BudgetId_Position",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CondominiumId", "BudgetId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CondominiumId_RuleId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CondominiumId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_PreviousVersionId",
                table: "Budgets",
                column: "PreviousVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_CondominiumId",
                table: "Budgets",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_CondominiumId_AccountingPeriodId_Version",
                table: "Budgets",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_CondominiumId_PreviousVersionId",
                table: "Budgets",
                columns: new[] { "TenantId", "CondominiumId", "PreviousVersionId" });

            migrationBuilder.CreateIndex(
                name: "UX_Budget_Current",
                table: "Budgets",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId" },
                unique: true,
                filter: "\"Status\"=2");
            AddBudgetGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Forward-only: preserve administrative history.");
    }
}
