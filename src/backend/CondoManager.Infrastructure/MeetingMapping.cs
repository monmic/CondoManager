using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
 public DbSet<CondominiumMeeting> Meetings=>Set<CondominiumMeeting>();
 public DbSet<MeetingSession> MeetingSessions=>Set<MeetingSession>();
 public DbSet<MeetingAgendaItem> MeetingAgendaItems=>Set<MeetingAgendaItem>();
 public DbSet<MeetingSnapshot> MeetingSnapshots=>Set<MeetingSnapshot>();
 public DbSet<MeetingNotice> MeetingNotices=>Set<MeetingNotice>();
 public DbSet<MeetingNoticeEvent> MeetingNoticeEvents=>Set<MeetingNoticeEvent>();
 public DbSet<MeetingProxy> MeetingProxies=>Set<MeetingProxy>();
 public DbSet<MeetingProxyUnit> MeetingProxyUnits=>Set<MeetingProxyUnit>();
 public DbSet<MeetingAttendance> MeetingAttendances=>Set<MeetingAttendance>();
 public DbSet<MeetingAttendanceUnit> MeetingAttendanceUnits=>Set<MeetingAttendanceUnit>();
 public DbSet<MeetingAbsence> MeetingAbsences=>Set<MeetingAbsence>();
 public DbSet<MeetingRevision> MeetingRevisions=>Set<MeetingRevision>();
 private void ConfigureMeetings(ModelBuilder m)
 {
  ConfigureTenant(m.Entity<CondominiumMeeting>());ConfigureTenant(m.Entity<MeetingSession>());ConfigureTenant(m.Entity<MeetingAgendaItem>());ConfigureTenant(m.Entity<MeetingSnapshot>());ConfigureTenant(m.Entity<MeetingNotice>());ConfigureTenant(m.Entity<MeetingNoticeEvent>());ConfigureTenant(m.Entity<MeetingProxy>());ConfigureTenant(m.Entity<MeetingProxyUnit>());ConfigureTenant(m.Entity<MeetingAttendance>());ConfigureTenant(m.Entity<MeetingAttendanceUnit>());ConfigureTenant(m.Entity<MeetingRevision>());
  ConfigureTenant(m.Entity<MeetingAbsence>());
  var types=new[]{typeof(MeetingAbsence),typeof(CondominiumMeeting),typeof(MeetingSession),typeof(MeetingAgendaItem),typeof(MeetingSnapshot),typeof(MeetingNotice),typeof(MeetingNoticeEvent),typeof(MeetingProxy),typeof(MeetingProxyUnit),typeof(MeetingAttendance),typeof(MeetingAttendanceUnit),typeof(MeetingRevision)};
  foreach(var type in types){var b=m.Entity(type);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");b.HasIndex("TenantId","CondominiumId");if(type!=typeof(CondominiumMeeting)){b.HasAlternateKey("TenantId","CondominiumId","MeetingId","Id");b.HasOne(typeof(CondominiumMeeting)).WithMany().HasForeignKey("TenantId","CondominiumId","MeetingId").HasPrincipalKey("TenantId","CondominiumId","Id");}}
  void Link(Type source,Type target,string property,bool person=false)=>m.Entity(source).HasOne(target).WithMany().HasForeignKey(person?new[]{"TenantId",property}:new[]{"TenantId","CondominiumId",property}).HasPrincipalKey(person?new[]{"TenantId","Id"}:new[]{"TenantId","CondominiumId","Id"});
  void Child(Type source,Type target,string property)=>m.Entity(source).HasOne(target).WithMany().HasForeignKey("TenantId","CondominiumId","MeetingId",property).HasPrincipalKey("TenantId","CondominiumId","MeetingId","Id");
  Link(typeof(CondominiumMeeting),typeof(AccountingPeriod),"AccountingPeriodId");Link(typeof(MeetingSnapshot),typeof(AllocationTable),"AllocationTableId");
  m.Entity<CondominiumMeeting>().HasOne<MeetingSnapshot>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.Id,x.CurrentSnapshotId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.MeetingId,x.Id});
  foreach(var t in new[]{typeof(MeetingNotice),typeof(MeetingProxy),typeof(MeetingAttendance)})Child(t,typeof(MeetingSnapshot),"SnapshotId");
  Link(typeof(MeetingNotice),typeof(Person),"PersonId",true);Link(typeof(MeetingProxy),typeof(Person),"DelegatorId",true);Link(typeof(MeetingProxy),typeof(Person),"DelegateId",true);Link(typeof(MeetingAttendance),typeof(Person),"PersonId",true);Link(typeof(MeetingAttendance),typeof(Person),"RepresentedById",true);
  Child(typeof(MeetingNoticeEvent),typeof(MeetingNotice),"NoticeId");Child(typeof(MeetingProxyUnit),typeof(MeetingProxy),"ProxyId");Child(typeof(MeetingAttendance),typeof(MeetingSession),"SessionId");Child(typeof(MeetingAttendance),typeof(MeetingProxy),"ProxyId");Child(typeof(MeetingAttendanceUnit),typeof(MeetingAttendance),"AttendanceId");Child(typeof(MeetingAttendanceUnit),typeof(MeetingSession),"SessionId");
  Link(typeof(MeetingProxyUnit),typeof(PropertyUnit),"PropertyUnitId");Link(typeof(MeetingAttendanceUnit),typeof(PropertyUnit),"PropertyUnitId");
  m.Entity<CondominiumMeeting>(b=>{b.Property(x=>x.Version).IsConcurrencyToken();b.Property(x=>x.Description).HasMaxLength(4000);b.Property(x=>x.RemoteMeetingUrl).HasMaxLength(1000);b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.AccountingPeriodId,x.Status});b.ToTable("Meetings",t=>t.HasCheckConstraint("CK_Meeting_State","\"Version\">0 AND \"Type\" BETWEEN 0 AND 1 AND \"Status\" BETWEEN 0 AND 5"));});
  m.Entity<MeetingSession>(b=>{b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.Call}).IsUnique();b.ToTable("MeetingSessions",t=>t.HasCheckConstraint("CK_MeetingSession_State","\"Call\" BETWEEN 0 AND 1 AND \"Status\" BETWEEN 0 AND 3"));});
  m.Entity<MeetingAgendaItem>(b=>{b.Property(x=>x.Description).HasMaxLength(4000);b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.Position}).IsUnique();b.ToTable("MeetingAgendaItems",t=>t.HasCheckConstraint("CK_Agenda_Position","\"Position\">0 AND ((\"RelatedId\" IS NULL)=(\"RelatedType\" IS NULL))"));});
  m.Entity<MeetingSnapshot>(b=>{b.Property(x=>x.ContentJson).HasColumnType("jsonb");b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.Version}).IsUnique();});
  m.Entity<MeetingNotice>(b=>b.Property(x=>x.Contact).HasMaxLength(1000));
  m.Entity<MeetingNoticeEvent>(b=>{b.Property(x=>x.Reference).HasMaxLength(1000);b.Property(x=>x.Notes).HasMaxLength(4000);});
  m.Entity<MeetingProxy>(b=>{b.Property(x=>x.Notes).HasMaxLength(4000);b.Property(x=>x.DocumentReference).HasMaxLength(1000);b.ToTable("MeetingProxies",t=>t.HasCheckConstraint("CK_Proxy_People","\"DelegatorId\"<>\"DelegateId\" AND \"Status\" BETWEEN 0 AND 4"));});
  m.Entity<MeetingProxyUnit>().HasIndex(x=>new{x.TenantId,x.MeetingId,x.PropertyUnitId}).IsUnique().HasFilter("\"IsActive\"");
  m.Entity<MeetingAttendance>(b=>{b.Property(x=>x.Notes).HasMaxLength(4000);b.ToTable("MeetingAttendances",t=>t.HasCheckConstraint("CK_Attendance_Dates","\"CheckOutAt\" IS NULL OR \"CheckOutAt\">=\"CheckInAt\""));});
  m.Entity<MeetingAttendanceUnit>(b=>{b.Property(x=>x.Millesimals).HasPrecision(18,6);b.HasIndex(x=>new{x.TenantId,x.SessionId,x.PropertyUnitId}).IsUnique().HasFilter("\"IsActive\"");});
  Link(typeof(MeetingAbsence),typeof(Person),"PersonId",true);Child(typeof(MeetingAbsence),typeof(MeetingSession),"SessionId");
  m.Entity<MeetingRevision>(b=>{b.Property(x=>x.ContentJson).HasColumnType("jsonb");b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.Version}).IsUnique();});
 }
}
