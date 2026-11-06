using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
 public DbSet<CondominiumDocument> Documents=>Set<CondominiumDocument>();
 public DbSet<DocumentLink> DocumentLinks=>Set<DocumentLink>();
 public DbSet<MeetingMinutes> MinutesRows=>Set<MeetingMinutes>();
 void ConfigureDocuments(ModelBuilder m)
 {
  ConfigureTenant(m.Entity<CondominiumDocument>());ConfigureTenant(m.Entity<DocumentLink>());ConfigureTenant(m.Entity<MeetingMinutes>());
  foreach(var t in new[]{typeof(CondominiumDocument),typeof(DocumentLink),typeof(MeetingMinutes)}){
   var b=m.Entity(t);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");b.HasIndex("TenantId","CondominiumId","CreatedAt");
  }
  m.Entity<CondominiumDocument>(b=>{
   b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RequestId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.StorageKey}).IsUnique();
   b.HasOne<CondominiumDocument>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.PreviousDocumentId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});
   b.ToTable("Documents",t=>t.HasCheckConstraint("CK_Document_Metadata","\"SizeBytes\">0 AND \"Version\">0 AND length(\"Sha256\")=64 AND \"Status\" BETWEEN 0 AND 3 AND \"Type\" BETWEEN 0 AND 9"));
  });
  m.Entity<DocumentLink>(b=>{b.HasIndex(x=>new{x.TenantId,x.DocumentId,x.TargetType,x.TargetId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.TargetType,x.TargetId});b.HasOne<CondominiumDocument>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.DocumentId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});});
  m.Entity<MeetingMinutes>(b=>{
   b.Property(x=>x.Revision).IsConcurrencyToken();b.Property(x=>x.SourceJson).HasColumnType("jsonb");
   b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.Version}).IsUnique();
   b.HasIndex(x=>new{x.TenantId,x.MeetingId}).IsUnique().HasFilter("\"Status\" IN (0,1)");
   b.HasOne<CondominiumMeeting>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.MeetingId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});
   b.HasOne<MeetingMinutes>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.PreviousMinutesId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});
   b.HasOne<CondominiumDocument>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.DocumentId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});
   b.ToTable("MeetingMinutes",t=>t.HasCheckConstraint("CK_Minutes_State","\"Version\">0 AND \"Revision\">0 AND \"Status\" BETWEEN 0 AND 3 AND ((\"Status\" IN (2,3)) = (\"DocumentId\" IS NOT NULL AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedBy\" IS NOT NULL))"));
  });
 }
}
