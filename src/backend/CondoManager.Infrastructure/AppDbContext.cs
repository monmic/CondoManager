using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CondoManager.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    private Guid? CurrentTenantId => tenantContext.TenantId;
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Condominium> Condominiums => Set<Condominium>();
    public DbSet<BuildingOrStaircase> Buildings => Set<BuildingOrStaircase>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<OwnershipPeriod> OwnershipPeriods => Set<OwnershipPeriod>();
    public DbSet<MillesimalTable> MillesimalTables => Set<MillesimalTable>();
    public DbSet<MillesimalShare> MillesimalShares => Set<MillesimalShare>();
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
        m.Entity<Tenant>(b => {
            b.HasKey(x => x.Id); b.HasIndex(x => x.Code).IsUnique();
            b.HasQueryFilter(x => CurrentTenantId != null && x.Id == CurrentTenantId);
        });
        ConfigureTenant(m.Entity<Condominium>());
        ConfigureTenant(m.Entity<BuildingOrStaircase>());
        ConfigureTenant(m.Entity<PropertyUnit>());
        ConfigureTenant(m.Entity<Person>());
        ConfigureTenant(m.Entity<OwnershipPeriod>());
        ConfigureTenant(m.Entity<MillesimalTable>());
        ConfigureTenant(m.Entity<MillesimalShare>());
        ConfigureTenant(m.Entity<AccountingPeriod>());
        m.Entity<Condominium>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        m.Entity<Person>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        m.Entity<BuildingOrStaircase>(b => {
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        });
        m.Entity<PropertyUnit>(b => {
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.HasOne<BuildingOrStaircase>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.BuildingOrStaircaseId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
        });
        m.Entity<OwnershipPeriod>(b => {
            b.HasIndex(x => new { x.TenantId, x.PropertyUnitId, x.PersonId, x.ValidFrom }).IsUnique();
            b.HasOne<PropertyUnit>().WithMany().HasForeignKey(x => new { x.TenantId, x.PropertyUnitId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.TenantId, x.PersonId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
            b.Property(x => x.Fraction).HasPrecision(18, 10);
            b.ToTable("OwnershipPeriods", t => {
                t.HasCheckConstraint("CK_Ownership_Dates", "\"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                t.HasCheckConstraint("CK_Ownership_Fraction", "\"Fraction\" > 0 AND \"Fraction\" <= 1");
            });
        });
        m.Entity<MillesimalTable>(b => {
            b.HasAlternateKey(x => new { x.TenantId, x.CondominiumId, x.Id });
            b.HasIndex(x => new { x.TenantId, x.CondominiumId, x.Code }).IsUnique();
            b.HasOne<Condominium>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        });
        m.Entity<MillesimalShare>(b => {
            b.HasIndex(x => new { x.TenantId, x.MillesimalTableId, x.PropertyUnitId, x.ValidFrom }).IsUnique();
            b.HasOne<MillesimalTable>().WithMany().HasForeignKey(x => new { x.TenantId, x.CondominiumId, x.MillesimalTableId }).HasPrincipalKey(x => new { x.TenantId, x.CondominiumId, x.Id });
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
        foreach (var entity in m.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string))) property.SetMaxLength(160);
            // Future monetary properties default to PostgreSQL numeric(18,2).
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) && p.GetPrecision() == null))
            { property.SetPrecision(18); property.SetScale(2); }
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private void PrepareWrites()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (CurrentTenantId is null || CurrentTenantId == Guid.Empty)
                throw new InvalidOperationException("Contesto tenant autenticato obbligatorio.");
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