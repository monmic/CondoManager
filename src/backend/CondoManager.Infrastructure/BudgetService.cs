using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CondoManager.Application;
using CondoManager.Domain;
using Microsoft.EntityFrameworkCore;
namespace CondoManager.Infrastructure;
public sealed class BudgetService(AppDbContext db,ICurrentTenant current,AllocationService resolver,IAllocationEngine engine) : IBudgetService
{
    private Guid Tenant=>current.RequireRead();
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web) {NumberHandling=JsonNumberHandling.AllowReadingFromString|JsonNumberHandling.WriteAsString,Converters={new JsonStringEnumConverter()}};
    private static string Serialize<T>(T value)=>JsonSerializer.Serialize(value,Json);
    private static T Read<T>(string value)=>JsonSerializer.Deserialize<T>(value,Json)??throw new InvalidOperationException("Snapshot non valido.");
    private static string Hash<T>(T value)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,Json)));
    private async Task Scope(Guid condo,CancellationToken ct) {_=Tenant;if(!await db.Condominiums.AnyAsync(c=>c.Id==condo,ct)) throw new NotFoundException();}
    private async Task<T> Write<T>(Guid condo,Func<Task<T>> action,CancellationToken ct)
    {
        current.RequireWrite();await Scope(condo,ct);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(ct):null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Condominiums\" WHERE \"Id\"={condo} AND \"TenantId\"={Tenant} FOR UPDATE",ct);
        if(!await db.Condominiums.AnyAsync(c=>c.Id==condo && c.IsActive,ct)) throw new InvalidOperationException("Condominio inattivo.");
        var result=await action();await db.SaveChangesAsync(ct);if(tx!=null)await tx.CommitAsync(ct);return result;
    }
    private async Task<Budget> Load(Guid condo,Guid id,CancellationToken ct)=>await db.Budgets.SingleOrDefaultAsync(b=>b.Id==id && b.CondominiumId==condo,ct)??throw new NotFoundException();
    private async Task<AccountingPeriod> Period(Guid condo,Guid id,CancellationToken ct)
    {var p=await db.AccountingPeriods.SingleOrDefaultAsync(p=>p.Id==id && p.CondominiumId==condo,ct)??throw new NotFoundException();if(p.Status!=AccountingPeriodStatus.Open) throw new InvalidOperationException("Esercizio non aperto.");return p;}
    private async Task<BudgetDto> Dto(Budget b,CancellationToken ct)
    {
        var lines=await db.BudgetLines.Where(l=>l.BudgetId==b.Id).OrderBy(l=>l.Position).ToListAsync(ct);
        var inputs=lines.Select(l=>Read<BudgetLineInput>(l.ParametersJson)).ToArray();var total=lines.Sum(l=>l.Amount);
        decimal? previous=b.PreviousVersionId.HasValue?await db.BudgetLines.Where(l=>l.BudgetId==b.PreviousVersionId).SumAsync(l=>l.Amount,ct):null;
        return new(b.Id,b.AccountingPeriodId,b.Version,b.PreviousVersionId,b.Description,b.Notes,b.Status,b.CreatedAt,b.ApprovedBy,b.ApprovedAt,b.DecisionReference,total,previous,previous.HasValue?total-previous:null,inputs,b.AllocationSnapshotJson==null?null:Read<BudgetPreviewDto>(b.AllocationSnapshotJson).Result);
    }
    private void Audit(string name,Guid id)=>db.SecurityAudits.Add(new(){Id=Guid.NewGuid(),TenantId=Tenant,ActorUserId=current.UserId,SubjectId=id,Event=name,Timestamp=DateTimeOffset.UtcNow});
    public async Task<IReadOnlyList<BudgetDto>> List(Guid condo,Guid? period,CancellationToken ct)
    {await Scope(condo,ct);var query=db.Budgets.Where(b=>b.CondominiumId==condo);if(period.HasValue)query=query.Where(b=>b.AccountingPeriodId==period);var result=new List<BudgetDto>();foreach(var b in await query.OrderByDescending(b=>b.Version).ToListAsync(ct))result.Add(await Dto(b,ct));return result;}
    public Task<BudgetDto> Save(Guid condo,Guid? id,BudgetInput input,CancellationToken ct)=>Write(condo,async()=>{
        var period=await Period(condo,input.AccountingPeriodId,ct);
        if(input.Lines==null || input.Lines.Count is <1 or >100) throw new ArgumentException("Da 1 a 100 voci richieste.");
        Budget b;
        if(id.HasValue) {b=await Load(condo,id.Value,ct);b.EnsureDraft();if(b.AccountingPeriodId!=period.Id || b.PreviousVersionId!=input.PreviousVersionId) throw new ArgumentException("Non cambiare esercizio o origine della versione.");b.Update(input.Description,input.Notes);db.RemoveRange(await db.BudgetLines.Where(l=>l.BudgetId==b.Id).ToListAsync(ct));}
        else {
            Budget? previous=input.PreviousVersionId.HasValue?await Load(condo,input.PreviousVersionId.Value,ct):null;
            if(previous!=null && previous.AccountingPeriodId!=period.Id)throw new ArgumentException("Versione di un altro esercizio.");
            var latest=await db.Budgets.Where(x=>x.CondominiumId==condo && x.AccountingPeriodId==period.Id).MaxAsync(x=>(int?)x.Version,ct)??0;
            if(latest>0 && (previous==null || previous.Version!=latest))throw new InvalidOperationException("Selezionare l'ultima versione come origine.");
            b=new(Tenant,condo,period,input.Description,latest+1,previous?.Id,input.Notes);db.Add(b);
        }
        foreach(var (line,i) in input.Lines.Select((l,i)=>(l,i)))
        {
            if(!await db.Accounts.AnyAsync(a=>a.Id==line.AccountId && a.CondominiumId==condo && a.Type==AccountType.Expense && a.IsActive,ct))throw new ArgumentException("Categoria di spesa non valida per il condominio.");
            if(!await db.AllocationRules.AnyAsync(r=>r.Id==line.RuleId && r.CondominiumId==condo && r.Status==AllocationRuleStatus.Active,ct))throw new ArgumentException("Regola attiva del condominio obbligatoria.");
            db.Add(new BudgetLine(Tenant,condo,b.Id,line.AccountId,line.RuleId,i,line.Description,line.Amount,Serialize(line),line.Notes));
        }
        Money.Positive(input.Lines.Sum(l=>l.Amount));Audit("Budget.Saved",b.Id);await db.SaveChangesAsync(ct);return await Dto(b,ct);
    },ct);
    public Task<BudgetDto> Propose(Guid condo,Guid id,CancellationToken ct)=>Write(condo,async()=>{var b=await Load(condo,id,ct);await Period(condo,b.AccountingPeriodId,ct);if(!await db.BudgetLines.AnyAsync(l=>l.BudgetId==id,ct))throw new ArgumentException("Preventivo vuoto.");b.Propose();Audit("Budget.Proposed",id);return await Dto(b,ct);},ct);
    private async Task<BudgetPreviewDto> Calculate(Guid condo,Budget b,bool locks,CancellationToken ct)
    {
        var p=await Period(condo,b.AccountingPeriodId,ct);var units=await db.PropertyUnits.Where(u=>u.CondominiumId==condo && u.IsActive).OrderBy(u=>u.Code).ThenBy(u=>u.Id).ToListAsync(ct);
        if(units.Count is <1 or >2000)throw new ArgumentException("Da 1 a 2000 unita attive richieste.");
        var lines=await db.BudgetLines.Where(l=>l.BudgetId==b.Id).OrderBy(l=>l.Position).ToListAsync(ct);var sources=new List<ResolvedAllocationSource>();
        foreach(var l in lines)
        {
            if(!await db.Accounts.AnyAsync(a=>a.Id==l.AccountId && a.CondominiumId==condo && a.IsActive && a.Type==AccountType.Expense,ct))throw new InvalidOperationException("Categoria disattivata.");
            var parameters=Read<BudgetLineInput>(l.ParametersJson);
            var (rule,components)=await resolver.ResolveCriteria(condo,p,l.RuleId,p.StartsOn,p.EndsOn,units,parameters.ExcludedUnitIds,parameters.DirectCharges,locks,ct);
            sources.Add(new(l.Position,Tenant,condo,l.Id,l.Description,l.Amount,0,rule.Id,rule.Name,rule.Version,components,AllocationSourceKind.BudgetLine));
        }
        var snapshot=new AllocationCalculationInput(Tenant,condo,p.Id,p.StartsOn,p.EndsOn,sources);var result=engine.Calculate(snapshot);
        return new(Hash(new{Actor=current.UserId,Budget=b.Id,b.Version,b.Description,b.Notes,Lines=lines.Select(l=>l.ParametersJson),Snapshot=snapshot,Engine=AllocationEngine.Version}),snapshot,result);
    }
    public async Task<BudgetPreviewDto> Preview(Guid condo,Guid id,CancellationToken ct)
    {await Scope(condo,ct);await using var tx=db.Database.CurrentTransaction==null?await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,ct):null;var b=await Load(condo,id,ct);var result=b.AllocationSnapshotJson==null?await Calculate(condo,b,false,ct):Read<BudgetPreviewDto>(b.AllocationSnapshotJson);if(tx!=null)await tx.CommitAsync(ct);return result;}
    public Task<BudgetDto> Approve(Guid condo,Guid id,BudgetApprovalInput input,CancellationToken ct)=>Write(condo,async()=>{
        current.RequireAdmin();var b=await Load(condo,id,ct);
        if(b.Status==BudgetStatus.Approved) {var saved=Read<BudgetPreviewDto>(b.AllocationSnapshotJson!);if(saved.Hash==input.PreviewHash && b.DecisionReference==Guard.Optional(input.DecisionReference))return await Dto(b,ct);throw new InvalidOperationException("Approvazione gia eseguita con parametri diversi.");}
        if(b.Status!=BudgetStatus.Proposed)throw new InvalidOperationException("Proporre il preventivo prima dell'approvazione.");
        var preview=await Calculate(condo,b,true,ct);if(preview.Hash!=input.PreviewHash)throw new InvalidOperationException("Fonti modificate: ripetere la simulazione.");if(!preview.Result.IsValid)throw new ArgumentException(string.Join(" ",preview.Result.Errors));
        var approved=await db.Budgets.SingleOrDefaultAsync(x=>x.CondominiumId==condo && x.AccountingPeriodId==b.AccountingPeriodId && x.Status==BudgetStatus.Approved,ct);
        if(approved!=null) {if(approved.Version>=b.Version)throw new InvalidOperationException("Una versione successiva e gia approvata.");approved.Supersede();await db.SaveChangesAsync(ct);}
        b.Approve(current.UserId!.Value,Serialize(preview),input.DecisionReference);Audit("Budget.Approved",b.Id);return await Dto(b,ct);
    },ct);
}
