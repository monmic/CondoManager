using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountingCoreMilestone04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_AccountingPeriods_TenantId_CondominiumId_Id",
                table: "AccountingPeriods",
                columns: new[] { "TenantId", "CondominiumId", "Id" });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.UniqueConstraint("AK_Accounts_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_Accounts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Account_Type", "\"Type\" BETWEEN 0 AND 4 AND \"Purpose\" BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_TenantId_CondominiumId_ParentAccountId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ParentAccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Address = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Iban = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.UniqueConstraint("AK_Suppliers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Suppliers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Iban = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationRequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.UniqueConstraint("AK_FinancialAccounts_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_FinancialAccounts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Financial_Type", "\"Type\" IN (0,1) AND (\"Type\" <> 1 OR \"Iban\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Accounts_TenantId_CondominiumId_AccountId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsRegistered = table.Column<bool>(type: "boolean", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDocuments", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseDocuments_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ExpenseDocuments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Expense_Amounts", "\"NetAmount\" >= 0 AND \"VatAmount\" >= 0 AND \"GrossAmount\" > 0 AND \"GrossAmount\" = \"NetAmount\" + \"VatAmount\" AND (\"DueDate\" IS NULL OR \"DueDate\" >= \"DocumentDate\")");
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_AccountingPeriods_TenantId_CondominiumId_A~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_Accounts_TenantId_CondominiumId_AccountId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_Suppliers_TenantId_SupplierId",
                        columns: x => new { x.TenantId, x.SupplierId },
                        principalTable: "Suppliers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTransactionId = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "txid_current()"),
                    RequestHash = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ReversesEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpenseDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPosted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.UniqueConstraint("AK_JournalEntries_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_JournalEntries_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Journal_Posted", "\"IsPosted\" AND \"Kind\" BETWEEN 0 AND 5 AND ((\"Kind\" = 5) = (\"ReversesEntryId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_JournalEntries_AccountingPeriods_TenantId_CondominiumId_Acc~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_ExpenseDocuments_TenantId_CondominiumId_Expe~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ExpenseDocumentId },
                        principalTable: "ExpenseDocuments",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_JournalEntries_TenantId_CondominiumId_Revers~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ReversesEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpensePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePayments", x => x.Id);
                    table.UniqueConstraint("AK_ExpensePayments_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_ExpensePayments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Payment_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_ExpenseDocuments_TenantId_CondominiumId_Exp~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.ExpenseDocumentId },
                        principalTable: "ExpenseDocuments",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_FinancialAccounts_TenantId_CondominiumId_Fi~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.FinancialAccountId },
                        principalTable: "FinancialAccounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_JournalEntries_TenantId_CondominiumId_Journ~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CondominiumId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.UniqueConstraint("AK_JournalEntryLines_TenantId_CondominiumId_Id", x => new { x.TenantId, x.CondominiumId, x.Id });
                    table.UniqueConstraint("AK_JournalEntryLines_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_JournalLine_Amount", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)");
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Accounts_TenantId_CondominiumId_AccountId",
                        columns: x => new { x.TenantId, x.CondominiumId, x.AccountId },
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Condominiums_TenantId_CondominiumId",
                        columns: x => new { x.TenantId, x.CondominiumId },
                        principalTable: "Condominiums",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_TenantId_CondominiumId_Jou~",
                        columns: x => new { x.TenantId, x.CondominiumId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CondominiumId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId_CondominiumId",
                table: "Accounts",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId_CondominiumId_Code",
                table: "Accounts",
                columns: new[] { "TenantId", "CondominiumId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId_CondominiumId_ParentAccountId",
                table: "Accounts",
                columns: new[] { "TenantId", "CondominiumId", "ParentAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId_CondominiumId_Purpose",
                table: "Accounts",
                columns: new[] { "TenantId", "CondominiumId", "Purpose" },
                unique: true,
                filter: "\"Purpose\" <> 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_TenantId_CondominiumId",
                table: "ExpenseDocuments",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_TenantId_CondominiumId_AccountId",
                table: "ExpenseDocuments",
                columns: new[] { "TenantId", "CondominiumId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_TenantId_CondominiumId_AccountingPeriodId_~",
                table: "ExpenseDocuments",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_TenantId_CondominiumId_SupplierId_Document~",
                table: "ExpenseDocuments",
                columns: new[] { "TenantId", "CondominiumId", "SupplierId", "DocumentNumber", "DocumentDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_TenantId_SupplierId",
                table: "ExpenseDocuments",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_JournalEntryId",
                table: "ExpensePayments",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_TenantId_CondominiumId",
                table: "ExpensePayments",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_TenantId_CondominiumId_ExpenseDocumentId",
                table: "ExpensePayments",
                columns: new[] { "TenantId", "CondominiumId", "ExpenseDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_TenantId_CondominiumId_FinancialAccountId",
                table: "ExpensePayments",
                columns: new[] { "TenantId", "CondominiumId", "FinancialAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_TenantId_CondominiumId_JournalEntryId",
                table: "ExpensePayments",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_TenantId_AccountId",
                table: "FinancialAccounts",
                columns: new[] { "TenantId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_TenantId_CondominiumId",
                table: "FinancialAccounts",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_TenantId_CondominiumId_AccountId",
                table: "FinancialAccounts",
                columns: new[] { "TenantId", "CondominiumId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_TenantId_CondominiumId_CreationRequestId",
                table: "FinancialAccounts",
                columns: new[] { "TenantId", "CondominiumId", "CreationRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_TenantId_CondominiumId_Name",
                table: "FinancialAccounts",
                columns: new[] { "TenantId", "CondominiumId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReversesEntryId",
                table: "JournalEntries",
                column: "ReversesEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CondominiumId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CondominiumId_AccountingPeriodId_Da~",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CondominiumId", "AccountingPeriodId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CondominiumId_ExpenseDocumentId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CondominiumId", "ExpenseDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CondominiumId_RequestId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CondominiumId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CondominiumId_ReversesEntryId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CondominiumId", "ReversesEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId_CondominiumId",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "CondominiumId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId_CondominiumId_AccountId",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "CondominiumId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId_CondominiumId_JournalEntryId",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "CondominiumId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_Name",
                table: "Suppliers",
                columns: new[] { "TenantId", "Name" });
            AccountingConstraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Accounting history must not be deleted by a downgrade.");
    }
}