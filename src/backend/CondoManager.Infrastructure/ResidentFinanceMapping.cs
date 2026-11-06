using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
    public DbSet<InstallmentPlan> InstallmentPlans=>Set<InstallmentPlan>();
    public DbSet<PlanSchedule> PlanSchedules=>Set<PlanSchedule>();
    public DbSet<InstallmentDebtor> InstallmentDebtors=>Set<InstallmentDebtor>();
    public DbSet<Installment> Installments=>Set<Installment>();
    public DbSet<ResidentReceipt> ResidentReceiptRows=>Set<ResidentReceipt>();
    public DbSet<ReceiptBankLink> ReceiptBankLinks=>Set<ReceiptBankLink>();
    public DbSet<ReceiptAllocation> ReceiptAllocations=>Set<ReceiptAllocation>();
    private void ConfigureResidentFinance(ModelBuilder m)
    {
        ConfigureTenant(m.Entity<InstallmentPlan>());ConfigureTenant(m.Entity<PlanSchedule>());ConfigureTenant(m.Entity<InstallmentDebtor>());ConfigureTenant(m.Entity<Installment>());ConfigureTenant(m.Entity<ResidentReceipt>());ConfigureTenant(m.Entity<ReceiptBankLink>());ConfigureTenant(m.Entity<ReceiptAllocation>());
        foreach(var type in new[]{typeof(InstallmentPlan),typeof(PlanSchedule),typeof(InstallmentDebtor),typeof(Installment),typeof(ResidentReceipt),typeof(ReceiptBankLink),typeof(ReceiptAllocation)})
        {
            var b=m.Entity(type);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasIndex("TenantId","CondominiumId");b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");
        }
        void Link(Type a,Type b,string field)=>m.Entity(a).HasOne(b).WithMany().HasForeignKey("TenantId","CondominiumId",field).HasPrincipalKey("TenantId","CondominiumId","Id");
        Link(typeof(InstallmentPlan),typeof(Budget),"BudgetId");Link(typeof(InstallmentPlan),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(InstallmentPlan),typeof(InstallmentPlan),"PreviousPlanId");Link(typeof(InstallmentPlan),typeof(JournalEntry),"JournalEntryId");
        m.Entity<InstallmentPlan>(b=>{b.Property<long>("CreationTransactionId").HasDefaultValueSql("txid_current()");b.Property(x=>x.InputJson).HasColumnType("jsonb");b.Property(x=>x.SnapshotJson).HasColumnType("jsonb");b.Property(x=>x.Status).IsConcurrencyToken();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.Version}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId}).IsUnique().HasFilter("\"Status\"=1").HasDatabaseName("UX_Plan_IssuedPeriod");b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RequestId}).IsUnique();b.HasIndex(x=>x.JournalEntryId).IsUnique();b.ToTable("InstallmentPlans",t=>t.HasCheckConstraint("CK_Plan_State","\"Total\">0 AND \"Version\">0 AND \"Status\" BETWEEN 0 AND 2 AND ((\"Status\"=1 AND \"JournalEntryId\" IS NOT NULL AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedBy\" IS NOT NULL) OR (\"Status\"<>1 AND \"JournalEntryId\" IS NULL AND \"ApprovedAt\" IS NULL AND \"ApprovedBy\" IS NULL))"));});
        Link(typeof(PlanSchedule),typeof(InstallmentPlan),"PlanId");Link(typeof(InstallmentDebtor),typeof(InstallmentPlan),"PlanId");Link(typeof(InstallmentDebtor),typeof(PropertyUnit),"PropertyUnitId");
        m.Entity<InstallmentDebtor>().HasOne<Person>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id});
        m.Entity<PlanSchedule>(b=>{b.HasAlternateKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.Id});b.Property(x=>x.Weight).HasPrecision(18,6);b.HasIndex(x=>new{x.PlanId,x.Position}).IsUnique();b.HasIndex(x=>new{x.PlanId,x.DueDate}).IsUnique();b.ToTable("PlanSchedules",t=>t.HasCheckConstraint("CK_Schedule","\"Weight\">0 AND \"Position\">=0"));});
        m.Entity<InstallmentDebtor>(b=>{b.HasAlternateKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.Id});b.HasIndex(x=>new{x.PlanId,x.PropertyUnitId,x.PersonId}).IsUnique();b.ToTable("InstallmentDebtors",t=>t.HasCheckConstraint("CK_Debtor_Amount","\"Amount\">=0"));});
        Link(typeof(Installment),typeof(InstallmentPlan),"PlanId");
        m.Entity<Installment>(b=>{b.HasOne<PlanSchedule>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.ScheduleId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.Id});b.HasOne<InstallmentDebtor>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.InstallmentDebtorId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.PlanId,x.Id});b.HasIndex(x=>new{x.PlanId,x.ScheduleId,x.InstallmentDebtorId}).IsUnique();b.ToTable("Installments",t=>t.HasCheckConstraint("CK_Installment_Amount","\"Amount\">=0"));});
        Link(typeof(ResidentReceipt),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(ResidentReceipt),typeof(FinancialAccount),"FinancialAccountId");
        m.Entity<ResidentReceipt>(b=>{b.HasOne<Person>().WithMany().HasForeignKey(x=>new{x.TenantId,x.DebtorId}).HasPrincipalKey(x=>new{x.TenantId,x.Id});b.HasOne<Person>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PayerId}).HasPrincipalKey(x=>new{x.TenantId,x.Id});b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RequestId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.PaymentDate});b.ToTable("ResidentReceipts",t=>t.HasCheckConstraint("CK_Receipt_Amount","\"Amount\">0"));});
        Link(typeof(ReceiptBankLink),typeof(ResidentReceipt),"ReceiptId");Link(typeof(ReceiptBankLink),typeof(JournalEntry),"JournalEntryId");
        m.Entity<ReceiptBankLink>(b=>{b.HasIndex(x=>x.ReceiptId).IsUnique();b.HasIndex(x=>x.JournalEntryId).IsUnique();b.Property(x=>x.Status).IsConcurrencyToken();b.ToTable("ReceiptBankLinks",t=>t.HasCheckConstraint("CK_Link_Status","\"Status\" IN (0,1)"));});
        Link(typeof(ReceiptAllocation),typeof(ResidentReceipt),"ReceiptId");Link(typeof(ReceiptAllocation),typeof(Installment),"InstallmentId");Link(typeof(ReceiptAllocation),typeof(JournalEntry),"JournalEntryId");Link(typeof(ReceiptAllocation),typeof(ReceiptAllocation),"ReversesAllocationId");
        m.Entity<ReceiptAllocation>(b=>{b.HasIndex(x=>new{x.ReceiptId,x.JournalEntryId,x.InstallmentId}).IsUnique();b.HasIndex(x=>x.ReversesAllocationId).IsUnique();b.ToTable("ReceiptAllocations",t=>t.HasCheckConstraint("CK_Attribution_Amount","\"Amount\">0"));});
    }
}
