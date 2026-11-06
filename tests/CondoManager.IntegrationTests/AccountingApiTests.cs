using System.Net;
using System.Net.Http.Json;
using CondoManager.Application;
using CondoManager.Domain;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace CondoManager.IntegrationTests;

[Collection("PostgreSQL")]
public class AccountingApiTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json=SecurityTestHost.Json;
    private sealed record Context(Guid? TenantId) : ICurrentTenant { public Guid? UserId=>TenantId; public StudioRole? Role=>StudioRole.StudioAdmin; }
    private static AppDbContext Db(Guid? tenant=null)=>new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("CONDOMANAGER_TEST_POSTGRES")).Options,new Context(tenant));
    private static async Task<Guid> SeedTenant() { var id=Guid.NewGuid();await using var db=Db();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Tenants\" (\"Id\",\"Code\",\"Name\",\"CreatedAt\",\"UpdatedAt\") VALUES ({id},{id.ToString()},{"Synthetic accounting studio"},now(),now())");return id; }
    private static async Task<T> Send<T>(HttpClient c,string path,object body,string method="POST") { using var req=new HttpRequestMessage(new HttpMethod(method),path){Content=JsonContent.Create(body,options:Json)};using var r=await c.SendAsync(req);Assert.True(r.IsSuccessStatusCode,$"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}");return (await r.Content.ReadFromJsonAsync<T>(Json))!; }
    private static async Task<T> Get<T>(HttpClient c,string path)=>(await c.GetFromJsonAsync<T>(path,Json))!;
    private static async Task<HttpStatusCode> Status(HttpClient c,string path,object body,string method="POST") { using var req=new HttpRequestMessage(new HttpMethod(method),path){Content=JsonContent.Create(body,options:Json)};using var r=await c.SendAsync(req);return r.StatusCode; }
    private static async Task<CondominiumDto> Condo(HttpClient c,string code="C")=>await Send<CondominiumDto>(c,"/api/condominiums",new CondominiumInput(code,"Synthetic accounting condominium",null,"Synthetic address","00000","Test city","XX","Test"));
    private static string Base(Guid condo)=>"/api/accounting/condominiums/"+condo;
    private static async Task<AccountingPeriodDto> Period(HttpClient c,Guid condo,int year=2026) { var path=$"/api/condominiums/{condo}/accounting-periods";var p=await Send<AccountingPeriodDto>(c,path,new AccountingPeriodInput(year.ToString(),"Synthetic period",new(year,1,1),new(year,12,31)));return await Send<AccountingPeriodDto>(c,path+"/"+p.Id+"/open",new{}); }
    private static Task<SupplierDto> Supplier(HttpClient c)=>Send<SupplierDto>(c,"/api/accounting/suppliers",new SupplierInput("Synthetic supplier",null,null,"supplier@test.invalid",null,"Synthetic address",null,null));
    private static async Task<ExpenseDto> Expense(HttpClient c,Guid condo,Guid period,Guid supplier,decimal gross=122) { var accounts=await Get<AccountDto[]>(c,Base(condo)+"/accounts");return await Send<ExpenseDto>(c,Base(condo)+"/expenses",new ExpenseInput(period,supplier,accounts.First(x=>x.Type==AccountType.Expense).Id,Guid.NewGuid().ToString(),new(2026,8,1),null,"Synthetic invoice",gross==122?100:gross,gross==122?22:0,gross,null)); }
    private static Task<FinancialAccountDto> Bank(HttpClient c,Guid condo,Guid period,decimal opening=500)=>Send<FinancialAccountDto>(c,Base(condo)+"/financial-accounts",new FinancialAccountInput("BANK-"+Guid.NewGuid().ToString("N")[..8],"Synthetic bank "+Guid.NewGuid().ToString("N")[..8],FinancialAccountType.BankAccount,null,opening,period,new(2026,1,1),Guid.NewGuid()));
    private static Task<JournalDto> Register(HttpClient c,Guid condo,Guid expense)=>Send<JournalDto>(c,Base(condo)+$"/expenses/{expense}/register",new PostingInput(Guid.NewGuid()));

    [PostgresFact] public async Task DefaultChartSupplierCrudUniquenessAndHierarchy()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var path=Base(condo.Id);
        var chart=await Get<AccountDto[]>(c,path+"/accounts");Assert.True(chart.Length>=13);Assert.Equal(chart.Length,chart.Select(x=>x.Code).Distinct().Count());
        Assert.Equal(chart.Length,(await Send<AccountDto[]>(c,path+"/accounts/initialize",new{})).Length);
        var parent=await Send<AccountDto>(c,path+"/accounts",new AccountInput("TEST","Synthetic parent",AccountType.Expense,null,null));
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+"/accounts",new AccountInput("test","Duplicate",AccountType.Expense,null,null)));
        var child=await Send<AccountDto>(c,path+"/accounts",new AccountInput("CHILD","Synthetic child",AccountType.Expense,null,parent.Id));
        Assert.Equal(HttpStatusCode.BadRequest,await Status(c,path+"/accounts/"+parent.Id,new AccountInput(parent.Code,parent.Name,parent.Type,null,child.Id),"PUT"));
        var supplier=await Supplier(c);var updated=await Send<SupplierDto>(c,"/api/accounting/suppliers/"+supplier.Id,new SupplierInput("Synthetic updated",null,null,null,null,null,null,"Historical notes",false),"PUT");Assert.False(updated.IsActive);
        Assert.Single(await Get<SupplierDto[]>(c,"/api/accounting/suppliers?search=updated"));
    }
    [PostgresFact] public async Task InvoicePartialFullPaymentRetryBalancesAndDecimalPrecision()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var supplier=await Supplier(c);var bank=await Bank(c,condo.Id,p.Id);var e=await Expense(c,condo.Id,p.Id,supplier.Id);var path=Base(condo.Id);
        var j=await Register(c,condo.Id,e.Id);Assert.Equal(122,j.Lines.Sum(x=>x.Debit));Assert.Equal(122,j.Lines.Sum(x=>x.Credit));
        var key=Guid.NewGuid();var input=new PaymentInput(key,p.Id,bank.Id,new(2026,8,2),22.01m,"Synthetic partial");var first=await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",input);Assert.Equal(first.Id,(await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",input)).Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/expenses/{e.Id}/payments",input with {Amount=22.02m}));
        var partial=Assert.Single(await Get<ExpenseDto[]>(c,path+"/expenses"));Assert.Equal(99.99m,partial.Residual);Assert.Equal(ExpenseStatus.PartiallyPaid,partial.Status);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/expenses/{e.Id}/payments",input with {RequestId=Guid.NewGuid(),Amount=100}));
        await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",input with {RequestId=Guid.NewGuid(),Amount=99.99m});
        var paid=Assert.Single(await Get<ExpenseDto[]>(c,path+"/expenses"));Assert.Equal(ExpenseStatus.Paid,paid.Status);Assert.Equal(2,paid.Payments.Count);Assert.Equal(0,paid.Residual);
        Assert.Equal(378,(await Get<FinancialAccountDto[]>(c,path+"/financial-accounts")).Single().Balance);
        var summary=await Get<LedgerSummary>(c,path+"/summary");Assert.Equal(122,summary.Expenses);Assert.Equal(122,summary.Payments);Assert.Equal(0,summary.Payables);Assert.Equal(378,summary.Availability);
        Assert.Equal(summary.Balances.Sum(x=>x.Debit),summary.Balances.Sum(x=>x.Credit));
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/expenses/{e.Id}",new ExpenseInput(p.Id,supplier.Id,e.AccountId,"Modified",new(2026,8,1),null,"Bad edit",1,0,1,null),"PUT"));
    }
    [PostgresFact] public async Task InvalidInvoiceAndUnbalancedPostingRollbackAtomically()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var supplier=await Supplier(c);var path=Base(condo.Id);var chart=await Get<AccountDto[]>(c,path+"/accounts");var a=chart.First(x=>x.Type==AccountType.Expense);
        Assert.Equal(HttpStatusCode.BadRequest,await Status(c,path+"/expenses",new ExpenseInput(p.Id,supplier.Id,a.Id,"INVALID",new(2026,8,1),null,"Invalid",100,22,120,null)));
        Assert.Empty(await Get<ExpenseDto[]>(c,path+"/expenses"));
        var lines=new[]{new JournalLineInput(a.Id,10,0),new JournalLineInput(chart.First(x=>x.Type==AccountType.Equity && x.Purpose==AccountPurpose.General).Id,0,9)};
        var input=new JournalInput(Guid.NewGuid(),p.Id,new(2026,8,1),"Unbalanced",null,lines);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+"/journals",input));Assert.Empty(await Get<JournalDto[]>(c,path+"/journals"));
        // A valid first line followed by a foreign account must leave neither header nor first line.
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,path+"/journals",input with {RequestId=Guid.NewGuid(),Lines=[lines[0],new(Guid.NewGuid(),0,10)]}));
        Assert.Empty(await Get<JournalDto[]>(c,path+"/journals"));
        await using var db=Db(tenant);Assert.Empty(await db.SecurityAudits.Where(x=>x.TenantId==tenant && x.Event=="Accounting.ManualPosted").ToListAsync());
    }
    [PostgresFact] public async Task ConcurrentPaymentsAndSameKeyRetriesNeverOverpay()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var s=await Supplier(c);var bank=await Bank(c,condo.Id,p.Id);var e=await Expense(c,condo.Id,p.Id,s.Id);await Register(c,condo.Id,e.Id);var path=Base(condo.Id)+$"/expenses/{e.Id}/payments";
        var input=new PaymentInput(Guid.NewGuid(),p.Id,bank.Id,new(2026,8,2),80,null);
        var replies=await Task.WhenAll(Status(c,path,input),Status(c,path,input with{RequestId=Guid.NewGuid()}));Assert.Single(replies,x=>x==HttpStatusCode.OK);Assert.Single(replies,x=>x==HttpStatusCode.Conflict);
        var second=input with{RequestId=Guid.NewGuid(),Amount=42};var retries=await Task.WhenAll(Send<JournalDto>(c,path,second),Send<JournalDto>(c,path,second));Assert.Equal(retries[0].Id,retries[1].Id);Assert.Equal(2,(await Get<ExpenseDto[]>(c,Base(condo.Id)+"/expenses")).Single().Payments.Count);
    }
    [PostgresFact] public async Task ReversalsPreserveHistoryAndClosedPeriodRequiresNewOpenPeriod()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var s=await Supplier(c);var bank=await Bank(c,condo.Id,p.Id);var e=await Expense(c,condo.Id,p.Id,s.Id);var original=await Register(c,condo.Id,e.Id);var path=Base(condo.Id);
        var payment=await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",new PaymentInput(Guid.NewGuid(),p.Id,bank.Id,new(2026,8,2),22,null));
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/journals/{original.Id}/reverse",new ReversalInput(Guid.NewGuid(),p.Id,new(2026,8,3),"Synthetic correction")));
        await AdministrativeTestFixture.Close(c,condo.Id,p.Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/journals/{payment.Id}/reverse",new ReversalInput(Guid.NewGuid(),p.Id,new(2026,8,3),"Closed")));
        var p2=await Period(c,condo.Id,2027);var key=Guid.NewGuid();var reversal=await Send<JournalDto>(c,path+$"/journals/{payment.Id}/reverse",new ReversalInput(key,p2.Id,new(2027,1,2),"Synthetic payment reversal"));Assert.Equal(payment.Id,reversal.ReversesEntryId);
        Assert.Equal(0,(await Get<ExpenseDto[]>(c,path+"/expenses")).Single().PaidAmount);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/journals/{original.Id}/reverse",new ReversalInput(Guid.NewGuid(),p2.Id,new(2027,1,2),"Closed cost cannot change")));
        Assert.Equal(ExpenseStatus.Registered,(await Get<ExpenseDto[]>(c,path+"/expenses")).Single().Status);Assert.Equal(5,(await Get<JournalDto[]>(c,path+"/journals")).Length);
        Assert.Equal(500,(await Get<FinancialAccountDto[]>(c,path+"/financial-accounts")).Single().Balance);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+$"/journals/{payment.Id}/reverse",new ReversalInput(Guid.NewGuid(),p2.Id,new(2027,1,3),"Duplicate reversal")));
        await using var db=Db(tenant);await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"JournalEntries\" SET \"Description\"={"Tampered"} WHERE \"Id\"={original.Id}"));
        await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"JournalEntryLines\" WHERE \"JournalEntryId\"={original.Id}"));
    }
    [PostgresFact] public async Task CrossTenantCrossCondominiumAndViewerAccessAreDenied()
    {
        var tenant=await SeedTenant();var other=await SeedTenant();await using var host=new SecurityTestHost(tenant);await using var host2=new SecurityTestHost(other);using var c=await host.CreateAuthenticatedClient();using var c2=await host2.CreateAuthenticatedClient();var a=await Condo(c);var b=await Condo(c,"B");var remote=await Condo(c2);var p=await Period(c,a.Id);var pb=await Period(c,b.Id);var s=await Supplier(c);var remoteSupplier=await Supplier(c2);var accounts=await Get<AccountDto[]>(c,Base(b.Id)+"/accounts");var account=accounts.First(x=>x.Type==AccountType.Expense);
        var input=new ExpenseInput(p.Id,s.Id,account.Id,"SYN",new(2026,8,1),null,"Cross",1,0,1,null);
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,Base(a.Id)+"/expenses",input));
        var own=(await Get<AccountDto[]>(c,Base(a.Id)+"/accounts")).First(x=>x.Type==AccountType.Expense);
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,Base(a.Id)+"/expenses",input with {AccountId=own.Id,SupplierId=remoteSupplier.Id}));
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,Base(a.Id)+"/expenses",input with {AccountId=own.Id,AccountingPeriodId=pb.Id}));
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Base(remote.Id)+"/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,"/api/accounting/suppliers/"+remoteSupplier.Id,new SupplierInput("Tampered",null,null,null,null,null,null,null),"PUT"));
        var bankB=await Bank(c,b.Id,pb.Id);var e=await Expense(c,a.Id,p.Id,s.Id);await Register(c,a.Id,e.Id);
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,Base(a.Id)+$"/expenses/{e.Id}/payments",new PaymentInput(Guid.NewGuid(),p.Id,bankB.Id,new(2026,8,2),1,null)));
        using var viewer=host.Browser();await SecurityTestHost.Login(viewer,await host.Account(tenant,StudioRole.Viewer),tenant);
        Assert.Equal(HttpStatusCode.OK,(await viewer.GetAsync(Base(a.Id)+"/accounts")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,await Status(viewer,Base(a.Id)+"/accounts/initialize",new{}));
        using var anonymous=host.Browser();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Base(a.Id)+"/summary")).StatusCode);
        c.DefaultRequestHeaders.Add("X-Expected-Tenant",other.ToString());Assert.Equal(HttpStatusCode.Conflict,(await c.GetAsync(Base(a.Id)+"/accounts")).StatusCode);
    }
    [PostgresFact] public async Task OpeningBalancesGenericReceiptsAndZeroOpeningRetryAreExact()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var path=Base(condo.Id);
        var input=new FinancialAccountInput("CASH","Synthetic cash",FinancialAccountType.Cash,null,0,p.Id,new(2026,1,1),Guid.NewGuid());var cash=await Send<FinancialAccountDto>(c,path+"/financial-accounts",input);Assert.Equal(cash.Id,(await Send<FinancialAccountDto>(c,path+"/financial-accounts",input)).Id);
        var bank=await Bank(c,condo.Id,p.Id,-10.01m);Assert.Equal(-10.01m,bank.Balance);
        var receipt=new ReceiptInput(Guid.NewGuid(),p.Id,cash.Id,new(2026,8,1),200.01m,"Synthetic unassigned receipt",null);
        var j=await Send<JournalDto>(c,path+"/receipts",receipt);Assert.Equal(j.Id,(await Send<JournalDto>(c,path+"/receipts",receipt)).Id);
        Assert.Equal(190,(await Get<LedgerSummary>(c,path+"/summary")).Availability);
        Assert.Empty(await Get<ExpenseDto[]>(c,path+"/expenses"));
        await Send<FinancialAccountDto>(c,path+"/financial-accounts/"+cash.Id,new FinancialAccountUpdate(cash.Name,null,false),"PUT");
        Assert.Equal(HttpStatusCode.NotFound,await Status(c,path+"/receipts",receipt with {RequestId=Guid.NewGuid()}));
        await using var db=Db(tenant);await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"FinancialAccounts\" SET \"OpeningBalance\"={999m} WHERE \"Id\"={cash.Id}"));
    }
    [PostgresFact] public async Task DatabaseRejectsUnbalancedDirectInsertAndLateLines()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var period=await Period(c,condo.Id);var path=Base(condo.Id);var chart=await Get<AccountDto[]>(c,path+"/accounts");var id=Guid.NewGuid();
        await using(var db=Db(tenant)) {
            await using var tx=await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"JournalEntries\" (\"Id\",\"TenantId\",\"CondominiumId\",\"AccountingPeriodId\",\"Date\",\"Description\",\"RequestId\",\"RequestHash\",\"Kind\",\"IsPosted\",\"CreatedAt\",\"UpdatedAt\") VALUES ({id},{tenant},{condo.Id},{period.Id},{new DateOnly(2026,8,1)},{"Unbalanced raw"},{Guid.NewGuid()},{"hash"},0,true,now(),now())");
            var exception=await Assert.ThrowsAsync<PostgresException>(()=>tx.CommitAsync());Assert.Equal(PostgresErrorCodes.CheckViolation,exception.SqlState);
        }
        Assert.Empty(await Get<JournalDto[]>(c,path+"/journals"));
        var input=new JournalInput(Guid.NewGuid(),period.Id,new(2026,8,1),"Synthetic balanced",null,[new(chart.First(x=>x.Type==AccountType.Expense).Id,10,0),new(chart.First(x=>x.Type==AccountType.Equity && x.Purpose==AccountPurpose.General).Id,0,10)]);
        var journal=await Send<JournalDto>(c,path+"/journals",input);
        await using(var db=Db(tenant)) {
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"JournalEntryLines\" (\"Id\",\"TenantId\",\"CondominiumId\",\"JournalEntryId\",\"AccountId\",\"Debit\",\"Credit\",\"CreatedAt\",\"UpdatedAt\") VALUES ({Guid.NewGuid()},{tenant},{condo.Id},{journal.Id},{chart[0].Id},1,0,now(),now())"));
        }
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,$"/api/condominiums/{condo.Id}/accounting-periods/{period.Id}/close",new{})); // Unclassified manual cost must not bypass administrative closure.
        Assert.Equal(journal.Id,(await Send<JournalDto>(c,path+"/journals",input)).Id);
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+"/journals",input with {RequestId=Guid.NewGuid(),Date=new(2027,1,1)})); // Date outside the open exercise; closed-period retry coverage is in M06.3.
    }
    [PostgresFact] public async Task DirectApplicationServiceCreatesOpeningAtomically()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var period=await Period(c,condo.Id);
        await using var db=Db(tenant);var service=new AccountingService(db,new Context(tenant));
        var result=await service.CreateFinancialAccount(condo.Id,new FinancialAccountInput("DIRECT","Synthetic direct bank",FinancialAccountType.BankAccount,null,10,period.Id,new(2026,1,1),Guid.NewGuid()),CancellationToken.None);
        Assert.Equal(10,result.Balance);
    }    [PostgresFact] public async Task OpeningCorrectionUsesReversalAndNewBalancedJournal()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var period=await Period(c,condo.Id);var bank=await Bank(c,condo.Id,period.Id,10);var path=Base(condo.Id);
        var opening=Assert.Single(await Get<JournalDto[]>(c,path+"/journals"));
        await Send<JournalDto>(c,path+$"/journals/{opening.Id}/reverse",new ReversalInput(Guid.NewGuid(),period.Id,new(2026,1,2),"Synthetic opening correction"));
        var chart=await Get<AccountDto[]>(c,path+"/accounts");var equity=chart.Single(x=>x.Purpose==AccountPurpose.OpeningEquity);
        var input=new JournalInput(Guid.NewGuid(),period.Id,new(2026,1,2),"Synthetic corrected opening",null,[new(bank.AccountId,25.01m,0),new(equity.Id,0,25.01m)]);
        await Send<JournalDto>(c,path+"/journals",input);
        var corrected=Assert.Single(await Get<FinancialAccountDto[]>(c,path+"/financial-accounts"));Assert.Equal(10,corrected.OpeningBalance);Assert.Equal(25.01m,corrected.Balance);
        using var collaborator=host.Browser();await SecurityTestHost.Login(collaborator,await host.Account(tenant,StudioRole.Collaborator),tenant);
        Assert.Equal(HttpStatusCode.Forbidden,await Status(collaborator,path+"/journals",input with{RequestId=Guid.NewGuid()}));
        Assert.Equal(HttpStatusCode.Forbidden,await Status(collaborator,path+$"/journals/{opening.Id}/reverse",new ReversalInput(Guid.NewGuid(),period.Id,new(2026,1,2),"Forbidden")));
    }    [PostgresFact] public async Task FullPaymentRetryAfterCloseDoesNotPostAgain()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var s=await Supplier(c);var bank=await Bank(c,condo.Id,p.Id);var e=await Expense(c,condo.Id,p.Id,s.Id);await Register(c,condo.Id,e.Id);var path=Base(condo.Id);
        var payment=new PaymentInput(Guid.NewGuid(),p.Id,bank.Id,new(2026,8,2),122,"Synthetic full payment");
        var original=await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",payment);
        Assert.Equal(ExpenseStatus.Paid,(await Get<ExpenseDto[]>(c,path+"/expenses")).Single().Status);
        await AdministrativeTestFixture.Close(c,condo.Id,p.Id);
        Assert.Equal(original.Id,(await Send<JournalDto>(c,path+$"/expenses/{e.Id}/payments",payment)).Id);
        Assert.Equal(3,(await Get<JournalDto[]>(c,path+"/journals")).Count(j=>j.Kind!=JournalKind.FinalAdjustment));
    }
    [PostgresFact] public async Task DuplicateFinancialAccountRollsBackAccountJournalAndAudit()
    {
        var tenant=await SeedTenant();await using var host=new SecurityTestHost(tenant);using var c=await host.CreateAuthenticatedClient();var condo=await Condo(c);var p=await Period(c,condo.Id);var bank=await Bank(c,condo.Id,p.Id);var path=Base(condo.Id);
        var duplicate=new FinancialAccountInput("ROLLBACK",""+bank.Name,FinancialAccountType.BankAccount,null,50,p.Id,new(2026,1,1),Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Conflict,await Status(c,path+"/financial-accounts",duplicate));
        Assert.DoesNotContain(await Get<AccountDto[]>(c,path+"/accounts"),x=>x.Code=="ROLLBACK");Assert.Single(await Get<FinancialAccountDto[]>(c,path+"/financial-accounts"));Assert.Single(await Get<JournalDto[]>(c,path+"/journals"));
        await using var db=Db(tenant);Assert.Single(await db.SecurityAudits.Where(x=>x.TenantId==tenant && x.Event=="Accounting.OpeningPosted").ToListAsync());
    }}
