using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
    public DbSet<AllocationRule> AllocationRules=>Set<AllocationRule>();
    public DbSet<AllocationRuleComponent> AllocationRuleComponents=>Set<AllocationRuleComponent>();
    public DbSet<AllocationGroup> AllocationGroups=>Set<AllocationGroup>();
    public DbSet<AllocationGroupMember> AllocationGroupMembers=>Set<AllocationGroupMember>();
    public DbSet<ConsumptionReading> ConsumptionReadings=>Set<ConsumptionReading>();
    public DbSet<AllocationAssignment> AllocationAssignments=>Set<AllocationAssignment>();
    public DbSet<AllocationRun> AllocationRuns=>Set<AllocationRun>();
    public DbSet<AllocationRunSource> AllocationRunSources=>Set<AllocationRunSource>();
    public DbSet<AllocationRunLine> AllocationRunLines=>Set<AllocationRunLine>();
    private void ConfigureAllocations(ModelBuilder m)
    {
        ConfigureTenant(m.Entity<AllocationRule>());ConfigureTenant(m.Entity<AllocationRuleComponent>());ConfigureTenant(m.Entity<AllocationGroup>());ConfigureTenant(m.Entity<AllocationGroupMember>());ConfigureTenant(m.Entity<ConsumptionReading>());ConfigureTenant(m.Entity<AllocationAssignment>());ConfigureTenant(m.Entity<AllocationRun>());ConfigureTenant(m.Entity<AllocationRunSource>());ConfigureTenant(m.Entity<AllocationRunLine>());
        foreach(var type in new[]{typeof(AllocationRule),typeof(AllocationRuleComponent),typeof(AllocationGroup),typeof(AllocationGroupMember),typeof(ConsumptionReading),typeof(AllocationAssignment),typeof(AllocationRun),typeof(AllocationRunSource),typeof(AllocationRunLine)})
        {
            var b=m.Entity(type);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasIndex("TenantId","CondominiumId");b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");
        }
        void Link(Type source,Type target,string property)=>m.Entity(source).HasOne(target).WithMany().HasForeignKey("TenantId","CondominiumId",property).HasPrincipalKey("TenantId","CondominiumId","Id");
        m.Entity<AllocationRule>(b=>{ b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.Name,x.Version}).IsUnique();b.HasIndex(x=>x.PreviousVersionId).IsUnique();b.ToTable("AllocationRules",t=>t.HasCheckConstraint("CK_Rule_Status","\"Status\" IN (0,1) AND \"Type\" BETWEEN 0 AND 4 AND \"Version\">0")); });
        Link(typeof(AllocationRule),typeof(AllocationRule),"PreviousVersionId");
        Link(typeof(AllocationRuleComponent),typeof(AllocationRule),"RuleId");Link(typeof(AllocationRuleComponent),typeof(AllocationGroup),"GroupId");Link(typeof(AllocationRuleComponent),typeof(AllocationTable),"TableId");
        m.Entity<AllocationRuleComponent>(b=>{ b.Property(x=>x.Percentage).HasPrecision(18,6);b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RuleId,x.Position}).IsUnique();b.ToTable("AllocationRuleComponents",t=>t.HasCheckConstraint("CK_Component_Percentage","\"Percentage\">0 AND \"Percentage\"<=100 AND \"Position\">=0 AND \"Criterion\" IN (0,1,2,4)")); });
        m.Entity<AllocationGroup>().HasIndex(x=>new{x.TenantId,x.CondominiumId,x.Name}).IsUnique();
        Link(typeof(AllocationGroupMember),typeof(AllocationGroup),"GroupId");Link(typeof(AllocationGroupMember),typeof(PropertyUnit),"PropertyUnitId");
        m.Entity<AllocationGroupMember>().HasIndex(x=>new{x.TenantId,x.CondominiumId,x.GroupId,x.PropertyUnitId}).IsUnique();
        Link(typeof(ConsumptionReading),typeof(PropertyUnit),"PropertyUnitId");Link(typeof(ConsumptionReading),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(ConsumptionReading),typeof(ConsumptionReading),"ReplacesReadingId");
        m.Entity<ConsumptionReading>(b=>{ b.Property(x=>x.Value).HasPrecision(18,6);b.HasIndex(x=>x.ReplacesReadingId).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.PropertyUnitId,x.ConsumptionType,x.Date});b.ToTable("ConsumptionReadings",t=>t.HasCheckConstraint("CK_Reading_Value","\"Value\">=0 AND \"Boundary\" IN (0,1) AND ((NOT \"IsEstimated\" AND \"ReplacesReadingId\" IS NULL) OR \"Reason\" IS NOT NULL)")); });
        Link(typeof(AllocationAssignment),typeof(Account),"AccountId");Link(typeof(AllocationAssignment),typeof(ExpenseDocument),"ExpenseDocumentId");Link(typeof(AllocationAssignment),typeof(AllocationRule),"RuleId");
        m.Entity<AllocationAssignment>(b=>{b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.ExpenseDocumentId}).IsUnique();b.ToTable("AllocationAssignments",t=>t.HasCheckConstraint("CK_Assignment_Target","(\"AccountId\" IS NULL) <> (\"ExpenseDocumentId\" IS NULL)"));});
        Link(typeof(AllocationRun),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(AllocationRun),typeof(AllocationRun),"PreviousRunId");
        m.Entity<AllocationRun>(b=>{b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RequestId}).IsUnique();b.HasIndex(x=>x.PreviousRunId).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.CreatedAt});b.Property(x=>x.InputJson).HasColumnType("jsonb");b.Property(x=>x.SnapshotJson).HasColumnType("jsonb");b.Property<long>("CreationTransactionId").HasDefaultValueSql("txid_current()");b.ToTable("AllocationRuns",t=>t.HasCheckConstraint("CK_Run_Total","\"TotalAmount\">0 AND \"Version\">0 AND \"CompetenceTo\">=\"CompetenceFrom\""));});
        Link(typeof(AllocationRunSource),typeof(AllocationRun),"RunId");Link(typeof(AllocationRunSource),typeof(ExpenseDocument),"ExpenseDocumentId");Link(typeof(AllocationRunSource),typeof(AllocationRule),"RuleId");
        m.Entity<AllocationRunSource>(b=>{b.HasAlternateKey(x=>new{x.TenantId,x.CondominiumId,x.RunId,x.Id});b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RunId,x.Position}).IsUnique();b.ToTable("AllocationRunSources",t=>t.HasCheckConstraint("CK_RunSource_Amount","\"Amount\">0 AND \"Position\">=0"));});
        Link(typeof(AllocationRunLine),typeof(AllocationRun),"RunId");Link(typeof(AllocationRunLine),typeof(PropertyUnit),"PropertyUnitId");
        m.Entity<AllocationRunLine>(b=>{b.HasOne<AllocationRunSource>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.RunId,x.SourceId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.RunId,x.Id});b.HasOne<Person>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id});b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RunId,x.Position}).IsUnique();b.Property(x=>x.UnroundedAmount).HasPrecision(28,12);b.Property(x=>x.RoundingAdjustment).HasPrecision(28,12);b.ToTable("AllocationRunLines",t=>t.HasCheckConstraint("CK_RunLine_Amount","\"Amount\">=0 AND \"BaseAmount\">=0 AND \"Position\">=0 AND \"Amount\"-\"UnroundedAmount\"=\"RoundingAdjustment\""));});
    }
}
