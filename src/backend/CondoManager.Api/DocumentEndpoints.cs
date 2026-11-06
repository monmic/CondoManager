using System.Text.Json;
using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.Api;
public static class DocumentEndpoints
{
 public record ReportInput(DocumentType Type,Guid? RelatedId,Guid RequestId);
 public static void MapDocuments(this WebApplication app)
 {
  var g=app.MapGroup("/api/documents/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
  g.MapGet("/",(Guid condo,string? search,DocumentType? type,DocumentTarget? targetType,Guid? target,IDocumentService s,CancellationToken ct)=>s.List(condo,search,type,targetType,target,ct));
  g.MapGet("/{id:guid}",(Guid condo,Guid id,IDocumentService s,CancellationToken ct)=>s.Details(condo,id,ct));
  g.MapGet("/{id:guid}/versions",(Guid condo,Guid id,IDocumentService s,CancellationToken ct)=>s.Versions(condo,id,ct));
  g.MapGet("/{id:guid}/links",async(Guid condo,Guid id,IDocumentService s,CancellationToken ct)=>(await s.Details(condo,id,ct)).Links);
  g.MapGet("/{id:guid}/download",async(Guid condo,Guid id,IDocumentService s,CancellationToken ct)=>{var f=await s.Download(condo,id,ct);return Results.File(f.Bytes,f.ContentType,f.FileName);});
  g.MapPost("/upload",async(Guid condo,HttpRequest request,IDocumentService s,IConfiguration config,CancellationToken ct)=>{
   var max=config.GetValue<int?>("Documents:MaxUploadBytes")??8388608;
   if(request.ContentLength is null||request.ContentLength>max+65536)throw new ArgumentException("Dimensione upload non consentita.");
   var form=await request.ReadFormAsync(ct);if(form.Files.Count!=1)throw new ArgumentException("Selezionare un solo allegato.");var file=form.Files[0];if(file.Length>max)throw new ArgumentException("File troppo grande.");
   await using var input=file.OpenReadStream();using var memory=new MemoryStream();await input.CopyToAsync(memory,ct);
   var links=JsonSerializer.Deserialize<DocumentLinkInput[]>(form["links"].ToString(),new JsonSerializerOptions(JsonSerializerDefaults.Web){Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()}})??[];
   return await s.Upload(condo,form["title"].ToString(),form["description"].ToString(),file.FileName,file.ContentType,memory.ToArray(),Guid.Parse(form["requestId"].ToString()),links,Guid.TryParse(form["previousDocumentId"],out var previous)?previous:null,ct);
  });
  var m=g.MapGroup("/meetings/{meeting:guid}/minutes");
  m.MapGet("/",(Guid condo,Guid meeting,IDocumentService s,CancellationToken ct)=>s.Minutes(condo,meeting,ct));
  m.MapPost("/",(Guid condo,Guid meeting,MinutesDraftInput i,IDocumentService s,CancellationToken ct)=>s.CreateDraft(condo,meeting,i,ct));
  m.MapPut("/{id:guid}",(Guid condo,Guid meeting,Guid id,MinutesEditInput i,IDocumentService s,CancellationToken ct)=>s.Edit(condo,meeting,id,i,ct));
  m.MapGet("/{id:guid}/validation",(Guid condo,Guid meeting,Guid id,IDocumentService s,CancellationToken ct)=>s.Validate(condo,meeting,id,ct));
  m.MapGet("/{id:guid}/preview",async(Guid condo,Guid meeting,Guid id,IDocumentService s,CancellationToken ct)=>{var f=await s.Preview(condo,meeting,id,ct);return Results.File(f.Bytes,f.ContentType,f.FileName);});
  m.MapPost("/{id:guid}/finalize",(Guid condo,Guid meeting,Guid id,MinutesFinalizeInput i,IDocumentService s,CancellationToken ct)=>s.Finalize(condo,meeting,id,i,ct));
  g.MapPost("/meetings/{meeting:guid}/reports",(Guid condo,Guid meeting,ReportInput i,IDocumentService s,CancellationToken ct)=>s.Report(condo,meeting,i.Type,i.RelatedId,i.RequestId,ct));
 }
}
