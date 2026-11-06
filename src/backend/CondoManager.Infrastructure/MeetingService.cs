using System.Security.Cryptography;
using System.Text.Json;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class MeetingService(AppDbContext db,ICurrentTenant current,IBudgetService budgetService,IFinalStatementService finalService,IVotingEngine votingEngine):IMeetingService
{
 private Guid Tenant=>current.RequireRead();
 private Guid Actor=>current.UserId??throw new AccessDeniedException();
 private static string Json(object value)=>JsonSerializer.Serialize(value);
 private static T Read<T>(string value)=>JsonSerializer.Deserialize<T>(value)!;
 private static string Hash(object value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
 private async Task Condo(Guid condo,CancellationToken ct){_ = Tenant;if(!await db.Condominiums.AnyAsync(x=>x.Id==condo,ct))throw new NotFoundException();}
 private async Task<CondominiumMeeting> Find(Guid condo,Guid id,CancellationToken ct){await Condo(condo,ct);return await db.Meetings.SingleOrDefaultAsync(x=>x.Id==id&&x.CondominiumId==condo,ct)??throw new NotFoundException();}
 private async Task<MeetingDetails> Mutate(Guid condo,Guid id,int version,string action,Func<CondominiumMeeting,Task> change,CancellationToken ct,Func<Task<bool>>? replay=null)
 {
  current.RequireWrite();await using var tx=await db.Database.BeginTransactionAsync(ct);await Condo(condo,ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"Id\"={condo} AND \"TenantId\"={Tenant} FOR UPDATE",ct);
  if(!await db.Condominiums.AnyAsync(x=>x.Id==condo&&x.IsActive,ct))throw new InvalidOperationException("Condominio inattivo.");
  var row=await Find(condo,id,ct);if(replay!=null&&await replay()){await tx.CommitAsync(ct);return await Details(condo,id,ct);}row.CheckVersion(version);row.EnsureOperational();await change(row);row.Touch();await db.SaveChangesAsync(ct);
  var result=await Details(condo,id,ct);db.Add(new MeetingRevision(Tenant,condo,id,row.Version,Actor,action,Json(result)));
  db.SecurityAudits.Add(new SecurityAudit{Id=Guid.NewGuid(),TenantId=Tenant,ActorUserId=Actor,SubjectId=id,Timestamp=DateTimeOffset.UtcNow,Event="Meeting."+action});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return result;
 }
 public async Task<IReadOnlyList<MeetingDto>> List(Guid condo,Guid? period,CancellationToken ct){await Condo(condo,ct);var rows=await db.Meetings.AsNoTracking().Where(x=>x.CondominiumId==condo&&(period==null||x.AccountingPeriodId==period)).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct);var result=new List<MeetingDto>();foreach(var row in rows)result.Add(await Dto(row,ct));return result;}
 private async Task<MeetingDto> Dto(CondominiumMeeting m,CancellationToken ct)
 {
  var dispositions=await db.MeetingAgendaDispositions.Where(x=>x.MeetingId==m.Id).ToListAsync(ct);var resolutions=await db.MeetingResolutions.Where(x=>x.MeetingId==m.Id).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct);var open=await db.MeetingVotes.Where(x=>x.MeetingId==m.Id&&x.Status==MeetingVoteStatus.Open).Select(x=>x.AgendaItemId).ToArrayAsync(ct);
  string State(Guid agenda)=>dispositions.SingleOrDefault(x=>x.AgendaItemId==agenda)?.Disposition.ToString()??resolutions.FirstOrDefault(x=>x.AgendaItemId==agenda)?.Outcome.ToString()??(open.Contains(agenda)?"Voting":"Planned");
  return new(m.Id,m.CondominiumId,m.AccountingPeriodId,m.Title,m.Type,m.Status,m.Location,m.RemoteMeetingUrl,m.Description,m.CreatedBy,m.CreatedAt,m.UpdatedAt,m.Version,m.CurrentSnapshotId,(await db.MeetingSessions.Where(x=>x.MeetingId==m.Id).OrderBy(x=>x.Call).ToListAsync(ct)).Select(x=>new SessionDto(x.Id,x.Call,x.StartsAt,x.Status)).ToArray(),(await db.MeetingAgendaItems.Where(x=>x.MeetingId==m.Id).OrderBy(x=>x.Position).ToListAsync(ct)).Select(x=>new AgendaDto(x.Id,x.Position,x.Title,x.Description,x.Category,x.RelatedType,x.RelatedId,State(x.Id))).ToArray());
 }
 public async Task<MeetingDetails> Details(Guid condo,Guid id,CancellationToken ct)
 {
  var row=await Find(condo,id,ct);var snapshots=(await db.MeetingSnapshots.Where(x=>x.MeetingId==id).OrderBy(x=>x.Version).ToListAsync(ct)).Select(x=>new SnapshotDto(x.Id,x.Version,x.CreatedAt,x.CreatedBy,x.Reason,Read<ParticipantsPreview>(x.ContentJson))).ToArray();
  if(current.Role==StudioRole.Viewer)snapshots=snapshots.Select(x=>x with{Participants=x.Participants with{People=x.Participants.People.Select(p=>p with{Email=null,MailingAddress=null}).ToArray()}}).ToArray();
  var ns=await db.MeetingNotices.Where(x=>x.MeetingId==id).OrderBy(x=>x.CreatedAt).ToListAsync(ct);var ne=await db.MeetingNoticeEvents.Where(x=>x.MeetingId==id).OrderBy(x=>x.CreatedAt).ToListAsync(ct);
  var proxies=await db.MeetingProxies.Where(x=>x.MeetingId==id).OrderBy(x=>x.CreatedAt).ToListAsync(ct);var pu=await db.MeetingProxyUnits.Where(x=>x.MeetingId==id).ToListAsync(ct);var at=await db.MeetingAttendances.Where(x=>x.MeetingId==id).OrderBy(x=>x.CreatedAt).ToListAsync(ct);var au=await db.MeetingAttendanceUnits.Where(x=>x.MeetingId==id).ToListAsync(ct);
  return new(await Dto(row,ct),snapshots,current.Role==StudioRole.Viewer?[]:ns.Select(n=>new NoticeDto(n.Id,n.SnapshotId,n.PersonId,n.RecipientName,n.Contact,n.Channel,n.Status,n.SentAt,ne.Where(e=>e.NoticeId==n.Id).Select(e=>new NoticeEventDto(e.Id,e.Status,e.DeclaredAt,e.DeclaredBy,e.Reference,e.Notes)).ToArray())).ToArray(),proxies.Select(p=>new ProxyDto(p.Id,p.SnapshotId,p.DelegatorId,p.DelegateId,p.Status,p.GrantedAt,p.VerifiedAt,p.VerifiedBy,current.Role==StudioRole.Viewer?null:p.Notes,current.Role==StudioRole.Viewer?null:p.DocumentReference,pu.Where(x=>x.ProxyId==p.Id).Select(x=>x.PropertyUnitId).ToArray(),p.DelegatorName,p.DelegateName)).ToArray(),at.Select(a=>new AttendanceDto(a.Id,a.SessionId,a.SnapshotId,a.PersonId,a.Type,a.ProxyId,a.RepresentedById,a.CheckInAt,a.CheckOutAt,current.Role==StudioRole.Viewer?null:a.Notes,au.Where(x=>x.AttendanceId==a.Id).Select(x=>x.PropertyUnitId).ToArray(),au.Where(x=>x.AttendanceId==a.Id).Sum(x=>x.Millesimals??0))).ToArray(),(await db.MeetingAbsences.Where(x=>x.MeetingId==id).OrderBy(x=>x.CreatedAt).ToListAsync(ct)).Select(x=>new AbsenceDto(x.Id,x.SessionId,x.PersonId,current.Role==StudioRole.Viewer?"Assenza dichiarata":x.Reason,x.CreatedAt)).ToArray());
 }
 public async Task<MeetingDetails> Save(Guid condo,Guid? id,MeetingInput input,CancellationToken ct)
 {
  if(id!=null)return await Mutate(condo,id.Value,input.ExpectedVersion,"Edited",async row=>{row.EnsureEditable();await Configure(row,input,ct);},ct);
  current.RequireWrite();if(input.ExpectedVersion!=0)throw new ArgumentException("Versione iniziale non valida.");await using var tx=await db.Database.BeginTransactionAsync(ct);await Condo(condo,ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"Id\"={condo} AND \"TenantId\"={Tenant} FOR UPDATE",ct);
  if(!await db.Condominiums.AnyAsync(x=>x.Id==condo&&x.IsActive,ct))throw new InvalidOperationException("Condominio inattivo.");
  var meeting=new CondominiumMeeting(Tenant,condo,input.Title,input.Type,Actor);db.Add(meeting);await Configure(meeting,input,ct);await db.SaveChangesAsync(ct);var result=await Details(condo,meeting.Id,ct);db.Add(new MeetingRevision(Tenant,condo,meeting.Id,1,Actor,"Created",Json(result)));db.SecurityAudits.Add(new SecurityAudit{Id=Guid.NewGuid(),TenantId=Tenant,ActorUserId=Actor,SubjectId=meeting.Id,Timestamp=DateTimeOffset.UtcNow,Event="Meeting.Created"});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return result;
 }
 private async Task Configure(CondominiumMeeting row,MeetingInput i,CancellationToken ct)
 {
  if(i.AccountingPeriodId!=null&&!await db.AccountingPeriods.AnyAsync(x=>x.Id==i.AccountingPeriodId&&x.CondominiumId==row.CondominiumId,ct))throw new NotFoundException();
  if(i.SecondCallAt!=null&&i.SecondCallAt<=i.FirstCallAt)throw new ArgumentException("La seconda convocazione deve seguire la prima.");row.Configure(i.Title,i.Type,i.AccountingPeriodId,i.Location,i.RemoteMeetingUrl,i.Description);
  var sessions=await db.MeetingSessions.Where(x=>x.MeetingId==row.Id).ToListAsync(ct);var first=sessions.SingleOrDefault(x=>x.Call==MeetingCall.First);if(first==null)db.Add(new MeetingSession(Tenant,row.CondominiumId,row.Id,MeetingCall.First,i.FirstCallAt));else first.Reschedule(i.FirstCallAt);
  var second=sessions.SingleOrDefault(x=>x.Call==MeetingCall.Second);if(i.SecondCallAt!=null){if(second==null)db.Add(new MeetingSession(Tenant,row.CondominiumId,row.Id,MeetingCall.Second,i.SecondCallAt.Value));else second.Reschedule(i.SecondCallAt.Value);}else if(second!=null)db.Remove(second);
 }
 public Task<MeetingDetails> Transition(Guid condo,Guid id,MeetingTransition i,CancellationToken ct)=>Mutate(condo,id,i.ExpectedVersion,"StatusChanged",async row=>{
  if(i.Status==MeetingStatus.Convened){if(row.CurrentSnapshotId==null||!await db.MeetingAgendaItems.AnyAsync(x=>x.MeetingId==id,ct))throw new InvalidOperationException("Consolidare partecipanti e ordine del giorno.");var snap=await Snapshot(row,ct);var people=snap.People.Select(p=>p.Id).ToArray();var notified=await db.MeetingNotices.Where(n=>n.MeetingId==id&&n.SnapshotId==row.CurrentSnapshotId&&n.Status!=Domain.NoticeStatus.Cancelled).Select(n=>n.PersonId).Distinct().ToArrayAsync(ct);if(people.Except(notified).Any())throw new InvalidOperationException("Preparare una convocazione per ogni destinatario dello snapshot.");}
  if(i.Status==MeetingStatus.InProgress)throw new InvalidOperationException("Avviare esplicitamente una sessione.");
  if(i.Status==MeetingStatus.Cancelled){foreach(var session in await db.MeetingSessions.Where(x=>x.MeetingId==id&&x.Status==SessionStatus.Planned).ToListAsync(ct))session.Transition(SessionStatus.NotHeld);}
  row.Transition(i.Status);
 },ct);
 public Task<MeetingDetails> Session(Guid condo,Guid id,Guid session,SessionTransition i,CancellationToken ct)=>Mutate(condo,id,i.ExpectedVersion,"SessionChanged",async row=>{
  if(row.Status is not (MeetingStatus.Convened or MeetingStatus.InProgress))throw new InvalidOperationException("Convocare l'assemblea prima di gestire le sessioni.");var s=await db.MeetingSessions.SingleOrDefaultAsync(x=>x.MeetingId==id&&x.Id==session,ct)??throw new NotFoundException();
  if(i.Status==SessionStatus.InProgress){if(await db.MeetingSessions.AnyAsync(x=>x.MeetingId==id&&x.Status==SessionStatus.InProgress,ct))throw new InvalidOperationException("Altra sessione in corso.");if(s.Call==MeetingCall.Second&&await db.MeetingSessions.AnyAsync(x=>x.MeetingId==id&&x.Call==MeetingCall.First&&x.Status==SessionStatus.Planned,ct))throw new InvalidOperationException("Registrare prima l'esito della prima convocazione.");if(row.Status==MeetingStatus.Convened)row.Transition(MeetingStatus.InProgress);}
  if(i.Status==SessionStatus.Ended&&await db.MeetingVotes.AnyAsync(v=>v.SessionId==session&&v.MeetingId==id&&v.Status==MeetingVoteStatus.Open,ct))throw new InvalidOperationException("Chiudere o annullare la votazione prima di terminare la sessione.");
  if(i.Status==SessionStatus.Ended&&await db.MeetingAttendances.AnyAsync(x=>x.SessionId==session&&x.CheckOutAt==null,ct))throw new InvalidOperationException("Registrare le uscite prima di terminare la sessione.");s.Transition(i.Status);
 },ct);
 public Task<MeetingDetails> Agenda(Guid condo,Guid id,AgendaInput i,CancellationToken ct)=>Mutate(condo,id,i.ExpectedVersion,"AgendaSaved",async row=>{
  row.EnsureEditable();if(i.RelatedType!=null){var valid=i.RelatedType switch{MeetingReference.Budget=>await db.Budgets.AnyAsync(x=>x.Id==i.RelatedId&&x.CondominiumId==condo,ct),MeetingReference.FinalStatement=>await db.FinalStatementRows.AnyAsync(x=>x.Id==i.RelatedId&&x.CondominiumId==condo,ct),MeetingReference.AccountingPeriod=>await db.AccountingPeriods.AnyAsync(x=>x.Id==i.RelatedId&&x.CondominiumId==condo,ct),_=>false};if(!valid)throw new NotFoundException();}
  var item=i.Id!=null?await db.MeetingAgendaItems.SingleOrDefaultAsync(x=>x.MeetingId==id&&x.Id==i.Id,ct)??throw new NotFoundException():new MeetingAgendaItem(Tenant,condo,id);var pos=i.Id==null?1+(await db.MeetingAgendaItems.Where(x=>x.MeetingId==id).MaxAsync(x=>(int?)x.Position,ct)??0):item.Position;item.Update(pos,i.Title,i.Description,i.Category,i.RelatedType,i.RelatedId);if(i.Id==null)db.Add(item);
 },ct);
 public Task<MeetingDetails> DeleteAgenda(Guid condo,Guid id,Guid item,MeetingVersion i,CancellationToken ct)=>Mutate(condo,id,i.ExpectedVersion,"AgendaDeleted",async row=>{row.EnsureEditable();var all=await db.MeetingAgendaItems.Where(x=>x.MeetingId==id).OrderBy(x=>x.Position).ToListAsync(ct);var target=all.SingleOrDefault(x=>x.Id==item)??throw new NotFoundException();db.Remove(target);await db.SaveChangesAsync(ct);await NumberAgenda(all.Where(x=>x!=target).ToArray(),ct);},ct);
 private async Task NumberAgenda(IReadOnlyList<MeetingAgendaItem> items,CancellationToken ct){for(int n=0;n<items.Count;n++){var x=items[n];x.Update(100000+n,x.Title,x.Description,x.Category,x.RelatedType,x.RelatedId);}await db.SaveChangesAsync(ct);for(int n=0;n<items.Count;n++){var x=items[n];x.Update(n+1,x.Title,x.Description,x.Category,x.RelatedType,x.RelatedId);}}
 public Task<MeetingDetails> Reorder(Guid condo,Guid id,AgendaOrder i,CancellationToken ct)=>Mutate(condo,id,i.ExpectedVersion,"AgendaReordered",async row=>{row.EnsureEditable();var all=await db.MeetingAgendaItems.Where(x=>x.MeetingId==id).ToListAsync(ct);if(i.Ids.Count!=all.Count||i.Ids.Distinct().Count()!=all.Count||i.Ids.Except(all.Select(x=>x.Id)).Any())throw new ArgumentException("Ordine incompleto o duplicato.");await NumberAgenda(i.Ids.Select(x=>all.Single(a=>a.Id==x)).ToArray(),ct);},ct);
 public async Task<IReadOnlyList<MeetingHistoryDto>> History(Guid condo,Guid id,CancellationToken ct){current.RequireWrite();await Find(condo,id,ct);return (await db.MeetingRevisions.Where(x=>x.MeetingId==id).OrderBy(x=>x.Version).ToListAsync(ct)).Select(x=>new MeetingHistoryDto(x.Version,x.CreatedAt,x.ActorId,x.Action,x.ContentJson)).ToArray();}
}
