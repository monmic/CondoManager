using CondoManager.Application;
namespace CondoManager.Api;
public static class AccountingEndpoints
{
    public static void MapAccounting(this WebApplication app)
    {
        var g=app.MapGroup("/api/accounting").RequireAuthorization("TenantMember");
        g.AddEndpointFilter(async (context,next)=> { var access=context.HttpContext.RequestServices.GetRequiredService<ICurrentTenant>(); access.RequireRead(); if(!HttpMethods.IsGet(context.HttpContext.Request.Method) && !HttpMethods.IsHead(context.HttpContext.Request.Method)) access.RequireWrite(); return await next(context); });
        g.MapGet("/suppliers",(string? search,IAccountingService s,CancellationToken ct)=>s.Suppliers(search,ct));
        g.MapPost("/suppliers",(SupplierInput input,IAccountingService s,CancellationToken ct)=>s.SaveSupplier(null,input,ct));
        g.MapPut("/suppliers/{id:guid}",(Guid id,SupplierInput input,IAccountingService s,CancellationToken ct)=>s.SaveSupplier(id,input,ct));
        g.MapDelete("/suppliers/{id:guid}",async(Guid id,IAccountingService s,CancellationToken ct)=>{await s.DeactivateSupplier(id,ct);return Results.NoContent();});
        var c=g.MapGroup("/condominiums/{condo:guid}");
        c.MapGet("/accounts",(Guid condo,IAccountingService s,CancellationToken ct)=>s.Accounts(condo,ct));
        c.MapPost("/accounts/initialize",(Guid condo,IAccountingService s,CancellationToken ct)=>s.InitializeChart(condo,ct));
        c.MapPost("/accounts",(Guid condo,AccountInput input,IAccountingService s,CancellationToken ct)=>s.SaveAccount(condo,null,input,ct));
        c.MapPut("/accounts/{id:guid}",(Guid condo,Guid id,AccountInput input,IAccountingService s,CancellationToken ct)=>s.SaveAccount(condo,id,input,ct));
        c.MapGet("/expenses",(Guid condo,Guid? period,IAccountingService s,CancellationToken ct)=>s.Expenses(condo,period,ct));
        c.MapPost("/expenses",(Guid condo,ExpenseInput input,IAccountingService s,CancellationToken ct)=>s.SaveExpense(condo,null,input,ct));
        c.MapPut("/expenses/{id:guid}",(Guid condo,Guid id,ExpenseInput input,IAccountingService s,CancellationToken ct)=>s.SaveExpense(condo,id,input,ct));
        c.MapPost("/expenses/{id:guid}/register",(Guid condo,Guid id,PostingInput input,IAccountingService s,CancellationToken ct)=>s.RegisterExpense(condo,id,input,ct));
        c.MapDelete("/expenses/{id:guid}",async(Guid condo,Guid id,IAccountingService s,CancellationToken ct)=>{await s.CancelDraft(condo,id,ct);return Results.NoContent();});
        c.MapPost("/expenses/{id:guid}/payments",(Guid condo,Guid id,PaymentInput input,IAccountingService s,CancellationToken ct)=>s.Pay(condo,id,input,ct));
        c.MapGet("/financial-accounts",(Guid condo,IAccountingService s,CancellationToken ct)=>s.FinancialAccounts(condo,ct));
        c.MapPost("/financial-accounts",(Guid condo,FinancialAccountInput input,IAccountingService s,CancellationToken ct)=>s.CreateFinancialAccount(condo,input,ct));
        c.MapPut("/financial-accounts/{id:guid}",(Guid condo,Guid id,FinancialAccountUpdate input,IAccountingService s,CancellationToken ct)=>s.UpdateFinancialAccount(condo,id,input,ct));
        c.MapPost("/receipts",(Guid condo,ReceiptInput input,IAccountingService s,CancellationToken ct)=>s.Receipt(condo,input,ct));
        c.MapGet("/journals",(Guid condo,Guid? period,Guid? account,DateOnly? from,DateOnly? to,string? search,IAccountingService s,CancellationToken ct)=>s.Journals(condo,period,account,from,to,search,ct));
        c.MapPost("/journals",(Guid condo,JournalInput input,IAccountingService s,CancellationToken ct)=>s.Post(condo,input,ct));
        c.MapPost("/journals/{id:guid}/reverse",(Guid condo,Guid id,ReversalInput input,IAccountingService s,CancellationToken ct)=>s.Reverse(condo,id,input,ct));
        c.MapGet("/summary",(Guid condo,Guid? period,IAccountingService s,CancellationToken ct)=>s.Summary(condo,period,ct));
    }
}
