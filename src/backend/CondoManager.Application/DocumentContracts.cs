using CondoManager.Domain;
namespace CondoManager.Application;
public record DocumentLinkInput(DocumentTarget Type,Guid Id);
public record DocumentDto(Guid Id,DocumentType Type,string Title,string? Description,DocumentStatus Status,int Version,Guid? PreviousDocumentId,string FileName,string ContentType,long SizeBytes,string Sha256,Guid CreatedBy,DateTimeOffset CreatedAt,DateTimeOffset FinalizedAt,string SourceFingerprint,IReadOnlyList<DocumentLinkInput> Links);
public record MinutesDto(Guid Id,Guid MeetingId,int Version,int Revision,MinutesStatus Status,Guid? PreviousMinutesId,string? CorrectionReason,string Title,string Introduction,string DiscussionNotes,string ClosingNotes,string SourceFingerprint,Guid? DocumentId,Guid PreparedBy,DateTimeOffset CreatedAt,Guid? FinalizedBy,DateTimeOffset? FinalizedAt);
public record MinutesDraftInput(string Title,Guid? PreviousMinutesId=null,string? CorrectionReason=null);
public record MinutesEditInput(int Revision,string Title,string? Introduction,string? DiscussionNotes,string? ClosingNotes);
public record MinutesFinalizeInput(int Revision,string SourceFingerprint,Guid RequestId);
public record MinutesValidation(bool CanFinalize,IReadOnlyList<string> Errors,IReadOnlyList<string> Warnings,string SourceFingerprint);
public record StoredFile(string Key,long SizeBytes,string Sha256);
public record PendingDocumentFile(Guid TenantId,string Key,DateTimeOffset CreatedAt);
public record DocumentFile(byte[] Bytes,string ContentType,string FileName);
public interface IDocumentStorage
{
 Task<StoredFile> Save(Guid tenant,ReadOnlyMemory<byte> bytes,CancellationToken ct);
 Task<byte[]> OpenRead(Guid tenant,string key,CancellationToken ct);
 Task<bool> Exists(Guid tenant,string key,CancellationToken ct);
 Task<StoredFile> GetMetadata(Guid tenant,string key,CancellationToken ct);
 // Only uncommitted staging keys can be deleted. Committed files are append-only.
 Task DeleteDraft(Guid tenant,string key,CancellationToken ct);
 Task<string> Commit(Guid tenant,string key,CancellationToken ct);
 Task Acknowledge(Guid tenant,string key,CancellationToken ct);
 Task DiscardUncommitted(Guid tenant,string key,CancellationToken ct);
 IEnumerable<PendingDocumentFile> Pending();
}
public interface IDocumentPdfGenerator { byte[] Generate(string title,IReadOnlyList<string> paragraphs,bool draft); }
public interface IDocumentScanner { Task Inspect(ReadOnlyMemory<byte> bytes,CancellationToken ct); }
public record MinutesSource(string CondominiumName,string Address,Guid CompletionId,DateTimeOffset CompletedAt,string CompletionFingerprint,string CompletionJson);
public interface IDocumentService
{
 Task<IReadOnlyList<DocumentDto>> List(Guid condo,string? search,DocumentType? type,DocumentTarget? targetType,Guid? target,CancellationToken ct);
 Task<DocumentDto> Details(Guid condo,Guid id,CancellationToken ct);
 Task<IReadOnlyList<DocumentDto>> Versions(Guid condo,Guid id,CancellationToken ct);
 Task<DocumentDto> Upload(Guid condo,string title,string? description,string name,string contentType,byte[] bytes,Guid request,IReadOnlyList<DocumentLinkInput> links,Guid? previous,CancellationToken ct);
 Task<DocumentFile> Download(Guid condo,Guid id,CancellationToken ct);
 Task<IReadOnlyList<MinutesDto>> Minutes(Guid condo,Guid meeting,CancellationToken ct);
 Task<MinutesDto> CreateDraft(Guid condo,Guid meeting,MinutesDraftInput input,CancellationToken ct);
 Task<MinutesDto> Edit(Guid condo,Guid meeting,Guid id,MinutesEditInput input,CancellationToken ct);
 Task<MinutesValidation> Validate(Guid condo,Guid meeting,Guid id,CancellationToken ct);
 Task<DocumentFile> Preview(Guid condo,Guid meeting,Guid id,CancellationToken ct);
 Task<MinutesDto> Finalize(Guid condo,Guid meeting,Guid id,MinutesFinalizeInput input,CancellationToken ct);
 Task<DocumentDto> Report(Guid condo,Guid meeting,DocumentType type,Guid? relatedId,Guid request,CancellationToken ct);
}
