using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed partial class AccountingService : IFinalStatementService
{
 private async Task<AccountingPeriod> FinalPeriod(Guid condo,Guid id,CancellationToken ct,bool open=true){var p=await db.AccountingPeriods.SingleOrDefaultAsync(p=>p.Id==id && p.CondominiumId==condo,ct)??throw new NotFoundException();if(open && p.Status!=AccountingPeriodStatus.Open)throw new InvalidOperationException("Esercizio non aperto.");return p;}
 private async Task<FinalStatement> Statement(Guid condo,Guid id,CancellationToken ct)=>await db.FinalStatementRows.SingleOrDefaultAsync(s=>s.Id==id && s.CondominiumId==condo,ct)??throw new NotFoundException();
 private static FinalDto FinalResult(FinalStatement s)=>new(s.Id,s.AccountingPeriodId,s.Version,s.PreviousVersionId,s.Description,s.Status,s.CreatedBy,s.CreatedAt,s.ApprovedBy,s.ApprovedAt,s.DecisionReference,FRead<FinalInput>(s.InputJson),FRead<FinalPreview>(s.SnapshotJson));
 private sealed record ResidentFact(Guid PropertyUnitId,string UnitCode,Guid PersonId,string PersonName,Guid OriginPeriodId,Guid JournalId,decimal Amount,string Kind);
 private async Task<List<ResidentFact>> ResidentFacts(Guid condo,CancellationToken ct)
 {
  var facts=new List<ResidentFact>();
  var issued=await (from d in db.InstallmentDebtors join p in db.InstallmentPlans on d.PlanId equals p.Id where p.CondominiumId==condo && p.JournalEntryId!=null select new{d,p}).ToListAsync(ct);
  facts.AddRange(issued.Select(x=>new ResidentFact(x.d.PropertyUnitId,x.d.UnitCode,x.d.PersonId,x.d.PersonName,x.p.AccountingPeriodId,x.p.JournalEntryId!.Value,x.d.Amount,"Issue")));
  var allocations=await (from a in db.ReceiptAllocations join i in db.Installments on a.InstallmentId equals i.Id join d in db.InstallmentDebtors on i.InstallmentDebtorId equals d.Id join p in db.InstallmentPlans on d.PlanId equals p.Id where a.CondominiumId==condo select new{a,d,p}).ToListAsync(ct);
  facts.AddRange(allocations.Select(x=>new ResidentFact(x.d.PropertyUnitId,x.d.UnitCode,x.d.PersonId,x.d.PersonName,x.p.AccountingPeriodId,x.a.JournalEntryId,x.a.ReversesAllocationId.HasValue?x.a.Amount:-x.a.Amount,"Receipt")));
  var settlements=await (from l in db.SettlementPositions join b in db.StatementSettlements on l.SettlementId equals b.Id where b.CondominiumId==condo && b.PostedAt!=null select new{l,b}).ToListAsync(ct);
  foreach(var x in settlements){if(x.b.JournalEntryId.HasValue)facts.Add(new(x.l.PropertyUnitId,x.l.UnitCode,x.l.PersonId,x.l.PersonName,x.b.AccountingPeriodId,x.b.JournalEntryId.Value,x.l.Amount,"Settlement"));if(x.b.ReversedJournalId.HasValue)facts.Add(new(x.l.PropertyUnitId,x.l.UnitCode,x.l.PersonId,x.l.PersonName,x.b.AccountingPeriodId,x.b.ReversedJournalId.Value,-x.l.Amount,"Settlement"));}
  var imports=await (from l in db.OpeningPositions join o in db.ResidentOpenings on l.OpeningId equals o.Id where o.CondominiumId==condo && o.Mode==OpeningMode.Import && o.JournalEntryId!=null select new{l,o}).ToListAsync(ct);
  facts.AddRange(imports.Select(x=>new ResidentFact(x.l.PropertyUnitId,x.l.UnitCode,x.l.PersonId,x.l.PersonName,x.o.AccountingPeriodId,x.o.JournalEntryId!.Value,x.l.Amount,"Opening")));
  return facts.OrderBy(f=>f.OriginPeriodId).ThenBy(f=>f.JournalId).ThenBy(f=>f.PropertyUnitId).ThenBy(f=>f.PersonId).ToList();
 }
 private static FinancialPosition[] GroupFacts(IEnumerable<ResidentFact> facts)=>facts.GroupBy(f=>(f.PropertyUnitId,f.PersonId)).OrderBy(g=>g.Key.PropertyUnitId).ThenBy(g=>g.Key.PersonId).Select(g=>new FinancialPosition(g.Key.PropertyUnitId,g.First().UnitCode,g.Key.PersonId,g.First().PersonName,g.Sum(f=>f.Amount))).ToArray();
 private async Task<FinalPreview> BuildFinal(Guid condo,FinalInput input,CancellationToken ct,bool locks=false)
 {
  var period=await FinalPeriod(condo,input.AccountingPeriodId,ct);Guard.Id(input.RequestId);Guard.Text(input.Description);Guard.Optional(input.NoBudgetReason);var anomalies=new List<StatementAnomaly>();void Problem(string code,string message,Guid? id=null,bool block=true)=>anomalies.Add(new(code,message,block,id));
  var budget=await db.Budgets.SingleOrDefaultAsync(b=>b.CondominiumId==condo && b.AccountingPeriodId==period.Id && b.Status==BudgetStatus.Approved,ct);
  if(budget==null && string.IsNullOrWhiteSpace(input.NoBudgetReason))Problem("BudgetMissing","Preventivo approvato assente: dichiarare esplicitamente la previsione zero e il motivo.");
  var accounts=await db.Accounts.Where(a=>a.CondominiumId==condo).ToDictionaryAsync(a=>a.Id,ct);
  var journals=await db.JournalEntries.Include(j=>j.Lines).Where(j=>j.CondominiumId==condo).OrderBy(j=>j.Id).ToListAsync(ct);var journalMap=journals.ToDictionary(j=>j.Id);
  var expenses=await db.ExpenseDocuments.Where(e=>e.CondominiumId==condo && e.AccountingPeriodId==period.Id && e.IsRegistered).OrderBy(e=>e.Id).ToListAsync(ct);
  var runs=await db.AllocationRuns.Where(r=>r.CondominiumId==condo && r.AccountingPeriodId==period.Id && !db.AllocationRuns.Any(n=>n.PreviousRunId==r.Id)).OrderBy(r=>r.Id).ToListAsync(ct);
  var sources=await db.AllocationRunSources.Where(s=>runs.Select(r=>r.Id).Contains(s.RunId)).OrderBy(s=>s.Id).ToListAsync(ct);
  var frozenRuns=new List<AllocationPreviewDto>();var actualRaw=new List<(Guid Unit,Guid? Person,decimal Amount)>();var evidence=new List<ExpenseEvidence>();
  var units=await db.PropertyUnits.Where(u=>u.CondominiumId==condo).OrderBy(u=>u.Id).ToDictionaryAsync(u=>u.Id,ct);var activeUnits=units.Values.Where(u=>u.IsActive).OrderBy(u=>u.Code).ThenBy(u=>u.Id).ToList();
  foreach(var run in runs)
  {
   var saved=FRead<AllocationPreviewDto>(run.SnapshotJson);var used=sources.Where(s=>s.RunId==run.Id).ToArray();var relevant=used.Where(s=>expenses.Any(e=>e.Id==s.ExpenseDocumentId && !e.IsCancelled)).ToArray();if(relevant.Length==0)continue;
   if(!saved.Result.IsValid || saved.Result.Lines.Sum(l=>l.Amount)!=run.TotalAmount)Problem("RunUnbalanced","Riparto confermato non quadrato.",run.Id);
   frozenRuns.Add(saved);var original=FRead<AllocationPreviewInput>(run.InputJson);
   foreach(var source in relevant)
   {
    var frozen=saved.Snapshot.Sources.SingleOrDefault(s=>s.Position==source.Position);if(frozen==null || frozen.ExpenseDocumentId!=source.ExpenseDocumentId || frozen.Amount!=source.Amount || frozen.TenantId!=TenantId || frozen.CondominiumId!=condo){Problem("RunScope","Fonte del riparto incoerente.",run.Id);continue;}
    try{var resolver=finalResolver??throw new InvalidOperationException("Resolver M05 non disponibile.");var parameters=original.Sources[source.Position];var (_,components)=await resolver.ResolveCriteria(condo,period,source.RuleId,run.CompetenceFrom,run.CompetenceTo,activeUnits,parameters.ExcludedUnitIds,parameters.DirectCharges,locks,ct);if(FHash(components)!=FHash(frozen.Components))Problem("AllocationSourcesChanged","Fonti M05 modificate: sostituire esplicitamente il riparto.",run.Id);}catch(Exception e)when(e is ArgumentException or InvalidOperationException or NotFoundException){Problem("AllocationSourcesChanged",e.Message,run.Id);}
    var lines=saved.Result.Lines.Where(l=>l.SourcePosition==source.Position).ToArray();if(lines.Sum(l=>l.Amount)!=source.Amount)Problem("SourceUnbalanced","Quote della spesa non quadrate.",source.ExpenseDocumentId);actualRaw.AddRange(lines.Select(l=>(l.PropertyUnitId,l.PersonId,l.Amount)));
   }
  }
  foreach(var e in expenses)
  {
   var posting=journals.SingleOrDefault(j=>j.Kind==JournalKind.Expense && j.ExpenseDocumentId==e.Id);if(posting==null){Problem("ExpenseJournalMissing","Spesa priva della scrittura di origine.",e.Id);continue;}
   if(e.IsCancelled){Problem("CancelledExpense","Spesa stornata esclusa; eventuali riparti restano nello storico.",e.Id,false);continue;}
   var used=sources.Where(s=>s.ExpenseDocumentId==e.Id).ToArray();var allocated=used.Sum(s=>s.Amount);if(allocated!=e.GrossAmount)Problem("ExpenseUnallocated","Spesa non interamente ripartita: "+e.DocumentNumber,e.Id);
   var payments=await (from p in db.ExpensePayments join j in db.JournalEntries on p.JournalEntryId equals j.Id where p.ExpenseDocumentId==e.Id select new PaymentDto(p.Id,p.FinancialAccountId,j.Id,j.Date,p.Amount,j.Reference,db.JournalEntries.Any(r=>r.ReversesEntryId==j.Id))).ToArrayAsync(ct);
   evidence.Add(new(e.Id,e.AccountId,accounts[e.AccountId].Name,e.DocumentNumber,e.DocumentDate,posting.Date,period.StartsOn,period.EndsOn,e.GrossAmount,allocated,used.Select(s=>s.RunId).Distinct().Order().ToArray(),payments,posting.CreatedAt));
  }
  var actual=evidence.Sum(e=>e.Amount);var allocatedTotal=actualRaw.Sum(r=>r.Amount);if(allocatedTotal!=actual)Problem("CostAllocationDifference","Totale quote diverso dal costo di competenza.");
  var expenseAccountIds=accounts.Values.Where(a=>a.Type==AccountType.Expense).Select(a=>a.Id).ToHashSet();
  var costLedger=journals.Where(j=>j.AccountingPeriodId==period.Id).SelectMany(j=>j.Lines).Where(l=>expenseAccountIds.Contains(l.AccountId)).Sum(l=>l.Debit-l.Credit);if(costLedger!=actual)Problem("UnclassifiedCost","Costi del giornale diversi dai documenti pertinenti: classificare o rettificare le scritture manuali prima di procedere.");
  if(journals.Any(j=>j.AccountingPeriodId==period.Id && j.Lines.Sum(l=>l.Debit)!=j.Lines.Sum(l=>l.Credit)))Problem("UnbalancedLedger","Scrittura Dare/Avere sbilanciata.");
  var plan=await db.InstallmentPlans.SingleOrDefaultAsync(p=>p.CondominiumId==condo && p.AccountingPeriodId==period.Id && p.JournalEntryId!=null,ct);
  var issued=plan==null?new List<InstallmentDebtor>():await db.InstallmentDebtors.Where(d=>d.PlanId==plan.Id).OrderBy(d=>d.Id).ToListAsync(ct);
  var chosen=input.Debtors??[];if(chosen.Select(d=>(d.PropertyUnitId,d.PersonId)).Distinct().Count()!=chosen.Count)throw new ArgumentException("Designazioni duplicate.");
  var budgetPersonIds=budget==null?Array.Empty<Guid>():FRead<BudgetPreviewDto>(budget.AllocationSnapshotJson!).Result.Lines.Where(l=>l.PersonId.HasValue).Select(l=>l.PersonId!.Value).ToArray();var peopleIds=actualRaw.Where(r=>r.Person.HasValue).Select(r=>r.Person!.Value).Concat(budgetPersonIds).Concat(chosen.Select(d=>d.PersonId)).Concat(issued.Select(d=>d.PersonId)).Distinct().Order().ToArray();
  if(locks)foreach(var id in peopleIds)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"People\" WHERE \"Id\"={id} AND \"TenantId\"={TenantId} FOR SHARE",ct);
  var people=await db.People.AsNoTracking().Where(p=>peopleIds.Contains(p.Id)).ToDictionaryAsync(p=>p.Id,ct);var associations=await db.OwnershipPeriods.Where(a=>units.Keys.Contains(a.PropertyUnitId)).OrderBy(a=>a.Id).ToListAsync(ct);var debtorDate=input.DebtorDate??period.StartsOn;if(debtorDate<period.StartsOn || debtorDate>period.EndsOn)throw new ArgumentException("Data designazione fuori esercizio.");
  var designations=new List<object>();
  Dictionary<(Guid Unit,Guid Person),decimal> Assign(IEnumerable<(Guid Unit,Guid? Person,decimal Amount)> rows)
  {
   var result=new Dictionary<(Guid,Guid),decimal>();void Add(Guid u,Guid p,decimal a){if(!people.ContainsKey(p) || !units.ContainsKey(u)){Problem("DebtorScope","Soggetto o unita fuori dallo studio/condominio.",u);return;}result[(u,p)]=result.GetValueOrDefault((u,p))+a;}
   foreach(var unitRows in rows.GroupBy(r=>r.Unit))
   {
    foreach(var direct in unitRows.Where(r=>r.Person.HasValue))Add(direct.Unit,direct.Person!.Value,direct.Amount);
    var amount=unitRows.Where(r=>!r.Person.HasValue).Sum(r=>r.Amount);if(amount==0)continue;
    var historical=issued.Where(d=>d.PropertyUnitId==unitRows.Key).OrderBy(d=>d.PersonId).ToArray();
    var selected=chosen.Where(d=>d.PropertyUnitId==unitRows.Key).OrderBy(d=>d.PersonId).ToArray();
    if(input.UseIssuedDebtors && historical.Length>0 && historical.Sum(d=>d.Amount)>0){var split=AllocationEngine.SplitCents(amount,historical.Select(d=>d.Amount).ToArray(),historical.Select(d=>d.PersonId.ToString("D")).ToArray());for(var i=0;i<historical.Length;i++)Add(unitRows.Key,historical[i].PersonId,split[i].Amount);designations.Add(new{Unit=unitRows.Key,OriginPlan=plan!.Id,Debtors=historical.Select(d=>new{d.PersonId,d.PersonName,d.Amount}).ToArray()});}
    else if(!input.UseIssuedDebtors && selected.Length>0 && selected.Sum(d=>d.Share)==1)
    {var valid=true;foreach(var d in selected){Guard.Decimal(d.Share,6,0.000001m,1);var a=associations.FirstOrDefault(a=>a.PersonId==d.PersonId && a.PropertyUnitId==d.PropertyUnitId && a.ValidFrom<=debtorDate && (a.ValidTo==null || a.ValidTo>=debtorDate));if(a==null || a.Fraction.HasValue && a.Fraction!=d.Share){Problem("DebtorUndetermined","Designazione non coerente con lo storico.",unitRows.Key);valid=false;}}if(valid){var split=AllocationEngine.SplitCents(amount,selected.Select(d=>d.Share).ToArray(),selected.Select(d=>d.PersonId.ToString("D")).ToArray());for(var i=0;i<selected.Length;i++)Add(unitRows.Key,selected[i].PersonId,split[i].Amount);designations.Add(new{Unit=unitRows.Key,Date=debtorDate,Debtors=selected});}}
    else Problem("DebtorUndetermined","Debitore non determinabile: confermare i soggetti storici del piano oppure designarli esplicitamente.",unitRows.Key);
   }
   return result;
  }
  var actualAssigned=Assign(actualRaw);var budgetSnapshot=budget==null?null:FRead<BudgetPreviewDto>(budget.AllocationSnapshotJson!);var budgetAssigned=Assign(budgetSnapshot?.Result.Lines.Select(l=>(l.PropertyUnitId,l.PersonId,l.Amount))??[]);
  var budgetLines=budget==null?new List<BudgetLine>():await db.BudgetLines.Where(l=>l.BudgetId==budget.Id).OrderBy(l=>l.Position).ToListAsync(ct);
  var categories=budgetLines.Select(l=>l.AccountId).Union(evidence.Select(e=>e.AccountId)).Order().Select(id=>FinalCalculations.Compare(id,accounts[id].Name,budgetLines.Where(l=>l.AccountId==id).Sum(l=>l.Amount),evidence.Where(e=>e.AccountId==id).Sum(e=>e.Amount))).ToArray();
  var opening=await db.ResidentOpenings.SingleOrDefaultAsync(o=>o.CondominiumId==condo && o.AccountingPeriodId==period.Id,ct);if(opening==null)Problem("OpeningUndetermined","Saldo iniziale non determinato: verificare zero, importare saldi o riportare la chiusura precedente.");
  var openingRows=opening==null?new List<OpeningPosition>():await db.OpeningPositions.Where(l=>l.OpeningId==opening.Id).ToListAsync(ct);var facts=await ResidentFacts(condo,ct);var oldJournalIds=new HashSet<Guid>();
  if(opening?.SourceClosureId!=null){var c=await db.AdministrativeClosures.SingleAsync(c=>c.Id==opening.SourceClosureId,ct);var review=FRead<ClosureReview>(c.SnapshotJson);using var doc=System.Text.Json.JsonDocument.Parse(review.EvidenceJson);foreach(var f in doc.RootElement.GetProperty("facts").EnumerateArray())oldJournalIds.Add(f.GetProperty("journalId").GetGuid());}
  var priorNew=facts.Where(f=>f.OriginPeriodId!=period.Id && !oldJournalIds.Contains(f.JournalId)).ToArray();if(opening?.Mode!=OpeningMode.CarryForward && priorNew.Any())Problem("PriorPeriodUndetermined","Esistono posizioni di altri esercizi senza riporto verificato.");
  var keys=actualAssigned.Keys.Union(budgetAssigned.Keys).Union(issued.Select(d=>(d.PropertyUnitId,d.PersonId))).Union(openingRows.Select(o=>(o.PropertyUnitId,o.PersonId))).Union(priorNew.Select(f=>(f.PropertyUnitId,f.PersonId))).OrderBy(k=>k.Item1).ThenBy(k=>k.Item2).ToArray();var positions=new List<FinalPosition>();
  foreach(var key in keys)
  {
   var (u,p)=key;var b=budgetAssigned.GetValueOrDefault(key);var a=actualAssigned.GetValueOrDefault(key);var emitted=issued.Where(d=>d.PropertyUnitId==u && d.PersonId==p).Sum(d=>d.Amount);var paid=-facts.Where(f=>f.OriginPeriodId==period.Id && f.PropertyUnitId==u && f.PersonId==p && f.Kind=="Receipt").Sum(f=>f.Amount);var initial=openingRows.Where(l=>l.PropertyUnitId==u && l.PersonId==p).Sum(l=>l.Amount);var priorPaid=-priorNew.Where(f=>f.PropertyUnitId==u && f.PersonId==p).Sum(f=>f.Amount);var label=issued.FirstOrDefault(d=>d.PropertyUnitId==u && d.PersonId==p);var old=openingRows.FirstOrDefault(l=>l.PropertyUnitId==u && l.PersonId==p);var delta=FinalCalculations.DeltaToPost(a,emitted);
   positions.Add(new(u,label?.UnitCode??old?.UnitCode??units.GetValueOrDefault(u)?.Code??u.ToString(),p,label?.PersonName??old?.PersonName??people.GetValueOrDefault(p)?.DisplayName??p.ToString(),b,a,0,FinalCalculations.Economic(b,a),emitted,paid,initial,priorPaid,delta,FinalCalculations.Financial(initial,emitted,delta,paid,priorPaid)));
  }
  if(actualAssigned.Values.Sum()!=actual)Problem("IndividualDifference","Quote individuali non quadrate con le spese.");
  var core=new{Period=period.Id,Budget=budget?.Id,BudgetVersion=budget?.Version,Opening=opening?.Id,Input=new{input.UseIssuedDebtors,input.DebtorDate,input.Debtors,input.NoBudgetReason},Expenses=expenses.Select(e=>new{e.Id,e.AccountingPeriodId,e.AccountId,e.SupplierId,e.DocumentNumber,e.DocumentDate,e.GrossAmount,e.IsCancelled}).ToArray(),Runs=runs.Select(r=>new{r.Id,r.PreviewHash,r.SnapshotJson}).ToArray(),Designations=designations,Positions=positions.Select(p=>new{p.PropertyUnitId,p.PersonId,p.Budget,p.Actual,p.Issued}).ToArray(),CostLedger=costLedger};
  var hash=FHash(core);return new(hash,budget?.Id,budget?.Version,budgetLines.Sum(l=>l.Amount),actual,allocatedTotal,actual-allocatedTotal,evidence,categories,positions,anomalies,FJson(new{Core=core,Budget=budgetSnapshot,Runs=frozenRuns,Opening=opening?.SnapshotJson}));
 }
 public async Task<FinalPreview> PreviewFinal(Guid condo,FinalInput input,CancellationToken ct){await Condo(condo,ct);await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct);var result=await BuildFinal(condo,input,ct);await tx.CommitAsync(ct);return result;}
 public Task<FinalDto> CreateFinal(Guid condo,FinalInput input,CancellationToken ct)=>Write(condo,async()=>{var hash=FHash(input);var existing=await db.FinalStatementRows.SingleOrDefaultAsync(s=>s.CondominiumId==condo && s.RequestId==input.RequestId,ct);if(existing!=null){if(existing.RequestHash!=hash)throw new InvalidOperationException("Chiave riutilizzata con dati diversi.");return FinalResult(existing);}var preview=await BuildFinal(condo,input,ct,true);var latest=await db.FinalStatementRows.Where(s=>s.CondominiumId==condo && s.AccountingPeriodId==input.AccountingPeriodId).OrderByDescending(s=>s.Version).FirstOrDefaultAsync(ct);if(latest?.Id!=input.PreviousVersionId)throw new InvalidOperationException("Indicare l'ultima versione precedente.");var s=new FinalStatement(TenantId,condo,input.AccountingPeriodId,preview.BudgetId,(latest?.Version??0)+1,latest?.Id,input.Description,input.Notes,current.UserId!.Value,input.RequestId,hash,FJson(input),FJson(preview),preview.Fingerprint);db.Add(s);Audit("Final.Created",s.Id);return FinalResult(s);},ct);
 public async Task<IReadOnlyList<FinalDto>> FinalStatements(Guid condo,Guid? period,CancellationToken ct){await Condo(condo,ct);return (await db.FinalStatementRows.Where(s=>s.CondominiumId==condo && (!period.HasValue || s.AccountingPeriodId==period)).OrderByDescending(s=>s.Version).ToListAsync(ct)).Select(FinalResult).ToArray();}
 public Task<FinalDto> ProposeFinal(Guid condo,Guid id,CancellationToken ct)=>Write(condo,async()=>{var s=await Statement(condo,id,ct);var live=await BuildFinal(condo,FRead<FinalInput>(s.InputJson),ct,true);EnsureFinalValid(s,live);s.Propose();Audit("Final.Proposed",id);return FinalResult(s);},ct);
 private static void EnsureFinalValid(FinalStatement s,FinalPreview preview){if(preview.Fingerprint!=s.SourceFingerprint)throw new InvalidOperationException("Fonti cambiate: creare una nuova versione del consuntivo.");if(preview.Anomalies.Any(a=>a.Blocking))throw new InvalidOperationException(string.Join(" ",preview.Anomalies.Where(a=>a.Blocking).Select(a=>a.Message)));}
 public Task<FinalDto> ApproveFinal(Guid condo,Guid id,FinalApproval input,CancellationToken ct)=>Write(condo,async()=>{current.RequireAdmin();var s=await Statement(condo,id,ct);if(s.Status==FinalStatementStatus.Approved && s.SourceFingerprint==input.Fingerprint && s.DecisionReference==Guard.Optional(input.DecisionReference))return FinalResult(s);await FinalPeriod(condo,s.AccountingPeriodId,ct);if(s.SourceFingerprint!=input.Fingerprint)throw new InvalidOperationException("Fingerprint non valido.");var live=await BuildFinal(condo,FRead<FinalInput>(s.InputJson),ct,true);EnsureFinalValid(s,live);if(await db.StatementSettlements.AnyAsync(b=>b.CondominiumId==condo && b.AccountingPeriodId==s.AccountingPeriodId && b.PostedAt!=null && b.ReversedJournalId==null,ct))throw new InvalidOperationException("Stornare esplicitamente il conguaglio prima di sostituire il consuntivo.");var old=await db.FinalStatementRows.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.AccountingPeriodId==s.AccountingPeriodId && x.Status==FinalStatementStatus.Approved,ct);if(old!=null){if(old.Version>=s.Version)throw new InvalidOperationException("Versione piu recente gia approvata.");old.Supersede();await db.SaveChangesAsync(ct);}s.Approve(current.UserId!.Value,input.DecisionReference);Audit("Final.Approved",id);return FinalResult(s);},ct);
}
