using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CondoManager.Infrastructure;

public sealed partial class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<SecurityGrant> SecurityGrants => Set<SecurityGrant>();
    public DbSet<SecurityAudit> SecurityAudits => Set<SecurityAudit>();

    private Guid? CurrentTenantId => tenantContext.TenantId;
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Condominium> Condominiums => Set<Condominium>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Staircase> Staircases => Set<Staircase>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<OwnershipPeriod> OwnershipPeriods => Set<OwnershipPeriod>();
    public DbSet<AllocationTable> AllocationTables => Set<AllocationTable>();
    public DbSet<AllocationTableEntry> AllocationTableEntries => Set<AllocationTableEntry>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    private void ConfigureTenant<T>(EntityTypeBuilder<T> b) where T : TenantEntity
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.Id });
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
    }
    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        ConfigureIdentity(m);
        ConfigureAccounting(m);
        ConfigureAllocations(m);
        ConfigureBudgets(m);
        ConfigureResidentFinance(m);
        ConfigureFinalStatements(m);
        ConfigureMeetings(m);
        ConfigureVoting(m);
        ConfigureDocuments(m);
        m.Entity<Tenant>(b => {
            b.HasKey(x => x.Id); b.HasIndex(x => x.Code).IsUnique();
            b.HasQueryFilter(x => CurrentTenantId != null && x.Id == CurrentTenantId);
        });
        ConfigureTenant(m.Entity<Condominium>());
        ConfigureTenant(m.Entity<Building>());
        ConfigureTenant(m.Entity<PropertyUnit>());
        ConfigureTenant(m.Entity<Staircase>());
        ConfigureTenant(m.Entity<Person>());
        ConfigureTenant(m.Entity<OwnershipPeriod>());
        ConfigureTenant(m.Entity<AllocationTable>());
        ConfigureTenant(m.Entity<AllocationTableEntry>());
        ConfigureTenant(m.Entity<AccountingPeriod>());
        m.Entity<Condominium>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        m.Entity<Person>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        m.Entity<Building>(b => {
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        });
        m.Entity<PropertyUnit>(b => {
            b.Property(x => x.BuildingId).HasColumnName("BuildingOrStaircaseId");
            b.HasOne<Staircase>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.StaircaseId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.HasOne<Building>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.BuildingId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
        });
        m.Entity<OwnershipPeriod>(b => {
            b.HasIndex(x => new { x.TenantId, x.PropertyUnitId, x.PersonId, x.Role, x.ValidFrom }).IsUnique();
            b.HasOne<PropertyUnit>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyUnitId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.TenantId, x.PersonId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.Property(x => x.Fraction).HasPrecision(18, 10);
            b.ToTable("OwnershipPeriods", t => {
                t.HasCheckConstraint("CK_Ownership_Dates", "\"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                t.HasCheckConstraint("CK_Ownership_Fraction", "(\"Role\" IN (0,1) AND \"Fraction\" IS NOT NULL AND \"Fraction\" > 0 AND \"Fraction\" <= 1) OR (\"Role\" IN (2,3,4) AND \"Fraction\" IS NULL)");
            });
        });
        m.Entity<AllocationTable>(b => {
            b.ToTable("MillesimalTables", t => t.HasCheckConstraint("CK_Table_Denominator", "\"Denominator\" > 0"));
            b.Property(x => x.Denominator).HasPrecision(18, 6);
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        });
        m.Entity<AllocationTableEntry>(b => {
            b.Property(x => x.AllocationTableId).HasColumnName("MillesimalTableId");
            b.HasIndex(x => new { x.TenantId, x.AllocationTableId, x.PropertyUnitId, x.ValidFrom }).IsUnique();
            b.HasOne<AllocationTable>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.AllocationTableId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasOne<PropertyUnit>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.PropertyUnitId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.Property(x => x.Value).HasPrecision(18, 6);
            b.ToTable("MillesimalShares", t => {
                t.HasCheckConstraint("CK_Share_Dates", "\"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                t.HasCheckConstraint("CK_Share_Value", "\"Value\" >= 0");
            });
        });
        m.Entity<AccountingPeriod>(b => {
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.ToTable("AccountingPeriods", t => t.HasCheckConstraint("CK_Accounting_Dates", "\"EndsOn\" >= \"StartsOn\""));
        });
        m.Entity<Staircase>(b => {
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.HasOne<Building>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.BuildingId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
        });
        m.Entity<Condominium>().HasIndex(x => new { x.TenantId, x.TaxCode }).IsUnique();
        m.Entity<Person>().HasIndex(x => new { x.TenantId, x.TaxCode }).IsUnique();
        foreach (var entity in m.Model.GetEntityTypes().Where(e => typeof(Entity).IsAssignableFrom(e.ClrType)))
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string) && p.GetColumnType() != "jsonb")) property.SetMaxLength(160);
            foreach (var property in entity.GetProperties().Where(p => (typeof(MeetingEntity).IsAssignableFrom(entity.ClrType) || entity.ClrType == typeof(CondominiumMeeting)) && (p.Name == "Description" || p.Name == "Contact" || p.Name == "RemoteMeetingUrl" || p.Name == "DocumentReference" || p.Name == "Reference"))) property.SetMaxLength(property.Name == "Description" ? 4000 : 1000);
            foreach (var property in entity.GetProperties().Where(p => p.Name == "ResolutionText")) property.SetMaxLength(12000);
            foreach (var property in entity.GetProperties().Where(p => entity.ClrType == typeof(VotingRule) && p.Name == "ReviewNote" || entity.ClrType == typeof(MeetingResolution) && p.Name == "CorrectionReason" || entity.ClrType == typeof(MeetingAgendaDisposition) && p.Name == "Reason")) property.SetMaxLength(2000);
            foreach (var property in entity.GetProperties().Where(p => entity.ClrType == typeof(MeetingMinutes) && (p.Name == "Introduction" || p.Name == "ClosingNotes"))) property.SetMaxLength(12000);
            foreach (var property in entity.GetProperties().Where(p => entity.ClrType == typeof(MeetingMinutes) && p.Name == "DiscussionNotes")) property.SetMaxLength(60000);
            foreach (var property in entity.GetProperties().Where(p => entity.ClrType == typeof(MeetingMinutes) && p.Name == "CorrectionReason")) property.SetMaxLength(2000);
            foreach (var property in entity.GetProperties().Where(p => entity.ClrType == typeof(CondominiumDocument) && p.Name == "Description")) property.SetMaxLength(4000);
            foreach (var property in entity.GetProperties().Where(p => p.Name == "Notes")) property.SetMaxLength(4000);
            foreach (var property in entity.GetProperties().Where(p => p.Name == "CadastralData")) property.SetMaxLength(2000);
            foreach (var property in entity.GetProperties().Where(p => p.Name == "MailingAddress")) property.SetMaxLength(500);
            foreach (var property in entity.GetProperties().Where(p => p.Name == "Email")) property.SetMaxLength(254);
            // Future monetary properties default to PostgreSQL numeric(18,2).
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) && p.GetPrecision() == null))
            { property.SetPrecision(18); property.SetScale(2); }
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureIdentity(ModelBuilder m)
    {
        m.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        m.Entity<TenantMembership>(b => {
            b.ToTable("Memberships", t => t.HasCheckConstraint("CK_Membership_Role", "\"Role\" IN (0,1,2)"));
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.IsActive });
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<UserSession>(b => {
            b.HasKey(x => x.Id); b.HasIndex(x => x.UserId);
            b.Property(x => x.SecurityStamp).HasMaxLength(256);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.ActiveTenantId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<SecurityGrant>(b => {
            b.HasKey(x => x.Id); b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Purpose, x.NormalizedEmail });
            b.Property(x => x.TokenHash).HasMaxLength(64);
            b.Property(x => x.Email).HasMaxLength(256); b.Property(x => x.NormalizedEmail).HasMaxLength(256);
            b.Property(x => x.SecurityStamp).HasMaxLength(256);
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<SecurityAudit>(b => {
            b.HasKey(x => x.Id); b.HasIndex(x => new { x.TenantId, x.Timestamp });
            b.Property(x => x.Event).HasMaxLength(80);
            // IDs deliberately survive future account deletion; no sensitive free-form payload.
        });
    }

    private void PrepareWrites()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is Entity && (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)))
        {
            if (tenantContext is not ICurrentTenant access) throw new AccessDeniedException();
            access.RequireWrite();
            if (CurrentTenantId is null || CurrentTenantId == Guid.Empty)
                throw new InvalidOperationException("Contesto tenant autenticato obbligatorio.");
            if (entry.Entity is AccountingPeriod && entry.State != EntityState.Added &&
                (AccountingPeriodStatus)entry.Property(nameof(AccountingPeriod.Status)).OriginalValue! == AccountingPeriodStatus.Closed)
                throw new InvalidOperationException("Esercizio chiuso immutabile.");
            if (entry.Entity is JournalEntry or JournalEntryLine or ExpensePayment && entry.State != EntityState.Added)
                throw new InvalidOperationException("Scrittura contabilizzata immutabile: usare uno storno.");
            if (entry.Entity is CondominiumDocument or DocumentLink or MeetingQuorumCheck or MeetingBallotRevision or MeetingResolution or ResolutionApplication or MeetingAgendaDisposition or MeetingCompletion or MeetingDecisionRequest or MeetingAbsence or MeetingSnapshot or MeetingNoticeEvent or MeetingRevision or SettlementPosition or ResidentOpening or OpeningPosition or AdministrativeClosure or PlanSchedule or InstallmentDebtor or Installment or ResidentReceipt or ReceiptAllocation or AllocationRun or AllocationRunSource or AllocationRunLine or ConsumptionReading && entry.State != EntityState.Added) throw new InvalidOperationException("Storico del riparto o lettura immutabile: creare una rettifica.");
            if (entry.Entity is MeetingMinutes && entry.State != EntityState.Added && (MinutesStatus)entry.Property(nameof(MeetingMinutes.Status)).OriginalValue! is MinutesStatus.Finalized or MinutesStatus.Superseded) throw new InvalidOperationException("Verbale definitivo immutabile.");
            if (entry.Entity is Tenant) throw new InvalidOperationException("Provisioning tenant non disponibile.");
            if (entry.Entity is TenantEntity row)
            {
                if (row.TenantId != CurrentTenantId || (entry.State != EntityState.Added &&
                    entry.Property(nameof(TenantEntity.TenantId)).OriginalValue is Guid original && original != CurrentTenantId))
                    throw new InvalidOperationException("Scrittura tra tenant vietata.");
                if (entry.State == EntityState.Modified && entry.Property(nameof(TenantEntity.TenantId)).IsModified)
                    throw new InvalidOperationException("Tenant immutabile.");
            }
            if (entry.Entity is Entity && entry.State is EntityState.Added or EntityState.Modified)
            {
                var now = DateTimeOffset.UtcNow;
                if (entry.State == EntityState.Added) entry.Property(nameof(Entity.CreatedAt)).CurrentValue = now;
                else entry.Property(nameof(Entity.CreatedAt)).IsModified = false;
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
            }
        }
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { PrepareWrites(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { PrepareWrites(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
}
