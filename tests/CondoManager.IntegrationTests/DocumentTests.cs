using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.Extensions.Configuration;
using UglyToad.PdfPig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
namespace CondoManager.IntegrationTests;

public class DocumentFileTests
{
 [Fact] public void ItalianUnicodeLongTextPaginationAndDraftAreExtractable()
 {
  var p=new DocumentPdfGenerator();var text="Delibera: proprietà, più, città, unità, perché; € 106.733,48; millesimi 120,000000.";
  var bytes=p.Generate("Verbale sintetico",Enumerable.Range(0,160).Select(n=>$"Riga {n}: {text}").ToArray(),true);
  Assert.True(bytes.AsSpan().StartsWith("%PDF-"u8));using var doc=PdfDocument.Open(bytes);Assert.True(doc.NumberOfPages>3);
  var all=string.Join(" ",doc.GetPages().Select(p=>p.Text));Assert.Contains("BOZZA",all);Assert.Contains("proprietà",all);Assert.Contains("106.733,48",all);Assert.Contains("Riga 159",all);Assert.Contains($"Pagina {doc.NumberOfPages} / {doc.NumberOfPages}",all);Assert.DoesNotContain("Mario Rossi",all);
 }
 [Theory][InlineData("x.html","text/html")][InlineData("x.pdf","image/png")][InlineData("x.exe","application/pdf")]
 public void UploadRejectsSpoofedFormats(string name,string type)=>Assert.Throws<ArgumentException>(()=>DocumentService.ValidateUpload(name,type,"%PDF-fake"u8.ToArray(),100));
 [Fact] public void UploadChecksActualPdfAndSize(){var b=new DocumentPdfGenerator().Generate("Synthetic",["Test"],false);DocumentService.ValidateUpload("a.pdf","application/pdf",b,b.Length);Assert.Throws<ArgumentException>(()=>DocumentService.ValidateUpload("a.pdf","application/pdf",b,b.Length-1));Assert.Throws<ArgumentException>(()=>DocumentService.ValidateUpload("a.pdf","application/pdf",[],100));}
 [Fact] public void PdfUsesAvailableWidthWithoutClippingLongUnbrokenText()
 {
  var b=new DocumentPdfGenerator().Generate("Verbale assemblea sintetica",[new string('W',800)],false);using var pdf=PdfDocument.Open(b);var p=pdf.GetPage(1);Assert.Single(p.Letters.Where(l=>l.FontSize==14).Select(l=>l.StartBaseLine.Y).Distinct());
  Assert.All(pdf.GetPages().SelectMany(p=>p.Letters),l=>{Assert.InRange(l.BoundingBox.Right,0,554);Assert.InRange(l.BoundingBox.Left,40,554);Assert.InRange(l.BoundingBox.Bottom,20,820);});
 }
 [Fact] public async Task StorageStagesCommitsChecksHashAndRejectsTraversal()
 {
  var root=Path.Combine(Directory.GetCurrentDirectory(),".artifacts","storage-tests",Guid.NewGuid().ToString("N"));var storage=new LocalDocumentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Documents:RootPath",root}}).Build());var tenant=Guid.NewGuid();var bytes="Synthetic integrity"u8.ToArray();
  var staged=await storage.Save(tenant,bytes,default);Assert.False(await storage.Exists(tenant,staged.Key,default));Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)),staged.Sha256);await storage.Commit(tenant,staged.Key,default);await storage.Commit(tenant,staged.Key,default);Assert.Equal(staged,await storage.GetMetadata(tenant,staged.Key,default));await storage.DeleteDraft(tenant,staged.Key,default);await storage.DeleteDraft(tenant,staged.Key,default);Assert.Equal(bytes,await storage.OpenRead(tenant,staged.Key,default));Assert.False(await storage.Exists(Guid.NewGuid(),staged.Key,default));await Assert.ThrowsAsync<ArgumentException>(()=>storage.OpenRead(tenant,"../escape",default));
  await storage.Acknowledge(tenant,staged.Key,default);await storage.DiscardUncommitted(tenant,staged.Key,default);Assert.True(await storage.Exists(tenant,staged.Key,default));
  var orphan=await storage.Save(tenant,bytes,default);await storage.Commit(tenant,orphan.Key,default);await storage.DiscardUncommitted(tenant,orphan.Key,default);await storage.DiscardUncommitted(tenant,orphan.Key,default);Assert.False(await storage.Exists(tenant,orphan.Key,default));
 }
}
[Collection("PostgreSQL")]
public class DocumentTests
{
 sealed class FailingStorage(IDocumentStorage inner):IDocumentStorage
 {
  public Task<StoredFile> Save(Guid t,ReadOnlyMemory<byte> b,CancellationToken c)=>inner.Save(t,b,c);
  public Task<byte[]> OpenRead(Guid t,string k,CancellationToken c)=>inner.OpenRead(t,k,c);
  public Task<bool> Exists(Guid t,string k,CancellationToken c)=>inner.Exists(t,k,c);
  public Task<StoredFile> GetMetadata(Guid t,string k,CancellationToken c)=>inner.GetMetadata(t,k,c);
  public Task DeleteDraft(Guid t,string k,CancellationToken c)=>inner.DeleteDraft(t,k,c);
  public Task<string> Commit(Guid t,string k,CancellationToken c)=>throw new IOException("Synthetic storage failure");
  public Task Acknowledge(Guid t,string k,CancellationToken c)=>inner.Acknowledge(t,k,c);
  public Task DiscardUncommitted(Guid t,string k,CancellationToken c)=>inner.DiscardUncommitted(t,k,c);
  public IEnumerable<PendingDocumentFile> Pending()=>inner.Pending();
 }
 static async Task<T> Send<T>(HttpClient c,string path,object body,string method="POST"){using var r=await c.SendAsync(new(new HttpMethod(method),path){Content=JsonContent.Create(body,options:SecurityTestHost.Json)});Assert.True(r.IsSuccessStatusCode,$"{path}: {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return(await r.Content.ReadFromJsonAsync<T>(SecurityTestHost.Json))!;}
 static async Task<VotingTests.Fixture> Completed(bool proxy=false)
 {
  var f=await VotingTests.Setup();
  if(proxy){var x=f.F.Data.People[0];var y=f.F.Data.People[1];var u=f.F.Data.Units[0];var a=f.F.D.Attendances.Single(a=>a.PersonId==x);await f.F.Change($"/attendances/{a.Id}/exit",new AttendanceExit(f.V,DateTimeOffset.UtcNow));await f.F.Change("/proxies",new ProxyInput(f.V,x,y,[u],DateTimeOffset.UtcNow,"Synthetic proxy","Synthetic verified written reference"));var p=f.F.D.Proxies.Single();await f.F.Change($"/proxies/{p.Id}/status",new ProxyTransition(f.V,ProxyStatus.Submitted));await f.F.Change($"/proxies/{p.Id}/status",new ProxyTransition(f.V,ProxyStatus.Verified));await f.F.Change("/attendances",new AttendanceInput(f.V,f.Session,x,AttendanceType.ByProxy,p.Id,[u],DateTimeOffset.UtcNow,null));}
  await f.Check();await f.Act("/votes",f.Open());await f.VoteAll();await f.FinalizeVote();await f.ExitAll();var review=(await f.C.GetFromJsonAsync<CompletionReview>(f.Path+"/completion-review?session="+f.Session,SecurityTestHost.Json))!;await f.Act("/complete",new CompleteMeetingInput(f.V,Guid.NewGuid(),f.Session,review.Fingerprint,"Synthetic completion"));return f;
 }
 static string Base(VotingTests.Fixture f)=>$"/api/documents/condominiums/{f.F.Data.Base.Condo}";
 static string Minutes(VotingTests.Fixture f)=>Base(f)+"/meetings/"+f.F.D.Meeting.Id+"/minutes";
 [PostgresFact] public async Task DraftPreviewFinalizeRetryCorrectionAndOriginalPdfAreImmutable()
 {
  await using var f=await Completed();var path=Minutes(f);var draft=await Send<MinutesDto>(f.C,path,new MinutesDraftInput("Verbale sintetico città"));Assert.Equal(MinutesStatus.Draft,draft.Status);
  draft=await Send<MinutesDto>(f.C,path+"/"+draft.Id,new MinutesEditInput(draft.Revision,draft.Title,"Introduzione sintetica","Discussione: approvazione con caratteri accentati.","Chiusura sintetica"),"PUT");
  var preview=await f.C.GetByteArrayAsync(path+"/"+draft.Id+"/preview");using(var p=PdfDocument.Open(preview)){var text=string.Join("",p.GetPages().Select(p=>p.Text));Directory.CreateDirectory(".artifacts");await File.WriteAllTextAsync(".artifacts/minutes-extracted.txt",text);Assert.Contains("BOZZA",text);Assert.Contains("Syntheticapprovedtext",string.Concat(text.Where(c=>!char.IsWhiteSpace(c))));Assert.Contains("millesimi",text);}
  var check=(await f.C.GetFromJsonAsync<MinutesValidation>(path+"/"+draft.Id+"/validation",SecurityTestHost.Json))!;Assert.True(check.CanFinalize,string.Join(";",check.Errors));
  var input=new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid());var final=await Send<MinutesDto>(f.C,path+"/"+draft.Id+"/finalize",input);var retry=await Send<MinutesDto>(f.C,path+"/"+draft.Id+"/finalize",input);Assert.Equal(final.DocumentId,retry.DocumentId);
  var doc=(await f.C.GetFromJsonAsync<DocumentDto>(Base(f)+"/"+final.DocumentId,SecurityTestHost.Json))!;var bytes=await f.C.GetByteArrayAsync(Base(f)+"/"+doc.Id+"/download");Assert.Equal(doc.Sha256,Convert.ToHexString(SHA256.HashData(bytes)));Assert.Equal(bytes.Length,doc.SizeBytes);using(var p=PdfDocument.Open(bytes))Assert.DoesNotContain("BOZZA",string.Join("",p.GetPages().Select(x=>x.Text)));
  using(var r=await f.C.PutAsJsonAsync(path+"/"+draft.Id,new MinutesEditInput(final.Revision,"Changed",null,null,null),SecurityTestHost.Json))Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
  var correction=await Send<MinutesDto>(f.C,path,new MinutesDraftInput("Verbale rettificato",draft.Id,"Correzione testuale sintetica"));Assert.Equal(2,correction.Version);Assert.Equal(draft.Id,correction.PreviousMinutesId);var cfinal=await Send<MinutesDto>(f.C,path+"/"+correction.Id+"/finalize",new MinutesFinalizeInput(correction.Revision,correction.SourceFingerprint,Guid.NewGuid()));Assert.NotEqual(final.DocumentId,cfinal.DocumentId);Assert.Equal(bytes,await f.C.GetByteArrayAsync(Base(f)+"/"+doc.Id+"/download"));
  var decisions=(await f.C.GetFromJsonAsync<DecisionsDto>(f.Path+"/decisions",SecurityTestHost.Json))!;Assert.Single(decisions.Resolutions);Assert.Equal("Synthetic approved text",decisions.Resolutions[0].ResolutionText);
  var search=(await f.C.GetFromJsonAsync<DocumentDto[]>(Base(f)+"?search=rettificato&type=MeetingMinutes",SecurityTestHost.Json))!;Assert.Single(search);
 }
 [PostgresFact] public async Task OpenMeetingDraftAndCrossCondominiumAreRejected()
 {
  await using var f=await VotingTests.Setup();using(var r=await f.C.PostAsJsonAsync(Minutes(f),new MinutesDraftInput("Too early"),SecurityTestHost.Json))Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
  using(var r=await f.C.GetAsync($"/api/documents/condominiums/{Guid.NewGuid()}"))Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
 }
 [PostgresFact] public async Task ReportsAndUploadsUseAuthorizedArchiveAndIdempotency()
 {
  await using var f=await Completed();foreach(var type in new[]{DocumentType.MeetingAttendance,DocumentType.MeetingVoting,DocumentType.MeetingResolution,DocumentType.MeetingNotice}){
   Guid? related=type==DocumentType.MeetingResolution?f.D.Resolutions[0].Id:type==DocumentType.MeetingNotice?f.F.D.Notices[0].Id:null;var input=new{Type=type,RelatedId=related,RequestId=Guid.NewGuid()};var d=await Send<DocumentDto>(f.C,Base(f)+"/meetings/"+f.F.D.Meeting.Id+"/reports",input);var retry=await Send<DocumentDto>(f.C,Base(f)+"/meetings/"+f.F.D.Meeting.Id+"/reports",input);Assert.Equal(d.Id,retry.Id);using var pdf=PdfDocument.Open(await f.C.GetByteArrayAsync(Base(f)+"/"+d.Id+"/download"));Assert.True(pdf.NumberOfPages>0);
  }
  using var anonymous=f.F.Data.Base.Host.Browser();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Base(f))).StatusCode);
 }
 [PostgresFact] public async Task ArchiveEnforcesRolesCsrfTenantAndAuditsDownloads()
 {
  await using var f=await Completed();var host=f.F.Data.Base.Host;var condo=f.F.Data.Base.Condo;var tenant=f.F.Data.Base.Tenant;
  var draft=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("Synthetic security"));
  using var viewer=host.Browser();await SecurityTestHost.Login(viewer,await host.Account(tenant,StudioRole.Viewer),tenant);Assert.Equal(HttpStatusCode.Forbidden,(await viewer.GetAsync(Base(f))).StatusCode);
  using var collaborator=host.Browser();await SecurityTestHost.Login(collaborator,await host.Account(tenant,StudioRole.Collaborator),tenant);Assert.Equal(HttpStatusCode.Forbidden,(await collaborator.PostAsJsonAsync(Minutes(f)+"/"+draft.Id+"/finalize",new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid()),SecurityTestHost.Json)).StatusCode);
  using var csrf=host.Browser();await SecurityTestHost.Login(csrf,await host.Account(tenant),tenant);csrf.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,(await csrf.PostAsJsonAsync(Minutes(f),new MinutesDraftInput("No CSRF"),SecurityTestHost.Json)).StatusCode);
  var finalized=await Send<MinutesDto>(f.C,Minutes(f)+"/"+draft.Id+"/finalize",new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid()));Assert.Equal(HttpStatusCode.Forbidden,(await viewer.GetAsync(Base(f)+"/"+finalized.DocumentId+"/download")).StatusCode);
  var otherTenant=Guid.NewGuid();await using(var seed=host.Services.CreateAsyncScope()){await seed.ServiceProvider.GetRequiredService<AppDbContext>().Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Tenants\" (\"Id\",\"Code\",\"Name\",\"CreatedAt\",\"UpdatedAt\") VALUES ({otherTenant},{otherTenant.ToString()},{"Synthetic document isolation"},now(),now())");}using var other=host.Browser();await SecurityTestHost.Login(other,await host.Account(otherTenant),otherTenant);Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(Base(f)+"/"+finalized.DocumentId+"/download")).StatusCode);
  Assert.Equal(HttpStatusCode.NotFound,(await f.C.GetAsync($"/api/documents/condominiums/{Guid.NewGuid()}/{finalized.DocumentId}/download")).StatusCode);
  await f.C.GetByteArrayAsync(Base(f)+"/"+finalized.DocumentId+"/download");await using var scope=host.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();Assert.True(await db.SecurityAudits.AnyAsync(a=>a.TenantId==tenant&&a.SubjectId==finalized.DocumentId&&a.Event=="Documents.Downloaded"));
 }
 [PostgresFact] public async Task UploadIsIdempotentSanitizedAndRejectsCrossCondominiumLinks()
 {
  await using var f=await Completed();var request=Guid.NewGuid();var bytes=new DocumentPdfGenerator().Generate("Synthetic attachment",["Synthetic"],false);
  async Task<HttpResponseMessage> Upload(Guid id,string links){var form=new MultipartFormDataContent();form.Add(new StringContent("Synthetic upload"),"title");form.Add(new StringContent(id.ToString()),"requestId");form.Add(new StringContent(links),"links");var content=new ByteArrayContent(bytes);content.Headers.ContentType=new("application/pdf");form.Add(content,"file","../../unsafe.pdf");return await f.C.PostAsync(Base(f)+"/upload",form);}
  using var r=await Upload(request,"[]");Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());var d=(await r.Content.ReadFromJsonAsync<DocumentDto>(SecurityTestHost.Json))!;Assert.Equal("unsafe.pdf",d.FileName);using var again=await Upload(request,"[]");Assert.Equal(d.Id,(await again.Content.ReadFromJsonAsync<DocumentDto>(SecurityTestHost.Json))!.Id);
  using var bad=await Upload(Guid.NewGuid(),$"[{{\"type\":\"Meeting\",\"id\":\"{Guid.NewGuid()}\"}}]");Assert.Equal(HttpStatusCode.NotFound,bad.StatusCode);
 }
 [PostgresFact] public async Task MissingAndCorruptFilesCannotBeDownloadedAndDatabaseHistoryIsProtected()
 {
  await using var f=await Completed();var draft=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("Synthetic integrity"));var final=await Send<MinutesDto>(f.C,Minutes(f)+"/"+draft.Id+"/finalize",new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid()));
  await using var scope=f.F.Data.Base.Host.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var row=await db.Documents.IgnoreQueryFilters().SingleAsync(d=>d.Id==final.DocumentId);var storage=scope.ServiceProvider.GetRequiredService<IDocumentStorage>();Assert.True(await storage.Exists(row.TenantId,row.StorageKey,default));
  await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Documents\" SET \"Title\"='tamper' WHERE \"Id\"={row.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"MeetingMinutes\" WHERE \"Id\"={draft.Id}"));
  // Dedicated synthetic test storage only. Locate via configured root, never touch user files.
  var root=scope.ServiceProvider.GetRequiredService<IConfiguration>()["Documents:RootPath"]??Path.Combine(AppContext.BaseDirectory,"private-documents");var path=Path.Combine(Path.GetFullPath(root),row.TenantId.ToString("N"),row.StorageKey+".bin");var original=await File.ReadAllBytesAsync(path);await File.WriteAllBytesAsync(path,"corrupt synthetic"u8.ToArray());Assert.Equal(HttpStatusCode.Conflict,(await f.C.GetAsync(Base(f)+"/"+row.Id+"/download")).StatusCode);await File.WriteAllBytesAsync(path,original);File.Move(path,path+".missing");Assert.Equal(HttpStatusCode.Conflict,(await f.C.GetAsync(Base(f)+"/"+row.Id+"/download")).StatusCode);File.Move(path+".missing",path);
 }
 [PostgresFact] public async Task StorageFailureNeverFinalizesMinutesAndCleansStaging()
 {
  await using var f=await Completed();var draft=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("Storage failure"));var host=f.F.Data.Base.Host;var storage=host.Services.GetRequiredService<IDocumentStorage>();var tenant=f.F.Data.Base.Tenant;
  await using var broken=host.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<IDocumentStorage>();s.AddSingleton<IDocumentStorage>(new FailingStorage(storage));}));using var client=broken.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost"),AllowAutoRedirect=false});await SecurityTestHost.Login(client,await host.Account(tenant),tenant);
  using var result=await client.PostAsJsonAsync(Minutes(f)+"/"+draft.Id+"/finalize",new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid()),SecurityTestHost.Json);Assert.Equal(HttpStatusCode.InternalServerError,result.StatusCode);
  var rows=(await f.C.GetFromJsonAsync<MinutesDto[]>(Minutes(f),SecurityTestHost.Json))!;Assert.Equal(MinutesStatus.Draft,rows.Single().Status);Assert.Null(rows.Single().DocumentId);Assert.Empty(storage.Pending().Where(p=>p.TenantId==tenant));
 }
 [PostgresFact] public async Task DatabaseFailureAfterStorageRollsBackAndRemovesOnlyUncommittedFile()
 {
  await using var f=await Completed();var draft=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("DB failure"));var host=f.F.Data.Base.Host;var tenant=f.F.Data.Base.Tenant;await using var scope=host.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var storage=scope.ServiceProvider.GetRequiredService<IDocumentStorage>();
  await db.Database.ExecuteSqlRawAsync("CREATE FUNCTION cm_document_test_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Synthetic database failure' USING ERRCODE='23514'; END $$; CREATE TRIGGER cm_document_test_failure BEFORE INSERT ON \"Documents\" FOR EACH ROW EXECUTE FUNCTION cm_document_test_failure();");
  try{using var result=await f.C.PostAsJsonAsync(Minutes(f)+"/"+draft.Id+"/finalize",new MinutesFinalizeInput(draft.Revision,draft.SourceFingerprint,Guid.NewGuid()),SecurityTestHost.Json);Assert.Equal(HttpStatusCode.BadRequest,result.StatusCode);var rows=(await f.C.GetFromJsonAsync<MinutesDto[]>(Minutes(f),SecurityTestHost.Json))!;Assert.Null(rows.Single().DocumentId);Assert.Empty(storage.Pending().Where(p=>p.TenantId==tenant));Assert.False(await db.Documents.IgnoreQueryFilters().AnyAsync(d=>d.TenantId==tenant));}
  finally{await db.Database.ExecuteSqlRawAsync("DROP TRIGGER cm_document_test_failure ON \"Documents\"; DROP FUNCTION cm_document_test_failure();");}
 }
 [PostgresFact] public async Task ConcurrentFinalizationsProduceOneDocumentAndStaleDraftEditsFail()
 {
  await using var f=await Completed();var d=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("Synthetic concurrency"));var updated=await Send<MinutesDto>(f.C,Minutes(f)+"/"+d.Id,new MinutesEditInput(d.Revision,d.Title,"Updated",null,null),"PUT");using var stale=await f.C.PutAsJsonAsync(Minutes(f)+"/"+d.Id,new MinutesEditInput(d.Revision,d.Title,"Stale",null,null),SecurityTestHost.Json);Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
  var input=new MinutesFinalizeInput(updated.Revision,updated.SourceFingerprint,Guid.NewGuid());var tasks=Enumerable.Range(0,2).Select(_=>f.C.PostAsJsonAsync(Minutes(f)+"/"+d.Id+"/finalize",input,SecurityTestHost.Json)).ToArray();var replies=await Task.WhenAll(tasks);Assert.All(replies,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));var ids=await Task.WhenAll(replies.Select(async r=>(await r.Content.ReadFromJsonAsync<MinutesDto>(SecurityTestHost.Json))!.DocumentId));Assert.Single(ids.Distinct());foreach(var r in replies)r.Dispose();var docs=(await f.C.GetFromJsonAsync<DocumentDto[]>(Base(f),SecurityTestHost.Json))!;Assert.Single(docs);
 }
 [PostgresFact] public async Task MinutesIncludeVerifiedProxyAndFrozenVotingData()
 {
  await using var f=await Completed(proxy:true);var d=await Send<MinutesDto>(f.C,Minutes(f),new MinutesDraftInput("Synthetic proxy minutes"));using var pdf=PdfDocument.Open(await f.C.GetByteArrayAsync(Minutes(f)+"/"+d.Id+"/preview"));var text=string.Join("",pdf.GetPages().Select(p=>p.Text));Assert.Contains("Delega:",text);Assert.Contains("Verified",text);Assert.Contains("ByProxy",text);Assert.Contains("500,000000",text.Replace(" ",""));Assert.Equal(1000,f.D.Quorums.Last().Evaluation.Totals.PresentValue);
 }
}




