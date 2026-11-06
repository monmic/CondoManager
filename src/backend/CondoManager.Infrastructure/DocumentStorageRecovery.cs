using CondoManager.Application;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
// Explicit local maintenance command only. No public endpoint and no automatic startup deletion.
public static class DocumentStorageRecovery
{
 public static async Task<(int Acknowledged,int Removed)> Run(AppDbContext db,IDocumentStorage storage,CancellationToken ct)
 {
  int acknowledged=0,removed=0;
  foreach(var p in storage.Pending().Where(p=>p.CreatedAt<DateTimeOffset.UtcNow.AddDays(-1)).ToArray()){
   // DB must be reachable. If a commit was ambiguous, the persisted row wins.
   var d=await db.Documents.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(d=>d.TenantId==p.TenantId&&d.StorageKey==p.Key,ct);
   if(d!=null){var actual=await storage.GetMetadata(p.TenantId,p.Key,ct);if(actual.Sha256!=d.Sha256||actual.SizeBytes!=d.SizeBytes)throw new InvalidOperationException("Ripristino storage necessario: integrità non verificata.");await storage.Acknowledge(p.TenantId,p.Key,ct);acknowledged++;}
   else{await storage.DiscardUncommitted(p.TenantId,p.Key,ct);removed++;}
  }
  return(acknowledged,removed);
 }
}
