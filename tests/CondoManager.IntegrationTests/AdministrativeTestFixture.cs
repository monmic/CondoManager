using System.Net.Http.Json;
using CondoManager.Application;
using CondoManager.Domain;
namespace CondoManager.IntegrationTests;
// Builds a real administrative close for legacy closed-period fixtures. No disabled guards or direct status SQL.
internal static class AdministrativeTestFixture
{
 private static async Task<T> Send<T>(HttpClient c,string path,object body){using var r=await c.PostAsJsonAsync(path,body,SecurityTestHost.Json);Assert.True(r.IsSuccessStatusCode,$"{path}: {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return (await r.Content.ReadFromJsonAsync<T>(SecurityTestHost.Json))!;}
 private static async Task<T> Get<T>(HttpClient c,string p)=>(await c.GetFromJsonAsync<T>(p,SecurityTestHost.Json))!;
 public static async Task<AccountingPeriodDto> Close(HttpClient c,Guid condo,Guid period)
 {
  var registry=$"/api/condominiums/{condo}";var finance=$"/api/final-statements/condominiums/{condo}";var periods=await Get<AccountingPeriodDto[]>(c,registry+"/accounting-periods");var p=periods.Single(p=>p.Id==period);var structure=await Get<StructureDto>(c,registry+"/structure");
  if(structure.Units.Count==0){await Send<PropertyUnitDto>(c,registry+"/units",new PropertyUnitInput("CLOSE",null,null,null,null,PropertyUnitKind.Apartment,null,null));structure=await Get<StructureDto>(c,registry+"/structure");}
  var owner=await Send<PersonDto>(c,"/api/people",new PersonInput("CLOSE-"+Guid.NewGuid().ToString("N")[..8],PersonKind.NaturalPerson,"Synthetic","Closure fixture","SYNTHETIC-CLOSE-"+Guid.NewGuid().ToString("N")[..8],null,null,null,null,null));
  foreach(var u in structure.Units)await Send<AssociationDto>(c,registry+$"/units/{u.Id}/associations",new AssociationInput(owner.Id,UnitPersonRole.Other,p.StartsOn,null,null));
  var ap=$"/api/allocations/condominiums/{condo}";var rule=await Send<RuleDto>(c,ap+"/rules",new RuleInput("Close-"+Guid.NewGuid().ToString("N")[..8],null,AllocationCriterion.EqualShares,[new(AllocationCriterion.EqualShares,100,null,null,null,null)]));await Send<RuleDto>(c,ap+$"/rules/{rule.Id}/activate",new{});
  var expenses=await Get<ExpenseDto[]>(c,$"/api/accounting/condominiums/{condo}/expenses?period={period}");var runs=await Get<AllocationRunDto[]>(c,ap+"/runs?period="+period);var current=runs.Where(r=>r.ReplacedByRunId==null).ToArray();
  foreach(var e in expenses.Where(e=>e.AccountingPeriodId==period && e.Status is not ExpenseStatus.Draft and not ExpenseStatus.Cancelled)){var allocated=current.SelectMany(r=>r.Snapshot.Sources).Where(s=>s.ExpenseDocumentId==e.Id).Sum(s=>s.Amount);var missing=e.GrossAmount-allocated;if(missing>0){var input=new AllocationPreviewInput(period,p.StartsOn,p.EndsOn,[new(e.Id,missing,rule.Id)]);var preview=await Send<AllocationPreviewDto>(c,ap+"/preview",input);await Send<AllocationRunDto>(c,ap+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),preview.PreviewHash,input));}}
  await Send<OpeningDto>(c,finance+$"/periods/{period}/opening",new OpeningInput(Guid.NewGuid(),OpeningMode.VerifiedZero,"Synthetic zero verified for legacy fixture"));
  var final=await Send<FinalDto>(c,finance,new FinalInput(period,"Synthetic controlled closure",Guid.NewGuid(),false,p.StartsOn,structure.Units.Select(u=>new DebtorSelection(u.Id,owner.Id,1)).ToArray(),"Synthetic exercise without preventive budget"));
  await Send<FinalDto>(c,finance+$"/{final.Id}/propose",new{});await Send<FinalDto>(c,finance+$"/{final.Id}/approve",new FinalApproval(final.Snapshot.Fingerprint));var settlement=await Send<SettlementPreview>(c,finance+$"/{final.Id}/settlement-preview",new SettlementPreviewInput(p.EndsOn));var batch=await Send<SettlementDto>(c,finance+$"/{final.Id}/settlement-confirm",new SettlementConfirm(Guid.NewGuid(),p.EndsOn,settlement.Fingerprint));await Send<SettlementDto>(c,finance+$"/settlements/{batch.Id}/post",new{});var review=await Get<ClosureReview>(c,finance+$"/periods/{period}/review");await Send<ClosureDto>(c,finance+$"/periods/{period}/close",new CloseInput(Guid.NewGuid(),review.Fingerprint));return (await Get<AccountingPeriodDto[]>(c,registry+"/accounting-periods")).Single(x=>x.Id==period);
 }
}
