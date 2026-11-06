using System.Net;
using System.Net.Http.Json;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CondoManager.IntegrationTests;
[Collection("PostgreSQL")]
public class BudgetApiTests
{
    private sealed record Context(Guid? TenantId) : ICurrentTenant {public Guid? UserId=>TenantId;public StudioRole? Role=>StudioRole.StudioAdmin;}
    private static AppDbContext Db(Guid? tenant=null)=>new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")).Options,new Context(tenant));
    private static async Task<T> Send<T>(HttpClient c,string path,object body,string method="POST") {using var r=await c.SendAsync(new(new HttpMethod(method),path){Content=JsonContent.Create(body,options:SecurityTestHost.Json)});Assert.True(r.IsSuccessStatusCode,$"{path}: {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return (await r.Content.ReadFromJsonAsync<T>(SecurityTestHost.Json))!;}
    private static async Task<HttpStatusCode> Status(HttpClient c,string path,object body,string method="POST") {using var r=await c.SendAsync(new(new HttpMethod(method),path){Content=JsonContent.Create(body,options:SecurityTestHost.Json)});return r.StatusCode;}
    internal sealed record Fixture(Guid Tenant,SecurityTestHost Host,HttpClient Client,Guid Condo,Guid Period,Guid Account,Guid Rule) : IAsyncDisposable
    {
        public string Base=>"/api/budgets/condominiums/"+Condo;
        public BudgetInput Input(decimal amount=12000,Guid? previous=null)=>new(Period,"Synthetic budget",[new(Account,"Synthetic expense",amount,Rule)],null,previous);
        public async ValueTask DisposeAsync(){Client.Dispose();await Host.DisposeAsync();}
    }
    internal static async Task<Fixture> Setup()
    {
        var tenant=Guid.NewGuid();await using(var db=Db())await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Tenants\" (\"Id\",\"Code\",\"Name\",\"CreatedAt\",\"UpdatedAt\") VALUES ({tenant},{tenant.ToString()},{"Synthetic budget studio"},now(),now())");
        var host=new SecurityTestHost(tenant);var c=await host.CreateAuthenticatedClient();var condo=await Send<CondominiumDto>(c,"/api/condominiums",new CondominiumInput("C","Synthetic condominium",null,"Synthetic","00000","Test","XX","Test"));var path="/api/condominiums/"+condo.Id;
        for(var i=0;i<3;i++)await Send<PropertyUnitDto>(c,path+"/units",new PropertyUnitInput(((char)('A'+i)).ToString(),null,null,null,null,PropertyUnitKind.Apartment,null,null));
        var p=await Send<AccountingPeriodDto>(c,path+"/accounting-periods",new AccountingPeriodInput("E","Synthetic period",new(2026,1,1),new(2026,12,31)));await Send<AccountingPeriodDto>(c,path+$"/accounting-periods/{p.Id}/open",new{});
        var allocationBase="/api/allocations/condominiums/"+condo.Id;var rule=await Send<RuleDto>(c,allocationBase+"/rules",new RuleInput("Equal",null,AllocationCriterion.EqualShares,[new(AllocationCriterion.EqualShares,100,null,null,null,null)]));await Send<RuleDto>(c,allocationBase+$"/rules/{rule.Id}/activate",new{});
        await using var data=Db(tenant);var account=await data.Accounts.Where(a=>a.CondominiumId==condo.Id && a.Type==AccountType.Expense).Select(a=>a.Id).FirstAsync();return new(tenant,host,c,condo.Id,p.Id,account,rule.Id);
    }
    [PostgresFact] public async Task BudgetApprovalVersionsSnapshotsAndNoArtificialAccounting()
    {
        await using var f=await Setup();var b=await Send<BudgetDto>(f.Client,f.Base,f.Input(95601.81m));await Send<BudgetDto>(f.Client,f.Base+$"/{b.Id}/propose",new{});
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+$"/{b.Id}",f.Input(),"PUT"));
        var next=await Send<BudgetDto>(f.Client,f.Base,f.Input(106733.48m,b.Id));Assert.Equal(2,next.Version);Assert.Equal(11131.67m,next.Difference);
        await Send<BudgetDto>(f.Client,f.Base+$"/{next.Id}/propose",new{});var preview=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{next.Id}/preview",new{});Assert.True(preview.Result.IsValid);Assert.Equal(106733.48m,preview.Result.Lines.Sum(l=>l.Amount));Assert.All(preview.Snapshot.Sources,s=>Assert.Equal(AllocationSourceKind.BudgetLine,s.SourceKind));
        var approved=await Send<BudgetDto>(f.Client,f.Base+$"/{next.Id}/approve",new BudgetApprovalInput(preview.Hash));Assert.Equal(BudgetStatus.Approved,approved.Status);Assert.NotNull(approved.ApprovedBy);Assert.Equal(approved.Id,(await Send<BudgetDto>(f.Client,f.Base+$"/{next.Id}/approve",new BudgetApprovalInput(preview.Hash))).Id);
        await using var db=Db(f.Tenant);Assert.Empty(await db.ExpenseDocuments.ToListAsync());Assert.Empty(await db.JournalEntries.ToListAsync());
        await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"BudgetLines\" SET \"Amount\"={1m} WHERE \"BudgetId\"={approved.Id}"));await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Budgets\" WHERE \"Id\"={approved.Id}"));
        var v3=await Send<BudgetDto>(f.Client,f.Base,f.Input(120000,next.Id));await Send<BudgetDto>(f.Client,f.Base+$"/{v3.Id}/propose",new{});var p3=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{v3.Id}/preview",new{});await Send<BudgetDto>(f.Client,f.Base+$"/{v3.Id}/approve",new BudgetApprovalInput(p3.Hash));db.ChangeTracker.Clear();Assert.Equal(BudgetStatus.Superseded,(await db.Budgets.SingleAsync(x=>x.Id==approved.Id)).Status);Assert.Single(await db.Budgets.Where(x=>x.Status==BudgetStatus.Approved).ToListAsync());
    }
    [PostgresFact] public async Task AnonymousViewerCollaboratorAndCrossScopeAreDenied()
    {
        await using var f=await Setup();using var anonymous=f.Host.Browser();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(f.Base)).StatusCode);
        var b=await Send<BudgetDto>(f.Client,f.Base,f.Input());await Send<BudgetDto>(f.Client,f.Base+$"/{b.Id}/propose",new{});var preview=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{b.Id}/preview",new{});
        foreach(var role in new[]{StudioRole.Viewer,StudioRole.Collaborator}) {using var c=f.Host.Browser();await SecurityTestHost.Login(c,await f.Host.Account(f.Tenant,role),f.Tenant);Assert.Equal(HttpStatusCode.Forbidden,await Status(c,f.Base+$"/{b.Id}/approve",new BudgetApprovalInput(preview.Hash)));if(role==StudioRole.Viewer)Assert.Equal(HttpStatusCode.Forbidden,await Status(c,f.Base,f.Input()));}
        await using var other=await Setup();Assert.Equal(HttpStatusCode.NotFound,await Status(other.Client,f.Base+$"/{b.Id}/preview",new{}));
        var second=await Send<CondominiumDto>(f.Client,"/api/condominiums",new CondominiumInput("OTHER","Other synthetic",null,"Synthetic","00000","Test","XX","Test"));Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,"/api/budgets/condominiums/"+second.Id+$"/{b.Id}/preview",new{}));
        Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,"/api/budgets/condominiums/"+second.Id,f.Input()));
    }
    [PostgresFact] public async Task ConcurrentApprovalOneCurrentAndChangedSourcesRejected()
    {
        await using var f=await Setup();var b=await Send<BudgetDto>(f.Client,f.Base,f.Input());await Send<BudgetDto>(f.Client,f.Base+$"/{b.Id}/propose",new{});var preview=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{b.Id}/preview",new{});
        var input=f.Input(13000,b.Id);var newer=await Send<BudgetDto>(f.Client,f.Base,input);await Send<BudgetDto>(f.Client,f.Base+$"/{newer.Id}/propose",new{});var p2=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{newer.Id}/preview",new{});
        var statuses=await Task.WhenAll(Status(f.Client,f.Base+$"/{b.Id}/approve",new BudgetApprovalInput(preview.Hash)),Status(f.Client,f.Base+$"/{newer.Id}/approve",new BudgetApprovalInput(p2.Hash)));Assert.Contains(HttpStatusCode.OK,statuses);
        await using var db=Db(f.Tenant);Assert.Single(await db.Budgets.Where(x=>x.Status==BudgetStatus.Approved).ToListAsync());
        var draft=await Send<BudgetDto>(f.Client,f.Base,f.Input(15000,newer.Id));await Send<BudgetDto>(f.Client,f.Base+$"/{draft.Id}/propose",new{});var old=await Send<BudgetPreviewDto>(f.Client,f.Base+$"/{draft.Id}/preview",new{});
        var unit=await db.PropertyUnits.FirstAsync(u=>u.CondominiumId==f.Condo);await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"PropertyUnits\" SET \"Code\"={"CHANGED"} WHERE \"Id\"={unit.Id}");Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+$"/{draft.Id}/approve",new BudgetApprovalInput(old.Hash)));
    }
    [PostgresFact] public async Task DraftEditIsAtomicAndClosedExerciseIsRejected()
    {
        await using var f=await Setup();var b=await Send<BudgetDto>(f.Client,f.Base,f.Input(100));
        var changed=await Send<BudgetDto>(f.Client,f.Base+$"/{b.Id}",f.Input(120),"PUT");Assert.Equal(120,changed.Total);
        var invalid=f.Input(200) with {Lines=[new(Guid.NewGuid(),"Invalid category",200,f.Rule)]};
        Assert.Equal(HttpStatusCode.BadRequest,await Status(f.Client,f.Base+$"/{b.Id}",invalid,"PUT"));
        var rows=(await f.Client.GetFromJsonAsync<BudgetDto[]>(f.Base,SecurityTestHost.Json))!;Assert.Equal(120,Assert.Single(rows).Total);
        await AdministrativeTestFixture.Close(f.Client,f.Condo,f.Period);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+$"/{b.Id}",f.Input(130),"PUT"));
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+$"/{b.Id}/preview",new{}));
    }}
