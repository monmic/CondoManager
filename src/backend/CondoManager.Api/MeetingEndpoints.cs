using CondoManager.Application;
namespace CondoManager.Api;
public static class MeetingEndpoints
{
 public static void MapMeetings(this WebApplication app)
 {
  var g=app.MapGroup("/api/meetings/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
  g.MapGet("/",(Guid condo,Guid? period,IMeetingService s,CancellationToken ct)=>s.List(condo,period,ct));
  g.MapPost("/",(Guid condo,MeetingInput i,IMeetingService s,CancellationToken ct)=>s.Save(condo,null,i,ct));
  g.MapGet("/{id:guid}",(Guid condo,Guid id,IMeetingService s,CancellationToken ct)=>s.Details(condo,id,ct));
  g.MapPut("/{id:guid}",(Guid condo,Guid id,MeetingInput i,IMeetingService s,CancellationToken ct)=>s.Save(condo,id,i,ct));
  g.MapPost("/{id:guid}/status",(Guid condo,Guid id,MeetingTransition i,IMeetingService s,CancellationToken ct)=>s.Transition(condo,id,i,ct));
  g.MapPost("/{id:guid}/sessions/{session:guid}",(Guid condo,Guid id,Guid session,SessionTransition i,IMeetingService s,CancellationToken ct)=>s.Session(condo,id,session,i,ct));
  g.MapPost("/{id:guid}/agenda",(Guid condo,Guid id,AgendaInput i,IMeetingService s,CancellationToken ct)=>s.Agenda(condo,id,i,ct));
  g.MapPost("/{id:guid}/agenda/{item:guid}/delete",(Guid condo,Guid id,Guid item,MeetingVersion i,IMeetingService s,CancellationToken ct)=>s.DeleteAgenda(condo,id,item,i,ct));
  g.MapPost("/{id:guid}/agenda/reorder",(Guid condo,Guid id,AgendaOrder i,IMeetingService s,CancellationToken ct)=>s.Reorder(condo,id,i,ct));
  g.MapPost("/{id:guid}/participants/preview",(Guid condo,Guid id,ParticipantsInput i,IMeetingService s,CancellationToken ct)=>s.PreviewParticipants(condo,id,i,ct));
  g.MapPost("/{id:guid}/snapshots",(Guid condo,Guid id,ParticipantsInput i,IMeetingService s,CancellationToken ct)=>s.Consolidate(condo,id,i,ct));
  g.MapPost("/{id:guid}/notices",(Guid condo,Guid id,NoticeInput i,IMeetingService s,CancellationToken ct)=>s.Notice(condo,id,i,ct));
  g.MapPost("/{id:guid}/notices/{notice:guid}/status",(Guid condo,Guid id,Guid notice,NoticeTransition i,IMeetingService s,CancellationToken ct)=>s.NoticeStatus(condo,id,notice,i,ct));
  g.MapPost("/{id:guid}/proxies",(Guid condo,Guid id,ProxyInput i,IMeetingService s,CancellationToken ct)=>s.Proxy(condo,id,i,ct));
  g.MapPost("/{id:guid}/proxies/{proxy:guid}/status",(Guid condo,Guid id,Guid proxy,ProxyTransition i,IMeetingService s,CancellationToken ct)=>s.ProxyStatus(condo,id,proxy,i,ct));
  g.MapPost("/{id:guid}/attendances",(Guid condo,Guid id,AttendanceInput i,IMeetingService s,CancellationToken ct)=>s.Attend(condo,id,i,ct));
  g.MapPost("/{id:guid}/attendances/{attendance:guid}/exit",(Guid condo,Guid id,Guid attendance,AttendanceExit i,IMeetingService s,CancellationToken ct)=>s.Leave(condo,id,attendance,i,ct));
  g.MapPost("/{id:guid}/absences",(Guid condo,Guid id,AbsenceInput i,IMeetingService s,CancellationToken ct)=>s.Absence(condo,id,i,ct));
  g.MapGet("/{id:guid}/history",(Guid condo,Guid id,IMeetingService s,CancellationToken ct)=>s.History(condo,id,ct));
 }
}
