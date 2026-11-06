using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
namespace CondoManager.Infrastructure;

public sealed class DocumentService(AppDbContext db,ICurrentTenant current,IDocumentStorage storage,IDocumentPdfGenerator pdf,IDocumentScanner scanner,IConfiguration configuration):IDocumentService,IAsyncDisposable
{
 readonly List<(Guid Tenant,string Key)> pending=[];
 public async ValueTask DisposeAsync()
 {
  foreach(var p in pending){try{if(await db.Documents.IgnoreQueryFilters().AsNoTracking().AnyAsync(d=>d.TenantId==p.Tenant&&d.StorageKey==p.Key))await storage.Acknowledge(p.Tenant,p.Key,default);else await storage.DiscardUncommitted(p.Tenant,p.Key,default);}catch{ /* Marker retained for explicit recovery after an ambiguous DB failure. No definitive file is deleted. */ }}
 }
 Guid Tenant=>current.RequireRead();Guid Actor=>current.UserId??throw new AccessDeniedException();
 static readonly CultureInfo It=CultureInfo.GetCultureInfo("it-IT");
 static string Json(object value)=>JsonSerializer.Serialize(value);
 static T Read<T>(string value)=>JsonSerializer.Deserialize<T>(value)??throw new InvalidOperationException("Snapshot non disponibile.");
 static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 void Operational(){current.RequireWrite();}
 async Task<Condominium> Condo(Guid id,CancellationToken ct)=>await db.Condominiums.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException();
 async Task Lock(Guid condo,CancellationToken ct){Operational();await Condo(condo,ct);await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"TenantId\"={Tenant} AND \"Id\"={condo} FOR UPDATE",ct);}
 void Audit(string action,Guid condo,Guid subject)=>db.Add(new SecurityAudit{TenantId=Tenant,ActorUserId=Actor,SubjectId=subject,Event="Documents."+action,Timestamp=DateTimeOffset.UtcNow});
 async Task<DocumentDto> Dto(CondominiumDocument d,CancellationToken ct)=>new(d.Id,d.Type,d.Title,d.Description,d.Status==DocumentStatus.Final&&await db.Documents.AnyAsync(x=>x.PreviousDocumentId==d.Id,ct)?DocumentStatus.Superseded:d.Status,d.Version,d.PreviousDocumentId,d.FileName,d.ContentType,d.SizeBytes,d.Sha256,d.CreatedBy,d.CreatedAt,d.FinalizedAt,d.SourceFingerprint,await db.DocumentLinks.Where(x=>x.DocumentId==d.Id).Select(x=>new DocumentLinkInput(x.TargetType,x.TargetId)).ToArrayAsync(ct));
 static MinutesDto Dto(MeetingMinutes m)=>new(m.Id,m.MeetingId,m.Version,m.Revision,m.Status,m.PreviousMinutesId,m.CorrectionReason,m.Title,m.Introduction,m.DiscussionNotes,m.ClosingNotes,m.SourceFingerprint,m.DocumentId,m.PreparedBy,m.CreatedAt,m.FinalizedBy,m.FinalizedAt);
 async Task<MeetingMinutes> FindMinutes(Guid condo,Guid meeting,Guid id,CancellationToken ct){Operational();await Condo(condo,ct);return await db.MinutesRows.SingleOrDefaultAsync(x=>x.Id==id&&x.MeetingId==meeting&&x.CondominiumId==condo,ct)??throw new NotFoundException();}
 public async Task<IReadOnlyList<DocumentDto>> List(Guid condo,string? search,DocumentType? type,DocumentTarget? targetType,Guid? target,CancellationToken ct)
 {
  Operational();await Condo(condo,ct);var q=db.Documents.Where(x=>x.CondominiumId==condo);if(type!=null)q=q.Where(x=>x.Type==type);if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim().ToLower();q=q.Where(x=>x.Title.ToLower().Contains(s));}
  if(target!=null){if(targetType==null)throw new ArgumentException("Specificare tipo collegamento.");q=q.Where(x=>db.DocumentLinks.Any(l=>l.DocumentId==x.Id&&l.TargetType==targetType&&l.TargetId==target));}
  var docs=await q.OrderByDescending(x=>x.CreatedAt).Take(250).ToListAsync(ct);var result=new List<DocumentDto>();foreach(var d in docs)result.Add(await Dto(d,ct));return result;
 }
 public async Task<DocumentDto> Details(Guid condo,Guid id,CancellationToken ct){Operational();await Condo(condo,ct);return await Dto(await db.Documents.SingleOrDefaultAsync(x=>x.Id==id&&x.CondominiumId==condo,ct)??throw new NotFoundException(),ct);}
 public async Task<IReadOnlyList<DocumentDto>> Versions(Guid condo,Guid id,CancellationToken ct)
 {
  var currentDoc=await Details(condo,id,ct);var chain=new List<DocumentDto>{currentDoc};var seen=new HashSet<Guid>{id};
  while(currentDoc.PreviousDocumentId is Guid prior){if(!seen.Add(prior))throw new InvalidOperationException("Catena documentale non valida.");currentDoc=await Details(condo,prior,ct);chain.Add(currentDoc);}
  var next=id;while(true){var d=await db.Documents.SingleOrDefaultAsync(x=>x.CondominiumId==condo&&x.PreviousDocumentId==next,ct);if(d==null)break;if(!seen.Add(d.Id))throw new InvalidOperationException("Catena documentale non valida.");chain.Add(await Dto(d,ct));next=d.Id;}
  return chain.OrderByDescending(x=>x.Version).ToArray();
 }
 public async Task<DocumentFile> Download(Guid condo,Guid id,CancellationToken ct)
 {
  var d=await Details(condo,id,ct);var row=await db.Documents.SingleAsync(x=>x.Id==id&&x.CondominiumId==condo,ct);
  if(!await storage.Exists(Tenant,row.StorageKey,ct))throw new InvalidOperationException("File non disponibile nello storage: ripristino necessario.");
  var bytes=await storage.OpenRead(Tenant,row.StorageKey,ct);if(bytes.LongLength!=d.SizeBytes||Convert.ToHexString(SHA256.HashData(bytes))!=d.Sha256)throw new InvalidOperationException("Integrità file non verificata: download bloccato.");
  Audit("Downloaded",condo,id);await db.SaveChangesAsync(ct);return new(bytes,d.ContentType,d.FileName);
 }
 async Task ValidateLinks(Guid condo,IReadOnlyList<DocumentLinkInput> links,CancellationToken ct)
 {
  if(links.Count>20||links.Distinct().Count()!=links.Count)throw new ArgumentException("Collegamenti duplicati o eccessivi.");
  foreach(var l in links){Guard.Id(l.Id);Guard.EnumValue(l.Type);var valid=l.Type switch{
   DocumentTarget.AccountingPeriod=>await db.AccountingPeriods.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.Meeting=>await db.Meetings.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.AgendaItem=>await db.MeetingAgendaItems.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.Notice=>await db.MeetingNotices.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.Proxy=>await db.MeetingProxies.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.Resolution=>await db.MeetingResolutions.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.Budget=>await db.Budgets.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),
   DocumentTarget.FinalStatement=>await db.FinalStatementRows.AnyAsync(x=>x.Id==l.Id&&x.CondominiumId==condo,ct),_=>false};if(!valid)throw new NotFoundException();}
 }
 public async Task<DocumentDto> Upload(Guid condo,string title,string? description,string name,string contentType,byte[] bytes,Guid request,IReadOnlyList<DocumentLinkInput> links,Guid? previous,CancellationToken ct)
 {
  Operational();Guard.Text(title);Guard.Id(request);ValidateUpload(name,contentType,bytes,configuration.GetValue<int?>("Documents:MaxUploadBytes")??8388608);
  var clean=SafeName(name);await scanner.Inspect(bytes,ct);
  await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock(condo,ct);await ValidateLinks(condo,links,ct);
  var hash=Hash(Json(new{title,description,clean,contentType,Bytes=Convert.ToHexString(SHA256.HashData(bytes)),links,previous}));
  var existing=await Existing(condo,request,hash,ct);if(existing!=null){await tx.CommitAsync(ct);return existing;}
  CondominiumDocument? prior=null;if(previous!=null){prior=await db.Documents.SingleOrDefaultAsync(x=>x.Id==previous&&x.CondominiumId==condo&&x.Type==DocumentType.AdministrativeAttachment,ct)??throw new NotFoundException();if(await db.Documents.AnyAsync(x=>x.PreviousDocumentId==previous,ct))throw new InvalidOperationException("Selezionare l'ultima versione dell'allegato.");if(string.IsNullOrWhiteSpace(description))throw new ArgumentException("Motivazione della nuova versione obbligatoria nella descrizione.");}
  var d=await Store(condo,DocumentType.AdministrativeAttachment,title,description,clean,contentType,bytes,Hash(Json(links)),request,hash,links,ct,(prior?.Version??0)+1,prior?.Id);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return await Dto(d,ct);
 }
 static string SafeName(string name){var leaf=name.Replace('\\','/').Split('/').Last();var safe=new string(leaf.Where(c=>char.IsLetterOrDigit(c)||c is '.' or '-' or '_' or ' ').ToArray());if(string.IsNullOrWhiteSpace(safe)||safe.Length>150)throw new ArgumentException("Nome file non valido.");return safe;}
 public static void ValidateUpload(string name,string mime,byte[] b,int limit)
 {
  if(b.Length==0||b.Length>limit)throw new ArgumentException("Dimensione allegato non consentita.");var ext=Path.GetExtension(name).ToLowerInvariant();
  if(ext==".pdf"&&mime=="application/pdf"&&b.AsSpan().StartsWith("%PDF-"u8)){try{using var d=PdfDocument.Open(b);if(d.NumberOfPages<1||d.NumberOfPages>2000)throw new ArgumentException("PDF non valido.");}catch(Exception e) when(e is not ArgumentException){throw new ArgumentException("PDF non valido.");}return;}
  if(ext==".png"&&mime=="image/png"&&b.Length>=45&&b.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})&&Encoding.ASCII.GetString(b,12,4)=="IHDR"&&Encoding.ASCII.GetString(b,b.Length-8,4)=="IEND"){
   var width=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(16,4));var height=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(20,4));if(width==0||height==0||(ulong)width*height>25000000)throw new ArgumentException("Dimensioni immagine non consentite.");
   try{var builder=new UglyToad.PdfPig.Writer.PdfDocumentBuilder();builder.AddPage(100,100).AddPng(b,new UglyToad.PdfPig.Core.PdfRectangle(0,0,100,100));}catch{throw new ArgumentException("Immagine PNG non valida.");}return;
  }
  if((ext==".jpg"||ext==".jpeg")&&mime=="image/jpeg"&&b.Length>10&&b[0]==255&&b[1]==216&&b[2]==255&&b[^2]==255&&b[^1]==217){try{var builder=new UglyToad.PdfPig.Writer.PdfDocumentBuilder();builder.AddPage(100,100).AddJpeg(b,new UglyToad.PdfPig.Core.PdfRectangle(0,0,100,100));}catch{throw new ArgumentException("Immagine JPEG non valida.");}return;}
  throw new ArgumentException("Formato reale, estensione e MIME non coerenti. Ammessi PDF, JPEG e PNG.");
 }
 async Task<DocumentDto?> Existing(Guid condo,Guid request,string hash,CancellationToken ct){Guard.Id(request);var d=await db.Documents.SingleOrDefaultAsync(x=>x.CondominiumId==condo&&x.RequestId==request,ct);if(d==null)return null;if(d.RequestHash!=hash)throw new InvalidOperationException("Chiave idempotenza già usata con contenuto differente.");if(!await storage.Exists(Tenant,d.StorageKey,ct))throw new InvalidOperationException("File archiviato mancante.");return await Dto(d,ct);}
 async Task<CondominiumDocument> Store(Guid condo,DocumentType type,string title,string? description,string name,string mime,byte[] bytes,string source,Guid request,string hash,IReadOnlyList<DocumentLinkInput> links,CancellationToken ct,int version=1,Guid? previous=null)
 {
  var staged=await storage.Save(Tenant,bytes,ct);pending.Add((Tenant,staged.Key));
  try{
   if(staged.SizeBytes!=bytes.LongLength||staged.Sha256!=Convert.ToHexString(SHA256.HashData(bytes)))throw new IOException("Storage non integro.");
   var key=await storage.Commit(Tenant,staged.Key,ct);var actual=await storage.GetMetadata(Tenant,key,ct);if(actual!=staged)throw new IOException("Finalizzazione storage non integra.");
   var d=new CondominiumDocument(Tenant,condo,type,title,description,name,mime,actual.SizeBytes,actual.Sha256,key,Actor,source,request,hash,version,previous);db.Add(d);foreach(var l in links)db.Add(new DocumentLink(Tenant,condo,d.Id,l.Type,l.Id));Audit("Archived",condo,d.Id);return d;
  }finally{await storage.DeleteDraft(Tenant,staged.Key,CancellationToken.None);}
 }
 public async Task<IReadOnlyList<MinutesDto>> Minutes(Guid condo,Guid meeting,CancellationToken ct){Operational();await Condo(condo,ct);if(!await db.Meetings.AnyAsync(m=>m.Id==meeting&&m.CondominiumId==condo,ct))throw new NotFoundException();var rows=await db.MinutesRows.Where(x=>x.MeetingId==meeting&&x.CondominiumId==condo).OrderByDescending(x=>x.Version).ToListAsync(ct);var latest=rows.Where(x=>x.Status==MinutesStatus.Finalized).Select(x=>x.Version).DefaultIfEmpty(0).Max();return rows.Select(x=>Dto(x) with{Status=x.Status==MinutesStatus.Finalized&&x.Version<latest?MinutesStatus.Superseded:x.Status}).ToArray();}
 async Task<MinutesSource> Source(Guid condo,Guid meeting,CancellationToken ct)
 {
  var c=await Condo(condo,ct);var m=await db.Meetings.SingleOrDefaultAsync(m=>m.Id==meeting&&m.CondominiumId==condo,ct)??throw new NotFoundException();if(m.Status!=MeetingStatus.Completed)throw new InvalidOperationException("Concludere l'assemblea prima di preparare il verbale.");
  var completion=await db.MeetingCompletions.SingleOrDefaultAsync(x=>x.MeetingId==meeting&&x.CondominiumId==condo,ct)??throw new InvalidOperationException("Snapshot conclusivo mancante.");
  return new(c.Name,string.Join(" ",c.Address,c.PostalCode,c.City,c.Province,c.Country),completion.Id,completion.CreatedAt,completion.Fingerprint,completion.ContentJson);
 }
 public async Task<MinutesDto> CreateDraft(Guid condo,Guid meeting,MinutesDraftInput i,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock(condo,ct);var source=await Source(condo,meeting,ct);var last=await db.MinutesRows.Where(x=>x.CondominiumId==condo&&x.MeetingId==meeting).OrderByDescending(x=>x.Version).FirstOrDefaultAsync(ct);
  if(last!=null&&(last.Status!=MinutesStatus.Finalized||last.Id!=i.PreviousMinutesId))throw new InvalidOperationException("Modificare la bozza corrente oppure indicare il verbale definitivo corrente da rettificare.");
  if(last==null&&i.PreviousMinutesId!=null)throw new NotFoundException();var json=Json(last==null?source:Read<MinutesSource>(last.SourceJson));var m=new MeetingMinutes(Tenant,condo,meeting,(last?.Version??0)+1,i.Title,json,Hash(json),Actor,last?.Id,i.CorrectionReason);db.Add(m);Audit(last==null?"MinutesDraft":"MinutesCorrection",condo,m.Id);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Dto(m);
 }
 public async Task<MinutesDto> Edit(Guid condo,Guid meeting,Guid id,MinutesEditInput i,CancellationToken ct)
 {
  await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock(condo,ct);var m=await FindMinutes(condo,meeting,id,ct);m.Edit(i.Revision,i.Title,i.Introduction,i.DiscussionNotes,i.ClosingNotes);Audit("MinutesEdited",condo,m.Id);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Dto(m);
 }
 public async Task<MinutesValidation> Validate(Guid condo,Guid meeting,Guid id,CancellationToken ct)
 {
  var m=await FindMinutes(condo,meeting,id,ct);var s=Read<MinutesSource>(m.SourceJson);var now=await Source(condo,meeting,ct);var errors=new List<string>();
  if(now.CompletionId!=s.CompletionId||now.CompletionFingerprint!=s.CompletionFingerprint||Hash(now.CompletionJson)!=Hash(s.CompletionJson)||Hash(Json(Read<MinutesSource>(m.SourceJson)))!=m.SourceFingerprint)errors.Add("Fonti assembleari modificate: impossibile consolidare.");
  using var doc=JsonDocument.Parse(s.CompletionJson);var details=Read<MeetingDetails>(doc.RootElement.GetProperty("Meeting").GetRawText());var decisions=Read<DecisionsDto>(doc.RootElement.GetProperty("Decisions").GetRawText());
  if(details.Snapshots.Count==0||decisions.Votes.Any(v=>v.Status is MeetingVoteStatus.Open or MeetingVoteStatus.Closed))errors.Add("Snapshot assenti o votazioni non consolidate.");
  return new(errors.Count==0,errors,["Documento non firmato. Presidente e segretario non designati nel modello: campi lasciati vuoti.","Archiviazione applicativa: non conservazione digitale a norma.","Intestazione congelata alla preparazione della prima bozza."],m.SourceFingerprint);
 }
 public async Task<DocumentFile> Preview(Guid condo,Guid meeting,Guid id,CancellationToken ct){var m=await FindMinutes(condo,meeting,id,ct);var b=Render(m,true);Audit("MinutesPreview",condo,id);await db.SaveChangesAsync(ct);return new(b,"application/pdf","bozza-verbale.pdf");}
 public async Task<MinutesDto> Finalize(Guid condo,Guid meeting,Guid id,MinutesFinalizeInput i,CancellationToken ct)
 {
  current.RequireAdmin();await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock(condo,ct);var m=await FindMinutes(condo,meeting,id,ct);
  var requestHash=Hash(Json(new{Minutes=id,i.Revision,i.SourceFingerprint}));var existing=await Existing(condo,i.RequestId,requestHash,ct);if(existing!=null){if(m.DocumentId!=existing.Id)throw new InvalidOperationException("Richiesta non coerente.");await tx.CommitAsync(ct);return Dto(m);}
  if(m.Revision!=i.Revision||m.SourceFingerprint!=i.SourceFingerprint)throw new InvalidOperationException("Bozza o fonti modificate: ricaricare.");if(m.Status==MinutesStatus.Finalized)throw new InvalidOperationException("Verbale già definitivo.");var validation=await Validate(condo,meeting,id,ct);if(!validation.CanFinalize)throw new InvalidOperationException(string.Join(" ",validation.Errors));
  Guid? previousDoc=null;if(m.PreviousMinutesId!=null)previousDoc=(await db.MinutesRows.SingleAsync(x=>x.Id==m.PreviousMinutesId&&x.MeetingId==meeting,ct)).DocumentId;
  var source=Read<MinutesSource>(m.SourceJson);using var doc=JsonDocument.Parse(source.CompletionJson);var detail=Read<MeetingDetails>(doc.RootElement.GetProperty("Meeting").GetRawText());var links=new List<DocumentLinkInput>{new(DocumentTarget.Meeting,meeting)};if(detail.Meeting.AccountingPeriodId is Guid p)links.Add(new(DocumentTarget.AccountingPeriod,p));
  var d=await Store(condo,DocumentType.MeetingMinutes,m.Title,m.CorrectionReason,$"verbale-v{m.Version}.pdf","application/pdf",Render(m,false),m.SourceFingerprint,i.RequestId,requestHash,links,ct,m.Version,previousDoc);m.Finalize(d.Id,Actor);Audit("MinutesFinalized",condo,m.Id);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Dto(m);
 }
 static string Threshold(ThresholdCheck t)=>$"{t.Actual.ToString("N6",It)} {(t.Comparison==ThresholdComparison.GreaterThan?">":">=")} {t.Numerator}/{t.Denominator} di {t.Basis.ToString("N6",It)}";
 static string Time(DateTimeOffset? at)=>at==null?"non registrato":TimeZoneInfo.ConvertTime(at.Value,TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")).ToString("dd/MM/yyyy HH:mm zzz",It);
 byte[] Render(MeetingMinutes m,bool draft)=>pdf.Generate(m.Title,Paragraphs(Read<MinutesSource>(m.SourceJson),m),draft);
 static List<string> Paragraphs(MinutesSource source,MeetingMinutes? minutes)
 {
  using var json=JsonDocument.Parse(source.CompletionJson);var details=Read<MeetingDetails>(json.RootElement.GetProperty("Meeting").GetRawText());var decisions=Read<DecisionsDto>(json.RootElement.GetProperty("Decisions").GetRawText());var m=details.Meeting;
  var list=new List<string>{source.CondominiumName,source.Address,$"Assemblea {m.Type} - {m.Title}","Luogo: "+m.Location,"Orario apertura: non registrato nel modello sessione; vedere ingressi e verifiche datate.","Conclusione registrata: "+Time(source.CompletedAt),$"Snapshot conclusivo: {source.CompletionId} / {source.CompletionFingerprint}","Presidente: __________________   Segretario: __________________","Nessuna firma acquisita. Nessuna attestazione automatica di conformità legale."};
  var review=Read<CompletionReview>(json.RootElement.GetProperty("Review").GetRawText());
  foreach(var session in m.Sessions)list.Add($"Convocazione {(session.Call==MeetingCall.First?"prima":"seconda")}: {Time(session.StartsAt)} - {(session.Id==review.SessionId?"sessione conclusa":"stato allo snapshot: "+session.Status)}");
  list.Add("INTRODUZIONE\n"+minutes?.Introduction);list.Add("COSTITUZIONE E QUORUM");
  foreach(var q in decisions.Quorums){var e=q.Evaluation;list.Add($"Verifica {Time(q.At)}; sessione {q.SessionId}; criterio {q.RuleId}; esito {q.Outcome}. {e.Reason}\nTeste presenti {e.Totals.Present} / aventi diritto {e.Totals.Eligible}; millesimi rappresentati {e.Totals.PresentValue.ToString("N6",It)} / {q.Electorate.BuildingValue.ToString("N6",It)}\nSoglie: teste {Threshold(e.Heads)}; valore {Threshold(e.Value)}");foreach(var anomaly in e.Anomalies)list.Add("Anomalia: "+anomaly);}
  var people=details.Snapshots.SelectMany(x=>x.Participants.People).GroupBy(x=>x.Id).ToDictionary(x=>x.Key,x=>x.Last().Name);string Name(Guid id)=>people.GetValueOrDefault(id,"Soggetto snapshot "+id);
  var units=details.Snapshots.SelectMany(x=>x.Participants.Units).GroupBy(u=>u.Id).ToDictionary(g=>g.Key,g=>g.Last());string Unit(Guid id)=>units.TryGetValue(id,out var u)?u.Code:id.ToString();
  list.Add("AVENTI DIRITTO POTENZIALI - SNAPSHOT ANAGRAFICO");foreach(var u in units.Values)list.Add($"Unità {u.Code}; millesimi {u.Millesimals?.ToString("N6",It)??"non determinati"}; titolari {string.Join(", ",u.Holdings.Select(h=>Name(h.PersonId)).Distinct())}");
  list.Add("PRESENZE E RAPPRESENTANZE");foreach(var a in details.Attendances)list.Add($"{Name(a.PersonId)}; modalità {a.Type}; rappresentato da {(a.RepresentedById is Guid by?Name(by):"direttamente")}; ingresso {Time(a.CheckInAt)}; uscita {Time(a.CheckOutAt)}; sessione {a.SessionId}; unità {string.Join(", ",a.PropertyUnitIds.Select(Unit))}; millesimi {a.Millesimals.ToString("N6",It)}");
  foreach(var p in details.Proxies)list.Add($"Delega: {p.DelegatorName} -> {p.DelegateName}; stato {p.Status}; unità {string.Join(", ",p.PropertyUnitIds.Select(Unit))}; verifica {Time(p.VerifiedAt)}");
  list.Add("ORDINE DEL GIORNO");foreach(var a in m.Agenda.OrderBy(a=>a.Position))list.Add($"{a.Position}. {a.Title}\n{a.Description}\nRiferimento: {a.RelatedType} {a.RelatedId}");
  list.Add("DISCUSSIONE\n"+minutes?.DiscussionNotes);list.Add("VOTAZIONI");
  foreach(var v in decisions.Votes){var t=v.Evaluation.Totals;list.Add($"Punto {m.Agenda.Single(a=>a.Id==v.AgendaItemId).Title}; apertura {Time(v.OpenedAt)}; chiusura {Time(v.ClosedAt)}\nCriterio versione {v.RuleVersion}: {v.Evidence.Criteria.LegalReference}; ambito {v.Evidence.Criteria.Scope}; teste {v.Evidence.Criteria.Heads.Comparison} {v.Evidence.Criteria.Heads.Numerator}/{v.Evidence.Criteria.Heads.Denominator} su {v.Evidence.Criteria.Heads.Basis}; millesimi {v.Evidence.Criteria.Value.Comparison} {v.Evidence.Criteria.Value.Numerator}/{v.Evidence.Criteria.Value.Denominator} su {v.Evidence.Criteria.Value.Basis}\nFavorevoli {t.InFavor} ({t.InFavorValue.ToString("N6",It)}); contrari {t.Against} ({t.AgainstValue.ToString("N6",It)}); astenuti {t.Abstained} ({t.AbstainedValue.ToString("N6",It)}); non votanti {t.NonVoting}. Esito {v.Evaluation.Outcome}: {v.Evaluation.Reason}");if(v.Evidence.Target is{} target){list.Add($"Oggetto M06: {target.Type} {target.Id}, versione {target.Version}. Fonte conservata nello snapshot documentale.");using var targetJson=JsonDocument.Parse(target.ContentJson);if(targetJson.RootElement.TryGetProperty("Total",out var total)&&total.TryGetDecimal(out var amount))list.Add("Importo EUR: "+amount.ToString("N2",It));}}
  list.Add("DELIBERE CONSOLIDATE - FONTE M07.2");foreach(var r in decisions.Resolutions)list.Add($"{r.Title} - versione {r.Version} - {r.Outcome}\n{r.ResolutionText}\nConsolidamento {Time(r.FinalizedAt)}; precedente {r.PreviousResolutionId}; rettifica: {r.CorrectionReason}");
  foreach(var a in decisions.Applications)list.Add($"Applicazione M06: delibera {a.ResolutionId} -> {a.RelatedType} {a.RelatedId}; storico {a.HistoricalOnly}; {Time(a.AppliedAt)}");
  foreach(var d in decisions.Dispositions)list.Add($"Punto {d.AgendaItemId}: {d.Disposition}. {d.Reason}");
  list.Add("CHIUSURA\n"+minutes?.ClosingNotes);if(minutes?.PreviousMinutesId!=null)list.Add($"Rettifica testuale della versione {minutes.PreviousMinutesId}: {minutes.CorrectionReason}. Esiti e delibere originali invariati.");return list;
 }
 public async Task<DocumentDto> Report(Guid condo,Guid meeting,DocumentType type,Guid? relatedId,Guid request,CancellationToken ct)
 {
  Operational();if(type is not(DocumentType.MeetingAttendance or DocumentType.MeetingVoting or DocumentType.MeetingResolution or DocumentType.MeetingNotice))throw new ArgumentException("Prospetto non disponibile.");
  await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock(condo,ct);var hash=Hash(Json(new{meeting,type,relatedId}));var existing=await Existing(condo,request,hash,ct);if(existing!=null){await tx.CommitAsync(ct);return existing;}
  var links=new List<DocumentLinkInput>{new(DocumentTarget.Meeting,meeting)};List<string> paragraphs;string fingerprint;
  if(type==DocumentType.MeetingNotice){var n=await db.MeetingNotices.SingleOrDefaultAsync(n=>n.Id==relatedId&&n.MeetingId==meeting&&n.CondominiumId==condo,ct)??throw new NotFoundException();var m=await db.Meetings.SingleAsync(x=>x.Id==meeting&&x.CondominiumId==condo,ct);var c=await Condo(condo,ct);var agenda=await db.MeetingAgendaItems.Where(a=>a.MeetingId==meeting).OrderBy(a=>a.Position).ToListAsync(ct);var sessions=await db.MeetingSessions.Where(s=>s.MeetingId==meeting).OrderBy(s=>s.Call).ToListAsync(ct);paragraphs=[c.Name,m.Title,"Destinatario: "+n.RecipientName,"Recapito: "+n.Contact,"Canale: "+n.Channel,"Luogo: "+m.Location,"Stato dichiarato: "+n.Status,"Questo PDF non attesta invio o consegna verificati."];paragraphs.AddRange(sessions.Select(s=>$"Convocazione {s.Call}: {Time(s.StartsAt)}"));paragraphs.AddRange(agenda.Select(a=>$"{a.Position}. {a.Title}\n{a.Description}"));links.Add(new(DocumentTarget.Notice,n.Id));fingerprint=Hash(Json(paragraphs));}
  else{var source=await Source(condo,meeting,ct);fingerprint=Hash(Json(source));paragraphs=Paragraphs(source,null);if(type==DocumentType.MeetingResolution){using var json=JsonDocument.Parse(source.CompletionJson);var dec=Read<DecisionsDto>(json.RootElement.GetProperty("Decisions").GetRawText());var r=dec.Resolutions.SingleOrDefault(r=>r.Id==relatedId)??throw new NotFoundException();paragraphs=[source.CondominiumName,"Assemblea: "+meeting,r.Title,r.ResolutionText,"Esito: "+r.Outcome,"Consolidamento: "+Time(r.FinalizedAt),"Fonte: "+r.ContentJson];links.Add(new(DocumentTarget.Resolution,r.Id));}}
  var previous=await db.Documents.Where(x=>x.CondominiumId==condo&&x.Type==type&&db.DocumentLinks.Any(l=>l.DocumentId==x.Id&&l.TargetType==(relatedId==null?DocumentTarget.Meeting:type==DocumentType.MeetingNotice?DocumentTarget.Notice:DocumentTarget.Resolution)&&l.TargetId==(relatedId??meeting))).OrderByDescending(x=>x.CreatedAt).FirstOrDefaultAsync(ct);
  var title=type.ToString();var d=await Store(condo,type,title,null,title+".pdf","application/pdf",pdf.Generate(title,paragraphs,false),fingerprint,request,hash,links,ct,(previous?.Version??0)+1,previous?.Id);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return await Dto(d,ct);
 }
}






