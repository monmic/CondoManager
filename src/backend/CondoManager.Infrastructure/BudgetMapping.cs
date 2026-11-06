using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
    public DbSet<Budget> Budgets=>Set<Budget>();
    public DbSet<BudgetLine> BudgetLines=>Set<BudgetLine>();
    private void ConfigureBudgets(ModelBuilder m)
    {
        ConfigureTenant(m.Entity<Budget>());ConfigureTenant(m.Entity<BudgetLine>());
        foreach(var type in new[]{typeof(Budget),typeof(BudgetLine)})
        {
            var b=m.Entity(type);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasIndex("TenantId","CondominiumId");
            b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");
        }
        void Link(Type source,Type target,string property)=>m.Entity(source).HasOne(target).WithMany().HasForeignKey("TenantId","CondominiumId",property).HasPrincipalKey("TenantId","CondominiumId","Id");
        Link(typeof(Budget),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(Budget),typeof(Budget),"PreviousVersionId");
        Link(typeof(BudgetLine),typeof(Budget),"BudgetId");Link(typeof(BudgetLine),typeof(Account),"AccountId");Link(typeof(BudgetLine),typeof(AllocationRule),"RuleId");
        m.Entity<Budget>(b=>{
            b.Property(x=>x.Status).IsConcurrencyToken();b.Property(x=>x.AllocationSnapshotJson).HasColumnType("jsonb");b.Property(x=>x.Notes).HasMaxLength(4000);
            b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.Version}).IsUnique();
            b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId}).IsUnique().HasFilter("\"Status\"=2").HasDatabaseName("UX_Budget_Current");
            b.HasIndex(x=>x.PreviousVersionId).IsUnique();
            b.ToTable("Budgets",t=>t.HasCheckConstraint("CK_Budget_State","\"Version\">0 AND \"Status\" BETWEEN 0 AND 3 AND ((\"Status\" IN (0,1) AND \"ApprovedAt\" IS NULL AND \"ApprovedBy\" IS NULL AND \"AllocationSnapshotJson\" IS NULL) OR (\"Status\" IN (2,3) AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedBy\" IS NOT NULL AND \"AllocationSnapshotJson\" IS NOT NULL))"));
        });
        m.Entity<BudgetLine>(b=>{b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.ParametersJson).HasColumnType("jsonb");b.Property(x=>x.Notes).HasMaxLength(4000);b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.BudgetId,x.Position}).IsUnique();b.ToTable("BudgetLines",t=>t.HasCheckConstraint("CK_BudgetLine_Amount","\"Amount\">0 AND \"Position\">=0"));});
    }
}
