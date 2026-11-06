namespace CondoManager.Domain;

public enum DocumentType { MeetingNotice, MeetingMinutes, MeetingAttendance, MeetingProxy, MeetingResolution, Budget, FinalStatement, AdministrativeAttachment, Other, MeetingVoting }
public enum DocumentStatus { Draft, Final, Superseded, Archived }
public enum MinutesStatus { Draft, Ready, Finalized, Superseded }
public enum DocumentTarget { AccountingPeriod, Meeting, AgendaItem, Notice, Proxy, Resolution, Budget, FinalStatement }

public sealed class CondominiumDocument : CondominiumEntity
{
    public DocumentType Type { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public DocumentStatus Status { get; private set; }
    public int Version { get; private set; }
    public Guid? PreviousDocumentId { get; private set; }
    public string FileName { get; private set; } = "";
    public string ContentType { get; private set; } = "";
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = "";
    public string StorageKey { get; private set; } = "";
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset FinalizedAt { get; private set; }
    public string SourceFingerprint { get; private set; } = "";
    public Guid RequestId { get; private set; }
    public string RequestHash { get; private set; } = "";
    public bool OperationalOnly { get; private set; } = true;
    private CondominiumDocument() : base(Guid.NewGuid(), Guid.NewGuid()) { }
    public CondominiumDocument(Guid tenant, Guid condo, DocumentType type, string title, string? description, string fileName,
        string contentType, long size, string hash, string key, Guid actor, string fingerprint, Guid request, string requestHash,
        int version=1, Guid? previous=null) : base(tenant, condo)
    {
        Type=Guard.EnumValue(type);Title=Guard.Text(title);Description=Guard.Optional(description,4000);
        FileName=Guard.Text(fileName);ContentType=Guard.Text(contentType);if(size<=0)throw new ArgumentException("File vuoto.");
        SizeBytes=size;Sha256=Guard.Text(hash,64);StorageKey=Guard.Text(key);CreatedBy=Guard.Id(actor);SourceFingerprint=Guard.Text(fingerprint,64);
        RequestId=Guard.Id(request);RequestHash=Guard.Text(requestHash,64);if(version<1)throw new ArgumentException("Versione non valida.");Version=version;PreviousDocumentId=previous;
        Status=DocumentStatus.Final;FinalizedAt=DateTimeOffset.UtcNow;
    }
}
public sealed class DocumentLink : CondominiumEntity
{
    public Guid DocumentId { get; private set; }
    public DocumentTarget TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    private DocumentLink():base(Guid.NewGuid(),Guid.NewGuid()){}
    public DocumentLink(Guid tenant,Guid condo,Guid document,DocumentTarget type,Guid target):base(tenant,condo)
    {DocumentId=Guard.Id(document);TargetType=Guard.EnumValue(type);TargetId=Guard.Id(target);}
}
public sealed class MeetingMinutes : MeetingEntity
{
    public int Version { get; private set; }
    public int Revision { get; private set; } = 1;
    public MinutesStatus Status { get; private set; }
    public Guid? PreviousMinutesId { get; private set; }
    public string? CorrectionReason { get; private set; }
    public string Title { get; private set; } = "";
    public string Introduction { get; private set; } = "";
    public string DiscussionNotes { get; private set; } = "";
    public string ClosingNotes { get; private set; } = "";
    public string SourceJson { get; private set; } = "";
    public string SourceFingerprint { get; private set; } = "";
    public Guid PreparedBy { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? DocumentId { get; private set; }
    private MeetingMinutes():base(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()){}
    public MeetingMinutes(Guid tenant,Guid condo,Guid meeting,int version,string title,string source,string fingerprint,Guid actor,Guid? previous=null,string? reason=null):base(tenant,condo,meeting)
    {if(version<1)throw new ArgumentException("Versione non valida.");Version=version;Title=Guard.Text(title);SourceJson=source;SourceFingerprint=Guard.Text(fingerprint,64);PreparedBy=Guard.Id(actor);PreviousMinutesId=previous;CorrectionReason=Guard.Optional(reason,2000);if(previous!=null&&CorrectionReason==null)throw new ArgumentException("Motivo rettifica obbligatorio.");}
    public void Edit(int revision,string title,string? introduction,string? discussion,string? closing)
    {if(Revision!=revision)throw new InvalidOperationException("Bozza modificata: ricaricare.");if(Status is MinutesStatus.Finalized or MinutesStatus.Superseded)throw new InvalidOperationException("Verbale definitivo immutabile.");Title=Guard.Text(title);Introduction=Guard.Optional(introduction,12000)??"";DiscussionNotes=Guard.Optional(discussion,60000)??"";ClosingNotes=Guard.Optional(closing,12000)??"";Status=MinutesStatus.Draft;Revision++;}
    public void Finalize(Guid document,Guid actor){if(Status is MinutesStatus.Finalized or MinutesStatus.Superseded)throw new InvalidOperationException("Verbale già consolidato.");DocumentId=Guard.Id(document);FinalizedBy=Guard.Id(actor);FinalizedAt=DateTimeOffset.UtcNow;Status=MinutesStatus.Finalized;Revision++;}
}
