namespace CondoManager.Domain;
public enum MeetingType { Ordinary, Extraordinary }
public enum MeetingStatus { Draft, Scheduled, Convened, InProgress, Completed, Cancelled }
public enum MeetingCall { First, Second }
public enum SessionStatus { Planned, InProgress, Ended, NotHeld }
public enum AgendaCategory { General, BudgetApproval, FinalStatementApproval, AdministratorAppointment, Maintenance, ExtraordinaryExpense, Other }
public enum MeetingReference { Budget, FinalStatement, AccountingPeriod }
public enum NoticeChannel { Email, PEC, RegisteredMail, HandDelivery, Other }
public enum NoticeStatus { Draft, Ready, Sent, Delivered, Failed, Cancelled }
public enum ProxyStatus { Draft, Submitted, Verified, Rejected, Revoked }
public enum AttendanceType { InPerson, Remote, ByProxy }
public sealed class CondominiumMeeting : CondominiumEntity
{
 public string Title { get; private set; } = "";
 public MeetingType Type { get; private set; }
 public MeetingStatus Status { get; private set; }
 public Guid? AccountingPeriodId { get; private set; }
 public string Location { get; private set; } = "";
 public string? RemoteMeetingUrl { get; private set; }
 public string? Description { get; private set; }
 public Guid CreatedBy { get; private set; }
 public int Version { get; private set; } = 1;
 public Guid? CurrentSnapshotId { get; private set; }
 public CondominiumMeeting(Guid tenantId,Guid condominiumId,string title,MeetingType type,Guid createdBy):base(tenantId,condominiumId){Title=Guard.Text(title);Type=Guard.EnumValue(type);CreatedBy=Guard.Id(createdBy);}
 public void Configure(string title,MeetingType type,Guid? period,string location,string? remote,string? description){EnsureEditable();Title=Guard.Text(title);Type=Guard.EnumValue(type);AccountingPeriodId=period;Location=Guard.Text(location);RemoteMeetingUrl=Guard.Optional(remote,1000);if(RemoteMeetingUrl!=null && (!Uri.TryCreate(RemoteMeetingUrl,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo)))throw new ArgumentException("URL remoto HTTPS senza credenziali obbligatorio.");Description=Guard.Optional(description,4000);}
 public void EnsureEditable(){if(Status is not (MeetingStatus.Draft or MeetingStatus.Scheduled))throw new InvalidOperationException("Assemblea consolidata: modifiche generali vietate.");}
 public void EnsureOperational(){if(Status is MeetingStatus.Completed or MeetingStatus.Cancelled)throw new InvalidOperationException("Assemblea conclusa o annullata.");}
 public void CheckVersion(int version){if(version!=Version)throw new InvalidOperationException("Versione superata: ricaricare l'assemblea.");}
 public void Complete(){if(Status!=MeetingStatus.InProgress)throw new InvalidOperationException("Assemblea non avviata.");Status=MeetingStatus.Completed;}
 public void Touch()=>Version++;
 public void Reconcile(){if(Status!=MeetingStatus.Convened)throw new InvalidOperationException("Riconciliazione non consentita.");Status=MeetingStatus.Scheduled;}
 public void Snapshot(Guid id){EnsureOperational();CurrentSnapshotId=Guard.Id(id);}
 public void Transition(MeetingStatus target){Guard.EnumValue(target);if(!((Status==MeetingStatus.Draft&&target==MeetingStatus.Scheduled)||(Status==MeetingStatus.Scheduled&&target==MeetingStatus.Convened)||(Status==MeetingStatus.Convened&&target==MeetingStatus.InProgress)||(target==MeetingStatus.Cancelled&&Status is MeetingStatus.Draft or MeetingStatus.Scheduled or MeetingStatus.Convened)))throw new InvalidOperationException("Transizione non consentita; conclusione e votazioni appartengono alla M07.2.");Status=target;}
}
public abstract class MeetingEntity : CondominiumEntity
{
 public Guid MeetingId { get; private set; }
 protected MeetingEntity(Guid tenantId,Guid condominiumId,Guid meetingId):base(tenantId,condominiumId)=>MeetingId=Guard.Id(meetingId);
}
public sealed class MeetingSession:MeetingEntity
{
 public MeetingCall Call { get; private set; }
 public DateTimeOffset StartsAt { get; private set; }
 public SessionStatus Status { get; private set; }
 public MeetingSession(Guid tenantId,Guid condominiumId,Guid meetingId,MeetingCall call,DateTimeOffset startsAt):base(tenantId,condominiumId,meetingId){Call=Guard.EnumValue(call);Reschedule(startsAt);}
 public void Reschedule(DateTimeOffset date){if(date==default||date.Offset!=TimeSpan.Zero)throw new ArgumentException("Data UTC esplicita obbligatoria.");StartsAt=date;}
 public void Transition(SessionStatus target){if(!((Status==SessionStatus.Planned&&target is SessionStatus.InProgress or SessionStatus.NotHeld)||(Status==SessionStatus.InProgress&&target==SessionStatus.Ended)))throw new InvalidOperationException("Transizione sessione non consentita.");Status=target;}
}
public sealed class MeetingAgendaItem:MeetingEntity
{
 public int Position { get; private set; }
 public string Title { get; private set; }="";
 public string? Description { get; private set; }
 public AgendaCategory Category { get; private set; }
 public MeetingReference? RelatedType { get; private set; }
 public Guid? RelatedId { get; private set; }
 public MeetingAgendaItem(Guid tenantId,Guid condominiumId,Guid meetingId):base(tenantId,condominiumId,meetingId){}
 public void Update(int position,string title,string? description,AgendaCategory category,MeetingReference? type,Guid? id){if(position<1)throw new ArgumentException("Posizione positiva obbligatoria.");if((type==null)!=(id==null))throw new ArgumentException("Riferimento incompleto.");Position=position;Title=Guard.Text(title);Description=Guard.Optional(description,4000);Category=Guard.EnumValue(category);RelatedType=type==null?null:Guard.EnumValue(type.Value);RelatedId=id;}
}
public sealed class MeetingSnapshot:MeetingEntity
{
 public int Version { get; private set; }
 public DateOnly ReferenceDate { get; private set; }
 public Guid? AllocationTableId { get; private set; }
 public string ContentJson { get; private set; }
 public string Fingerprint { get; private set; }
 public string Reason { get; private set; }
 public Guid CreatedBy { get; private set; }
 public MeetingSnapshot(Guid tenantId,Guid condominiumId,Guid meetingId,int version,DateOnly referenceDate,Guid? allocationTableId,string contentJson,string fingerprint,string reason,Guid createdBy):base(tenantId,condominiumId,meetingId){Version=version;Guard.Dates(referenceDate,null);ReferenceDate=referenceDate;AllocationTableId=allocationTableId;ContentJson=contentJson;Fingerprint=Guard.Text(fingerprint);Reason=Guard.Text(reason);CreatedBy=Guard.Id(createdBy);}
}
public sealed class MeetingNotice:MeetingEntity
{
 public Guid SnapshotId { get; private set; }
 public Guid PersonId { get; private set; }
 public string RecipientName { get; private set; }
 public string Contact { get; private set; }
 public NoticeChannel Channel { get; private set; }
 public NoticeStatus Status { get; private set; }
 public DateTimeOffset? SentAt { get; private set; }
 public MeetingNotice(Guid tenantId,Guid condominiumId,Guid meetingId,Guid snapshotId,Guid personId,string recipientName,string contact,NoticeChannel channel):base(tenantId,condominiumId,meetingId){SnapshotId=Guard.Id(snapshotId);PersonId=Guard.Id(personId);RecipientName=Guard.Text(recipientName);Contact=Guard.Text(contact,1000);Channel=Guard.EnumValue(channel);}
 public void Transition(NoticeStatus target,DateTimeOffset at){Guard.EnumValue(target);if(at==default||at>DateTimeOffset.UtcNow.AddMinutes(1))throw new ArgumentException("Data evento non valida.");if(!((Status==NoticeStatus.Draft&&target==NoticeStatus.Ready)||(Status==NoticeStatus.Ready&&target==NoticeStatus.Sent)||(Status==NoticeStatus.Sent&&target is NoticeStatus.Delivered or NoticeStatus.Failed)||(Status is NoticeStatus.Draft or NoticeStatus.Ready or NoticeStatus.Failed&&target==NoticeStatus.Cancelled)))throw new InvalidOperationException("Transizione convocazione non valida; per un nuovo invio creare una nuova convocazione.");if(SentAt!=null&&at<SentAt)throw new ArgumentException("Evento precedente all'invio.");Status=target;if(target==NoticeStatus.Sent)SentAt=at;}
}
public sealed class MeetingNoticeEvent:MeetingEntity
{
 public Guid NoticeId { get; private set; }
 public NoticeStatus Status { get; private set; }
 public DateTimeOffset DeclaredAt { get; private set; }
 public Guid DeclaredBy { get; private set; }
 public string? Reference { get; private set; }
 public string? Notes { get; private set; }
 public MeetingNoticeEvent(Guid tenantId,Guid condominiumId,Guid meetingId,Guid noticeId,NoticeStatus status,DateTimeOffset declaredAt,Guid declaredBy,string? reference,string? notes):base(tenantId,condominiumId,meetingId){NoticeId=noticeId;Status=status;DeclaredAt=declaredAt;DeclaredBy=declaredBy;Reference=Guard.Optional(reference,1000);Notes=Guard.Optional(notes,4000);}
}
public sealed class MeetingProxy:MeetingEntity
{
 public Guid SnapshotId { get; private set; }
 public Guid DelegatorId { get; private set; }
 public Guid DelegateId { get; private set; }
 public string DelegatorName { get; private set; }
 public string DelegateName { get; private set; }
 public ProxyStatus Status { get; private set; }
 public DateTimeOffset GrantedAt { get; private set; }
 public DateTimeOffset? VerifiedAt { get; private set; }
 public Guid? VerifiedBy { get; private set; }
 public string? Notes { get; private set; }
 public string? DocumentReference { get; private set; }
 public MeetingProxy(Guid tenantId,Guid condominiumId,Guid meetingId,Guid snapshotId,Guid delegatorId,Guid delegateId,DateTimeOffset grantedAt,string? notes,string? documentReference,string? delegatorName=null,string? delegateName=null):base(tenantId,condominiumId,meetingId){SnapshotId=snapshotId;DelegatorName=Guard.Text(delegatorName??delegatorId.ToString());DelegateName=Guard.Text(delegateName??delegateId.ToString());DelegatorId=Guard.Id(delegatorId);DelegateId=Guard.Id(delegateId);if(delegatorId==delegateId)throw new ArgumentException("Delegante e delegato devono essere distinti.");if(grantedAt==default||grantedAt>DateTimeOffset.UtcNow.AddMinutes(1))throw new ArgumentException("Data delega non valida.");GrantedAt=grantedAt;Notes=Guard.Optional(notes,4000);DocumentReference=Guard.Optional(documentReference,1000);}
 public void Transition(ProxyStatus target,Guid actor){if(!((Status==ProxyStatus.Draft&&target==ProxyStatus.Submitted)||(Status==ProxyStatus.Submitted&&target is ProxyStatus.Verified or ProxyStatus.Rejected)||(Status is ProxyStatus.Draft or ProxyStatus.Submitted or ProxyStatus.Verified&&target==ProxyStatus.Revoked)))throw new InvalidOperationException("Transizione delega non consentita.");Status=target;if(target==ProxyStatus.Verified){VerifiedAt=DateTimeOffset.UtcNow;VerifiedBy=actor;}}
}
public sealed class MeetingProxyUnit:MeetingEntity
{
 public Guid ProxyId { get; private set; }
 public Guid PropertyUnitId { get; private set; }
 public bool IsActive { get; private set; }=true;
 public MeetingProxyUnit(Guid tenantId,Guid condominiumId,Guid meetingId,Guid proxyId,Guid propertyUnitId):base(tenantId,condominiumId,meetingId){ProxyId=proxyId;PropertyUnitId=propertyUnitId;}
 public void Release()=>IsActive=false;
}
public sealed class MeetingAttendance:MeetingEntity
{
 public Guid SessionId { get; private set; }
 public Guid SnapshotId { get; private set; }
 public Guid PersonId { get; private set; }
 public AttendanceType Type { get; private set; }
 public Guid? ProxyId { get; private set; }
 public Guid? RepresentedById { get; private set; }
 public DateTimeOffset CheckInAt { get; private set; }
 public DateTimeOffset? CheckOutAt { get; private set; }
 public string? Notes { get; private set; }
 public MeetingAttendance(Guid tenantId,Guid condominiumId,Guid meetingId,Guid sessionId,Guid snapshotId,Guid personId,AttendanceType type,Guid? proxyId,Guid? representedById,DateTimeOffset checkInAt,string? notes):base(tenantId,condominiumId,meetingId){SessionId=sessionId;SnapshotId=snapshotId;PersonId=personId;Type=Guard.EnumValue(type);ProxyId=proxyId;RepresentedById=representedById;if((type==AttendanceType.ByProxy)!=(proxyId!=null&&representedById!=null))throw new ArgumentException("Dati delega incoerenti.");if(type!=AttendanceType.ByProxy&&(proxyId!=null||representedById!=null))throw new ArgumentException("Delega inattesa.");if(checkInAt==default||checkInAt>DateTimeOffset.UtcNow.AddMinutes(1))throw new ArgumentException("Ingresso non valido.");CheckInAt=checkInAt;Notes=Guard.Optional(notes,4000);}
 public void Leave(DateTimeOffset at){if(CheckOutAt!=null||at<CheckInAt||at>DateTimeOffset.UtcNow.AddMinutes(1))throw new InvalidOperationException("Uscita non valida.");CheckOutAt=at;}
}
public sealed class MeetingAttendanceUnit:MeetingEntity
{
 public Guid AttendanceId { get; private set; }
 public Guid SessionId { get; private set; }
 public Guid PropertyUnitId { get; private set; }
 public decimal? Millesimals { get; private set; }
 public bool IsActive { get; private set; }=true;
 public MeetingAttendanceUnit(Guid tenantId,Guid condominiumId,Guid meetingId,Guid attendanceId,Guid sessionId,Guid propertyUnitId,decimal? millesimals):base(tenantId,condominiumId,meetingId){AttendanceId=attendanceId;SessionId=sessionId;PropertyUnitId=propertyUnitId;Millesimals=millesimals;}
 public void Release()=>IsActive=false;
}
public sealed class MeetingRevision:MeetingEntity
{
 public int Version { get; private set; }
 public Guid ActorId { get; private set; }
 public string Action { get; private set; }
 public string ContentJson { get; private set; }
 public MeetingRevision(Guid tenantId,Guid condominiumId,Guid meetingId,int version,Guid actorId,string action,string contentJson):base(tenantId,condominiumId,meetingId){Version=version;ActorId=actorId;Action=action;ContentJson=contentJson;}
}
public sealed class MeetingAbsence:MeetingEntity
{
 public Guid SessionId { get; private set; }
 public Guid PersonId { get; private set; }
 public string Reason { get; private set; }
 public MeetingAbsence(Guid tenantId,Guid condominiumId,Guid meetingId,Guid sessionId,Guid personId,string reason):base(tenantId,condominiumId,meetingId){SessionId=sessionId;PersonId=Guard.Id(personId);Reason=Guard.Text(reason);}
}
