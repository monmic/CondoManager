using System.Net;
using System.Net.Http.Json;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CondoManager.IntegrationTests;
[Collection("PostgreSQL")]
public class AllocationApiTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json=SecurityTestHost.Json;
    private sealed record Context(Guid? TenantId) : ICurrentTenant {public Guid? UserId=>TenantId;public StudioRole? Role=>StudioRole.StudioAdmin;}
    private static AppDbContext Db(Guid? tenant=null)=>new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")).Options,new Context(tenant));
    private static async Task<Guid> Tenant() {var id=Guid.NewGuid();await using var db=Db();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Tenants\" (\"Id\",\"Code\",\"Name\",\"CreatedAt\",\"UpdatedAt\") VALUES ({id},{id.ToString()},{"Synthetic allocation studio"},now(),now())");return id;}
    private static async Task<T> Send<T>(HttpClient c,string path,object body,string method="POST") {using var r=await c.SendAsync(new(new HttpMethod(method),path){Content=JsonContent.Create(body,options:Json)});Assert.True(r.IsSuccessStatusCode,$"{path}: {r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return (await r.Content.ReadFromJsonAsync<T>(Json))!;}
    private static async Task<HttpStatusCode> Status(HttpClient c,string path,object body,string method="POST") {using var r=await c.SendAsync(new(new HttpMethod(method),path){Content=JsonContent.Create(body,options:Json)});return r.StatusCode;}
    private static async Task<T> Get<T>(HttpClient c,string path)=>(await c.GetFromJsonAsync<T>(path,Json))!;
    private sealed record Fixture(Guid Tenant,SecurityTestHost Host,HttpClient Client,Guid Condo,Guid Period,Guid[] Units,Guid Expense,Guid ExpenseAccount) : IAsyncDisposable
    {
        public string Base=>"/api/allocations/condominiums/"+Condo;
        public async ValueTask DisposeAsync() {Client.Dispose();await Host.DisposeAsync();}
        public AllocationPreviewInput Input(Guid? rule,decimal amount=100)=>new(Period,new(2026,1,1),new(2026,12,31),[new(Expense,amount,rule)]);
    }
    private static async Task<Fixture> Setup(decimal amount=100)
    {
        var t=await Tenant();var host=new SecurityTestHost(t);var c=await host.CreateAuthenticatedClient();
        var condo=await Send<CondominiumDto>(c,"/api/condominiums",new CondominiumInput("C","Synthetic allocation condominium",null,"Synthetic","00000","Test","XX","Test"));var path="/api/condominiums/"+condo.Id;
        var a=await Send<StaircaseDto>(c,path+"/staircases",new StaircaseInput("A","Synthetic staircase A",null));var b=await Send<StaircaseDto>(c,path+"/staircases",new StaircaseInput("B","Synthetic staircase B",null));
        var units=new List<Guid>();for(var i=0;i<3;i++) units.Add((await Send<PropertyUnitDto>(c,path+"/units",new PropertyUnitInput(((char)('A'+i)).ToString(),null,i<2?a.Id:b.Id,null,null,PropertyUnitKind.Apartment,null,null))).Id);
        var p=await Send<AccountingPeriodDto>(c,path+"/accounting-periods",new AccountingPeriodInput("E","Synthetic period",new(2026,1,1),new(2026,12,31)));await Send<AccountingPeriodDto>(c,path+$"/accounting-periods/{p.Id}/open",new{});
        var supplier=await Send<SupplierDto>(c,"/api/accounting/suppliers",new SupplierInput("Synthetic allocation supplier",null,null,null,null,null,null,null));
        var ledger="/api/accounting/condominiums/"+condo.Id;var account=(await Get<AccountDto[]>(c,ledger+"/accounts")).First(x=>x.Type==AccountType.Expense);
        var expense=await Send<ExpenseDto>(c,ledger+"/expenses",new ExpenseInput(p.Id,supplier.Id,account.Id,"SYN-ALLOC",new(2026,8,1),null,"Synthetic expense to allocate",amount,0,amount,null));await Send<JournalDto>(c,ledger+$"/expenses/{expense.Id}/register",new PostingInput(Guid.NewGuid()));
        return new(t,host,c,condo.Id,p.Id,units.ToArray(),expense.Id,account.Id);
    }
    private static RuleComponentInput Component(AllocationCriterion criterion=AllocationCriterion.EqualShares,decimal percent=100,Guid? table=null,Guid? group=null,bool normalize=false)=>new(criterion,percent,table,group,criterion==AllocationCriterion.Consumption?"COLDWATER":null,criterion==AllocationCriterion.Consumption?"M3":null,normalize);
    private static async Task<RuleDto> Rule(Fixture f,AllocationCriterion type=AllocationCriterion.EqualShares,params RuleComponentInput[] components)
    {var row=await Send<RuleDto>(f.Client,f.Base+"/rules",new RuleInput("Synthetic "+Guid.NewGuid().ToString("N"),null,type,components.Length==0?[Component(type)]:components));return await Send<RuleDto>(f.Client,f.Base+$"/rules/{row.Id}/activate",new{});}
    private static async Task<Guid> Table(Fixture f)
    {var p=$"/api/condominiums/{f.Condo}/allocation-tables";var table=await Send<AllocationTableDto>(f.Client,p,new AllocationTableInput("T","Synthetic allocation table",1000,new(2026,1,1)));for(var i=0;i<3;i++) await Send<AllocationTableDto>(f.Client,p+$"/{table.Id}/entries/{f.Units[i]}",new AllocationEntryInput(new decimal[]{500,300,200}[i]),"PUT");await Send<AllocationTableDto>(f.Client,p+$"/{table.Id}/activate",new{});return table.Id;}
    private static Task<AllocationPreviewDto> Preview(Fixture f,AllocationPreviewInput input)=>Send<AllocationPreviewDto>(f.Client,f.Base+"/preview",input);
    private static Task<AllocationRunDto> Confirm(Fixture f,AllocationPreviewInput input,AllocationPreviewDto preview,Guid? request=null)=>Send<AllocationRunDto>(f.Client,f.Base+"/confirm",new AllocationConfirmInput(request??Guid.NewGuid(),preview.PreviewHash,input));
    private static Task<ReadingDto> Reading(Fixture f,int unit,ReadingBoundary boundary,decimal value,string meter="M",DateOnly? date=null,bool estimated=false,Guid? previous=null)=>Send<ReadingDto>(f.Client,f.Base+"/readings",new ReadingInput(f.Units[unit],f.Period,"COLDWATER",meter,"M3",date??(boundary==ReadingBoundary.Initial?new(2026,1,1):new(2026,12,31)),boundary,value,estimated,estimated||previous.HasValue?"Synthetic estimate/correction":null,previous));
    [PostgresFact] public async Task EqualSharesConfirmIsIdempotentImmutableAndDoesNotChangeAccounting()
    {
        await using var f=await Setup();var rule=await Rule(f);var input=f.Input(rule.Id);var preview=await Preview(f,input);Assert.True(preview.Result.IsValid);Assert.Equal(new decimal[]{33.34m,33.33m,33.33m},preview.Result.Lines.Select(l=>l.Amount));
        Assert.Empty(await Get<AllocationRunDto[]>(f.Client,f.Base+"/runs"));var key=Guid.NewGuid();var run=await Confirm(f,input,preview,key);Assert.Equal(run.Id,(await Confirm(f,input,preview,key)).Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),preview.PreviewHash,input)));
        Assert.Single(await Get<JournalDto[]>(f.Client,$"/api/accounting/condominiums/{f.Condo}/journals"));
        await using var db=Db(f.Tenant);Assert.Equal(100,await db.AllocationRunLines.Where(l=>l.RunId==run.Id).SumAsync(l=>l.Amount));Assert.Single(await db.SecurityAudits.Where(x=>x.TenantId==f.Tenant && x.Event=="Allocation.RunConfirmed").ToListAsync());
        await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AllocationRunLines\" SET \"Amount\"={1m} WHERE \"RunId\"={run.Id}"));
        await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"AllocationRuns\" WHERE \"Id\"={run.Id}"));
    }
    [PostgresFact] public async Task StaircaseGroupExplicitNormalizationAndExcludedUnits()
    {
        await using var f=await Setup(1000);var table=await Table(f);var group=await Send<GroupDto>(f.Client,f.Base+"/groups",new GroupInput("Scala A",true,[f.Units[0],f.Units[1]]));
        var strict=await Rule(f,AllocationCriterion.Millesimal,Component(AllocationCriterion.Millesimal,100,table,group.Id));var invalid=await Preview(f,f.Input(strict.Id,1000));Assert.False(invalid.Result.IsValid);Assert.Contains(invalid.Result.Errors,e=>e.Contains("normalizzazione"));
        var normalized=await Rule(f,AllocationCriterion.Millesimal,Component(AllocationCriterion.Millesimal,100,table,group.Id,true));var result=await Preview(f,f.Input(normalized.Id,1000));Assert.Equal(new decimal[]{625,375},result.Result.Lines.Select(l=>l.Amount));Assert.Single(result.Result.Warnings);Assert.Equal(800,result.Result.Lines[0].EffectiveDenominator);Assert.Equal(1000,result.Result.Lines[0].NominalDenominator);
        var input=f.Input(normalized.Id,1000);input=input with{Sources=[input.Sources[0] with{ExcludedUnitIds=[f.Units[1]]}]};var excluded=await Preview(f,input);Assert.Single(excluded.Result.Lines);Assert.Equal(1000,excluded.Result.Lines[0].Amount);
        var standard=await Rule(f,AllocationCriterion.Millesimal,Component(AllocationCriterion.Millesimal,100,table));Assert.Equal(new decimal[]{500,300,200},(await Preview(f,f.Input(standard.Id,1000))).Result.Lines.Select(l=>l.Amount));
    }
    [PostgresFact] public async Task MixedReferenceCAndConsumptionSnapshots()
    {
        await using var f=await Setup(1000);var table=await Table(f);for(var i=0;i<3;i++){await Reading(f,i,ReadingBoundary.Initial,100);await Reading(f,i,ReadingBoundary.Final,100+new decimal[]{10,20,40}[i]);}
        var rule=await Rule(f,AllocationCriterion.Mixed,Component(AllocationCriterion.Millesimal,30,table),Component(AllocationCriterion.Consumption,70));var input=f.Input(rule.Id,1000);var p=await Preview(f,input);Assert.Equal(new decimal[]{250,290,460},p.Result.Lines.GroupBy(l=>l.UnitCode).Select(g=>g.Sum(l=>l.Amount)));Assert.Equal(0,p.Result.Difference);var run=await Confirm(f,input,p);Assert.Contains("initialReadingId",run.Snapshot.Sources[0].Components[1].EvidenceJson);Assert.Equal(1000,run.Result.AllocatedAmount);
    }
    [PostgresFact] public async Task CategoryDocumentAndQuotaPrecedencePreventsDoubleAllocation()
    {
        await using var f=await Setup();var equal=await Rule(f);var direct=await Rule(f,AllocationCriterion.DirectCharge);var group=await Send<GroupDto>(f.Client,f.Base+"/groups",new GroupInput("Only A",true,[f.Units[0]]));var single=await Rule(f,AllocationCriterion.EqualShares,Component(group:group.Id));
        await Send<AssignmentDto>(f.Client,f.Base+"/assignments",new AssignmentInput(f.ExpenseAccount,null,equal.Id),"PUT");await Send<AssignmentDto>(f.Client,f.Base+"/assignments",new AssignmentInput(null,f.Expense,single.Id),"PUT");
        var input=f.Input(null);input=input with{Sources=[new(f.Expense,60,null),new(f.Expense,40,direct.Id,null,[new(f.Units[2],null,40)])]};var p=await Preview(f,input);Assert.Equal(new decimal[]{60,40},p.Result.Lines.Select(l=>l.Amount));await Confirm(f,input,p);Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",f.Input(equal.Id,1)));
    }
    [PostgresFact] public async Task GroupChangesInvalidatePreviewAndRuleVersionsDoNotRewriteHistory()
    {
        await using var f=await Setup();var group=await Send<GroupDto>(f.Client,f.Base+"/groups",new GroupInput("Synthetic group",true,[f.Units[0],f.Units[1]]));var rule=await Rule(f,AllocationCriterion.EqualShares,Component(group:group.Id));var input=f.Input(rule.Id);var old=await Preview(f,input);
        await Send<GroupDto>(f.Client,f.Base+"/groups/"+group.Id,new GroupInput(group.Name,true,[f.Units[0],f.Units[2]]),"PUT");Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),old.PreviewHash,input)));
        var current=await Preview(f,input);var run=await Confirm(f,input,current);var clone=await Send<RuleDto>(f.Client,f.Base+"/rules/"+rule.Id,new RuleInput(rule.Name,"New description",AllocationCriterion.EqualShares,[Component()]),"PUT");Assert.NotEqual(rule.Id,clone.Id);Assert.Equal(2,clone.Version);Assert.Equal(AllocationRuleStatus.Draft,clone.Status);
        await Send<RuleDto>(f.Client,f.Base+"/rules/"+clone.Id,new RuleInput(rule.Name,"Edited draft",AllocationCriterion.EqualShares,[Component(group:group.Id)]),"PUT");await Send<RuleDto>(f.Client,f.Base+$"/rules/{clone.Id}/activate",new{});
        var history=await Get<AllocationRunDto>(f.Client,f.Base+"/runs/"+run.Id);Assert.Equal(1,history.Snapshot.Sources[0].RuleVersion);Assert.DoesNotContain(history.Result.Lines,l=>l.PropertyUnitId==f.Units[1]);
        await using var db=Db(f.Tenant);await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AllocationRules\" SET \"Name\"={"Tampered"} WHERE \"Id\"={rule.Id}"));
    }
    [PostgresFact] public async Task MissingReadingsAndNegativeConsumptionAreExplicitErrors()
    {
        await using var f=await Setup();var rule=await Rule(f,AllocationCriterion.Consumption);Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",f.Input(rule.Id)));
        await Reading(f,0,ReadingBoundary.Initial,100);
        Assert.Equal(HttpStatusCode.BadRequest,await Status(f.Client,f.Base+"/readings",new ReadingInput(f.Units[0],f.Period,"COLDWATER","M","M3",new(2026,12,31),ReadingBoundary.Final,99,false,null,null)));
        var consumption=await Get<UnitConsumptionDto[]>(f.Client,f.Base+$"/consumptions?period={f.Period}&type=COLDWATER&unitOfMeasure=M3&from=2026-01-01&to=2026-12-31");Assert.All(consumption,x=>Assert.Null(x.Result.Consumption));
    }
    [PostgresFact] public async Task ReplacementMetersEstimatesCorrectionsAndRunVersionsPreserveSnapshots()
    {
        await using var f=await Setup();var group=await Send<GroupDto>(f.Client,f.Base+"/groups",new GroupInput("Only A",true,[f.Units[0]]));var rule=await Rule(f,AllocationCriterion.Consumption,Component(AllocationCriterion.Consumption,group:group.Id));
        await Reading(f,0,ReadingBoundary.Initial,100,"OLD");await Reading(f,0,ReadingBoundary.Final,130,"OLD",new(2026,6,30));await Reading(f,0,ReadingBoundary.Initial,0,"NEW",new(2026,6,30));var final=await Reading(f,0,ReadingBoundary.Final,40,"NEW",estimated:true);
        var input=f.Input(rule.Id);var p=await Preview(f,input);Assert.Single(p.Result.Warnings);Assert.Equal(70,p.Result.Lines[0].Weight);var run=await Confirm(f,input,p);
        var replacement=input with{PreviousRunId=run.Id};var stale=await Preview(f,replacement);await Reading(f,0,ReadingBoundary.Final,50,"NEW",estimated:true,previous:final.Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),stale.PreviewHash,replacement)));
        var updated=await Preview(f,replacement);Assert.Equal(80,updated.Result.Lines[0].Weight);var version2=await Confirm(f,replacement,updated);Assert.Equal(2,version2.Version);Assert.Equal(run.Id,version2.PreviousRunId);
        var original=await Get<AllocationRunDto>(f.Client,f.Base+"/runs/"+run.Id);Assert.Equal(70,original.Result.Lines[0].Weight);Assert.Equal(version2.Id,original.ReplacedByRunId);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",f.Input(rule.Id,1)));
        await using var db=Db(f.Tenant);await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"ConsumptionReadings\" SET \"Value\"={9m} WHERE \"Id\"={final.Id}"));
    }
    [PostgresFact] public async Task DirectPersonalChargeUsesHistoricalSubjectNotCurrentOwner()
    {
        await using var f=await Setup();var old=await Send<PersonDto>(f.Client,"/api/people",new PersonInput("OLD",PersonKind.NaturalPerson,"Synthetic","Former subject","SYN-OLD",null,null,null,null,null));var now=await Send<PersonDto>(f.Client,"/api/people",new PersonInput("NEW",PersonKind.NaturalPerson,"Synthetic","Current subject","SYN-NEW",null,null,null,null,null));
        var p=$"/api/condominiums/{f.Condo}/units/{f.Units[0]}/associations";await Send<AssociationDto>(f.Client,p,new AssociationInput(old.Id,UnitPersonRole.Owner,new(2026,1,1),new(2026,6,30),1));await Send<AssociationDto>(f.Client,p,new AssociationInput(now.Id,UnitPersonRole.Owner,new(2026,7,1),null,1));
        var rule=await Rule(f,AllocationCriterion.DirectCharge);var input=new AllocationPreviewInput(f.Period,new(2026,1,1),new(2026,6,30),[new(f.Expense,100,rule.Id,null,[new(f.Units[0],old.Id,100)])]);var result=await Preview(f,input);Assert.Equal(old.Id,result.Result.Lines.Single().PersonId);var run=await Confirm(f,input,result);Assert.Equal(old.DisplayName,run.Result.Lines.Single().PersonName);
        var invalid=input with{PreviousRunId=run.Id,Sources=[new(f.Expense,100,rule.Id,null,[new(f.Units[0],now.Id,100)])]};Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",invalid));
    }
    [PostgresFact] public async Task ConcurrentConfirmationsUseOneBudgetAndSameRequestIsIdempotent()
    {
        await using var f=await Setup();var rule=await Rule(f);var input=f.Input(rule.Id);var p=await Preview(f,input);var key=Guid.NewGuid();var replies=await Task.WhenAll(Confirm(f,input,p,key),Confirm(f,input,p,key));Assert.Equal(replies[0].Id,replies[1].Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),p.PreviewHash,input)));Assert.Single(await Get<AllocationRunDto[]>(f.Client,f.Base+"/runs"));
    }
    [PostgresFact] public async Task ClosedPeriodBlocksNewCalculationsButConfirmedHistoryAndRetriesRemainReadable()
    {
        await using var f=await Setup();var rule=await Rule(f);var input=f.Input(rule.Id);var p=await Preview(f,input);var key=Guid.NewGuid();var run=await Confirm(f,input,p,key);
        await AdministrativeTestFixture.Close(f.Client,f.Condo,f.Period);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",input));Assert.Equal(run.Id,(await Confirm(f,input,p,key)).Id);Assert.Equal(run.Id,(await Get<AllocationRunDto>(f.Client,f.Base+"/runs/"+run.Id)).Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/readings",new ReadingInput(f.Units[0],f.Period,"COLDWATER",null,"M3",new(2026,1,1),ReadingBoundary.Initial,0,false,null,null)));
    }
    [PostgresFact] public async Task TenantCondominiumRolesAndCsrfProtectEveryAllocationFlow()
    {
        await using var f=await Setup();await using var foreign=await Setup();var rule=await Rule(f);var remoteRule=await Rule(foreign);var table=await Table(foreign);
        Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,f.Base+"/preview",f.Input(remoteRule.Id)));
        Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,f.Base+"/rules",new RuleInput("Invalid",null,AllocationCriterion.Millesimal,[Component(AllocationCriterion.Millesimal,table:table)])));
        Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,f.Base+"/groups",new GroupInput("Foreign",true,[foreign.Units[0]])));
        var otherCondo=await Send<CondominiumDto>(f.Client,"/api/condominiums",new CondominiumInput("OTHER","Synthetic other",null,"Test","00000","Test","XX","Test"));
        Assert.Equal(HttpStatusCode.NotFound,await Status(f.Client,$"/api/allocations/condominiums/{otherCondo.Id}/assignments",new AssignmentInput(f.ExpenseAccount,null,rule.Id),"PUT"));
        var preview=await Preview(f,f.Input(rule.Id));var run=await Confirm(f,f.Input(rule.Id),preview);
        Assert.Equal(HttpStatusCode.NotFound,(await foreign.Client.GetAsync(f.Base+"/runs/"+run.Id)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await f.Client.GetAsync($"/api/allocations/condominiums/{otherCondo.Id}/runs/{run.Id}")).StatusCode);
        using var viewer=f.Host.Browser();await SecurityTestHost.Login(viewer,await f.Host.Account(f.Tenant,StudioRole.Viewer),f.Tenant);Assert.Equal(HttpStatusCode.OK,(await viewer.GetAsync(f.Base+"/runs")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,await Status(viewer,f.Base+"/confirm",new AllocationConfirmInput(Guid.NewGuid(),preview.PreviewHash,f.Input(rule.Id))));
        Assert.Equal(HttpStatusCode.Forbidden,await Status(viewer,f.Base+"/groups",new GroupInput("Denied",true,[f.Units[0]])));viewer.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");Assert.Equal(HttpStatusCode.BadRequest,await Status(viewer,f.Base+"/preview",f.Input(rule.Id)));
        using var anon=f.Host.Browser();Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(f.Base+"/rules")).StatusCode);
    }
    [PostgresFact] public async Task DatabaseDeferredConstraintsRejectIncompleteRunAndRollBack()
    {
        await using var f=await Setup();var id=Guid.NewGuid();await using(var db=Db(f.Tenant)) {await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"AllocationRuns\" (\"Id\",\"TenantId\",\"CondominiumId\",\"AccountingPeriodId\",\"RequestId\",\"RequestHash\",\"PreviewHash\",\"EngineVersion\",\"Version\",\"ConfirmedByUserId\",\"CompetenceFrom\",\"CompetenceTo\",\"TotalAmount\",\"InputJson\",\"SnapshotJson\",\"CreatedAt\",\"UpdatedAt\") VALUES ({id},{f.Tenant},{f.Condo},{f.Period},{Guid.NewGuid()},{"hash"},{"hash"},{AllocationEngine.Version},1,{Guid.NewGuid()},{new DateOnly(2026,1,1)},{new DateOnly(2026,12,31)},100,'{{}}'::jsonb,'{{}}'::jsonb,now(),now())");var e=await Assert.ThrowsAsync<PostgresException>(()=>tx.CommitAsync());Assert.Equal(PostgresErrorCodes.CheckViolation,e.SqlState);}
        Assert.Empty(await Get<AllocationRunDto[]>(f.Client,f.Base+"/runs"));
    }
    [PostgresFact] public async Task LaterAccountingCancellationIsVisibleWithoutRewritingHistoricalAllocation()
    {
        await using var f=await Setup();var rule=await Rule(f);var input=f.Input(rule.Id);var preview=await Preview(f,input);var run=await Confirm(f,input,preview);
        var ledger=$"/api/accounting/condominiums/{f.Condo}";var journal=Assert.Single(await Get<JournalDto[]>(f.Client,ledger+"/journals"));
        await Send<JournalDto>(f.Client,ledger+$"/journals/{journal.Id}/reverse",new ReversalInput(Guid.NewGuid(),f.Period,new(2026,8,2),"Synthetic cancelled expense"));
        var history=await Get<AllocationRunDto>(f.Client,f.Base+"/runs/"+run.Id);Assert.Single(history.SourceWarnings);Assert.Equal(100,history.Result.AllocatedAmount);Assert.Equal(run.PreviewHash,history.PreviewHash);
        Assert.Equal(HttpStatusCode.Conflict,await Status(f.Client,f.Base+"/preview",input));
    }}
