using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AppDbContext
{
 public DbSet<VotingRule> VotingRules=>Set<VotingRule>();
 public DbSet<MeetingQuorumCheck> MeetingQuorumChecks=>Set<MeetingQuorumCheck>();
 public DbSet<MeetingVote> MeetingVotes=>Set<MeetingVote>();
 public DbSet<MeetingBallot> MeetingBallots=>Set<MeetingBallot>();
 public DbSet<MeetingBallotRevision> MeetingBallotRevisions=>Set<MeetingBallotRevision>();
 public DbSet<MeetingResolution> MeetingResolutions=>Set<MeetingResolution>();
 public DbSet<ResolutionApplication> ResolutionApplications=>Set<ResolutionApplication>();
 public DbSet<MeetingAgendaDisposition> MeetingAgendaDispositions=>Set<MeetingAgendaDisposition>();
 public DbSet<MeetingCompletion> MeetingCompletions=>Set<MeetingCompletion>();
 public DbSet<MeetingDecisionRequest> MeetingDecisionRequests=>Set<MeetingDecisionRequest>();
 private void ConfigureVoting(ModelBuilder m)
 {
  ConfigureTenant(m.Entity<VotingRule>());ConfigureTenant(m.Entity<MeetingQuorumCheck>());ConfigureTenant(m.Entity<MeetingVote>());ConfigureTenant(m.Entity<MeetingBallot>());ConfigureTenant(m.Entity<MeetingBallotRevision>());ConfigureTenant(m.Entity<MeetingResolution>());ConfigureTenant(m.Entity<ResolutionApplication>());ConfigureTenant(m.Entity<MeetingAgendaDisposition>());ConfigureTenant(m.Entity<MeetingCompletion>());ConfigureTenant(m.Entity<MeetingDecisionRequest>());
  var types=new[]{typeof(VotingRule),typeof(MeetingQuorumCheck),typeof(MeetingVote),typeof(MeetingBallot),typeof(MeetingBallotRevision),typeof(MeetingResolution),typeof(ResolutionApplication),typeof(MeetingAgendaDisposition),typeof(MeetingCompletion),typeof(MeetingDecisionRequest)};
  foreach(var type in types){var b=m.Entity(type);b.HasAlternateKey("TenantId","CondominiumId","Id");b.HasOne(typeof(Condominium)).WithMany().HasForeignKey("TenantId","CondominiumId").HasPrincipalKey("TenantId","Id");b.HasIndex("TenantId","CondominiumId");if(type!=typeof(VotingRule)){b.HasAlternateKey("TenantId","CondominiumId","MeetingId","Id");b.HasOne(typeof(CondominiumMeeting)).WithMany().HasForeignKey("TenantId","CondominiumId","MeetingId").HasPrincipalKey("TenantId","CondominiumId","Id");}foreach(var property in new[]{"CriteriaJson","ContentJson","ElectorateJson","ResultJson"})if(b.Metadata.FindProperty(property)!=null)b.Property(property).HasColumnType("jsonb");}
  void Child(Type source,Type target,string property)=>m.Entity(source).HasOne(target).WithMany().HasForeignKey("TenantId","CondominiumId","MeetingId",property).HasPrincipalKey("TenantId","CondominiumId","MeetingId","Id");
  void Rule(Type source)=>m.Entity(source).HasOne(typeof(VotingRule)).WithMany().HasForeignKey("TenantId","CondominiumId","RuleId").HasPrincipalKey("TenantId","CondominiumId","Id");
  m.Entity<VotingRule>(b=>{b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.Code,x.Version}).IsUnique();b.HasIndex(x=>x.PreviousVersionId).IsUnique();b.HasOne<VotingRule>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CondominiumId,x.PreviousVersionId}).HasPrincipalKey(x=>new{x.TenantId,x.CondominiumId,x.Id});b.Property(x=>x.Status).IsConcurrencyToken();b.Property(x=>x.ReviewNote).HasMaxLength(2000);b.ToTable("VotingRules",t=>t.HasCheckConstraint("CK_VotingRule_Version","\"Version\">0 AND \"Status\" BETWEEN 0 AND 2 AND (\"EffectiveTo\" IS NULL OR \"EffectiveTo\">=\"EffectiveFrom\")"));});
  Rule(typeof(MeetingVote));Rule(typeof(MeetingQuorumCheck));Child(typeof(MeetingQuorumCheck),typeof(MeetingSession),"SessionId");Child(typeof(MeetingQuorumCheck),typeof(MeetingSnapshot),"SnapshotId");
  Child(typeof(MeetingVote),typeof(MeetingSession),"SessionId");Child(typeof(MeetingVote),typeof(MeetingAgendaItem),"AgendaItemId");Child(typeof(MeetingVote),typeof(MeetingQuorumCheck),"QuorumCheckId");Child(typeof(MeetingVote),typeof(MeetingSnapshot),"SnapshotId");
  m.Entity<MeetingVote>(b=>{b.Property(x=>x.Status).IsConcurrencyToken();b.HasIndex(x=>new{x.TenantId,x.MeetingId}).IsUnique().HasFilter("\"Status\"=1");b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.SessionId,x.AgendaItemId}).IsUnique().HasFilter("\"Status\"=1");b.ToTable("MeetingVotes",t=>t.HasCheckConstraint("CK_MeetingVote_Status","\"Status\" BETWEEN 0 AND 4 AND \"RuleVersion\">0"));});
  Child(typeof(MeetingBallot),typeof(MeetingVote),"VoteId");m.Entity<MeetingBallot>(b=>{b.Property(x=>x.Millesimals).HasPrecision(18,6);b.Property(x=>x.Revision).IsConcurrencyToken();b.HasIndex(x=>new{x.TenantId,x.VoteId,x.HeadKey}).IsUnique();b.ToTable("MeetingBallots",t=>t.HasCheckConstraint("CK_Ballot_Choice","\"Choice\" BETWEEN 0 AND 2 AND \"Millesimals\">=0 AND \"Revision\">0"));});
  Child(typeof(MeetingBallotRevision),typeof(MeetingBallot),"BallotId");Child(typeof(MeetingBallotRevision),typeof(MeetingVote),"VoteId");m.Entity<MeetingBallotRevision>().HasIndex(x=>new{x.TenantId,x.BallotId,x.Version}).IsUnique();
  Child(typeof(MeetingResolution),typeof(MeetingVote),"VoteId");Child(typeof(MeetingResolution),typeof(MeetingAgendaItem),"AgendaItemId");Child(typeof(MeetingResolution),typeof(MeetingResolution),"PreviousResolutionId");m.Entity<MeetingResolution>(b=>{b.Property(x=>x.ResolutionText).HasMaxLength(12000);b.Property(x=>x.CorrectionReason).HasMaxLength(2000);b.HasIndex(x=>new{x.TenantId,x.VoteId,x.Version}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.RequestId}).IsUnique();b.HasIndex(x=>x.PreviousResolutionId).IsUnique();});
  Child(typeof(ResolutionApplication),typeof(MeetingResolution),"ResolutionId");m.Entity<ResolutionApplication>(b=>{b.HasIndex(x=>new{x.TenantId,x.CondominiumId,x.RelatedType,x.RelatedId}).IsUnique();b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.RequestId}).IsUnique();});
  Child(typeof(MeetingAgendaDisposition),typeof(MeetingAgendaItem),"AgendaItemId");m.Entity<MeetingAgendaDisposition>(b=>{b.Property(x=>x.Reason).HasMaxLength(2000);b.HasIndex(x=>new{x.TenantId,x.MeetingId,x.AgendaItemId}).IsUnique();});
  Child(typeof(MeetingCompletion),typeof(MeetingSession),"SessionId");m.Entity<MeetingCompletion>().HasIndex(x=>new{x.TenantId,x.MeetingId}).IsUnique();m.Entity<MeetingDecisionRequest>().HasIndex(x=>new{x.TenantId,x.MeetingId,x.RequestId}).IsUnique();
 }
}
