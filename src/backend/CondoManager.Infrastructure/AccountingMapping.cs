using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ExpenseDocument> ExpenseDocuments => Set<ExpenseDocument>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<ExpensePayment> ExpensePayments => Set<ExpensePayment>();
    private void ConfigureAccounting(ModelBuilder m)
    {
        ConfigureTenant(m.Entity<Account>()); ConfigureTenant(m.Entity<Supplier>()); ConfigureTenant(m.Entity<ExpenseDocument>());
        ConfigureTenant(m.Entity<FinancialAccount>()); ConfigureTenant(m.Entity<JournalEntry>()); ConfigureTenant(m.Entity<JournalEntryLine>()); ConfigureTenant(m.Entity<ExpensePayment>());
        m.Entity<AccountingPeriod>().HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
        foreach (var type in new[] { typeof(Account),typeof(ExpenseDocument),typeof(FinancialAccount),typeof(JournalEntry),typeof(JournalEntryLine),typeof(ExpensePayment) })
        {
            var b = m.Entity(type);
            b.HasAlternateKey("TenantId","CondominiumId","Id");
            b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");
            b.HasIndex("TenantId","CondominiumId");
        }
        m.Entity<Account>(b => {
            b.HasIndex(x => new { x.TenantId,x.CondominiumId,x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId,x.CondominiumId,x.Purpose }).IsUnique().HasFilter("\"Purpose\" <> 0");
            b.HasOne<Account>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.ParentAccountId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.ToTable("Accounts", t => t.HasCheckConstraint("CK_Account_Type", "\"Type\" BETWEEN 0 AND 4 AND \"Purpose\" BETWEEN 0 AND 3"));
        });
        m.Entity<Supplier>().HasIndex(x => new {x.TenantId,x.Name});
        m.Entity<ExpenseDocument>(b => {
            b.HasOne<AccountingPeriod>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.AccountingPeriodId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<Account>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.AccountId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<Supplier>().WithMany().HasForeignKey(x => new {x.TenantId,x.SupplierId}).HasPrincipalKey(x => new {x.TenantId,x.Id});
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.SupplierId,x.DocumentNumber,x.DocumentDate}).IsUnique();
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.DocumentDate});
            b.ToTable("ExpenseDocuments", t => t.HasCheckConstraint("CK_Expense_Amounts", "\"NetAmount\" >= 0 AND \"VatAmount\" >= 0 AND \"GrossAmount\" > 0 AND \"GrossAmount\" = \"NetAmount\" + \"VatAmount\" AND (\"DueDate\" IS NULL OR \"DueDate\" >= \"DocumentDate\")"));
        });
        m.Entity<FinancialAccount>(b => {
            b.HasOne<Account>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.AccountId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasIndex(x => new {x.TenantId,x.AccountId}).IsUnique();
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.CreationRequestId}).IsUnique();
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.Name}).IsUnique();
            b.ToTable("FinancialAccounts", t => t.HasCheckConstraint("CK_Financial_Type", "\"Type\" IN (0,1) AND (\"Type\" <> 1 OR \"Iban\" IS NULL)"));
        });
        m.Entity<JournalEntry>(b => {
            b.Property<long>("CreationTransactionId").HasDefaultValueSql("txid_current()");
            b.HasOne<AccountingPeriod>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.AccountingPeriodId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<ExpenseDocument>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.ExpenseDocumentId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.ReversesEntryId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.RequestId}).IsUnique();
            b.HasIndex(x => x.ReversesEntryId).IsUnique();
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.Date});
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.JournalEntryId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.Navigation(x => x.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
            b.ToTable("JournalEntries", t => t.HasCheckConstraint("CK_Journal_Posted", "\"IsPosted\" AND \"Kind\" BETWEEN 0 AND 5 AND ((\"Kind\" = 5) = (\"ReversesEntryId\" IS NOT NULL))"));
        });
        m.Entity<JournalEntryLine>(b => {
            b.HasOne<Account>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.AccountId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasIndex(x => new {x.TenantId,x.CondominiumId,x.AccountId});
            b.ToTable("JournalEntryLines", t => t.HasCheckConstraint("CK_JournalLine_Amount", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)"));
        });
        m.Entity<ExpensePayment>(b => {
            b.HasOne<ExpenseDocument>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.ExpenseDocumentId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<FinancialAccount>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.FinancialAccountId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasOne<JournalEntry>().WithMany().HasForeignKey(x => new {x.TenantId,x.CondominiumId,x.JournalEntryId}).HasPrincipalKey(x => new {x.TenantId,x.CondominiumId,x.Id});
            b.HasIndex(x => x.JournalEntryId).IsUnique();
            b.ToTable("ExpensePayments", t => t.HasCheckConstraint("CK_Payment_Positive", "\"Amount\" > 0"));
        });
    }
}
