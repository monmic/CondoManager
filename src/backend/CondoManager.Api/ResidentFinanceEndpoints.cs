using CondoManager.Application;
namespace CondoManager.Api;
public static class ResidentFinanceEndpoints
{
    public static void MapResidentFinance(this WebApplication app)
    {
        var g=app.MapGroup("/api/resident-finance/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
        g.MapPost("/plans/preview",(Guid condo,PlanInput input,IResidentFinanceService s,CancellationToken ct)=>s.PreviewPlan(condo,input,ct));
        g.MapPost("/plans",(Guid condo,PlanInput input,IResidentFinanceService s,CancellationToken ct)=>s.CreatePlan(condo,input,ct));
        g.MapGet("/plans",(Guid condo,Guid? period,IResidentFinanceService s,CancellationToken ct)=>s.Plans(condo,period,ct));
        g.MapPost("/plans/{id:guid}/approve",(Guid condo,Guid id,PlanApproval input,IResidentFinanceService s,CancellationToken ct)=>s.ApprovePlan(condo,id,input,ct));
        g.MapGet("/credits",(Guid condo,Guid? period,DateOnly? asOf,IResidentFinanceService s,CancellationToken ct)=>s.Credits(condo,period,asOf,ct));
        g.MapGet("/receipts",(Guid condo,Guid? period,IResidentFinanceService s,CancellationToken ct)=>s.ResidentReceipts(condo,period,ct));
        g.MapPost("/receipts",(Guid condo,ResidentReceiptInput input,IResidentFinanceService s,CancellationToken ct)=>s.RecordReceipt(condo,input,ct));
        g.MapPost("/receipts/{id:guid}/allocate",(Guid condo,Guid id,AllocateReceiptInput input,IResidentFinanceService s,CancellationToken ct)=>s.AllocateReceipt(condo,id,input,ct));
        g.MapPost("/allocations/{id:guid}/reverse",(Guid condo,Guid id,ReversalInput input,IResidentFinanceService s,CancellationToken ct)=>s.ReverseAllocation(condo,id,input,ct));
        g.MapGet("/bank-movements",(Guid condo,IResidentFinanceService s,CancellationToken ct)=>s.AvailableBankMovements(condo,ct));
        g.MapPost("/receipts/{id:guid}/match",(Guid condo,Guid id,BankMatchInput input,IResidentFinanceService s,CancellationToken ct)=>s.MatchReceipt(condo,id,input,false,ct));
        g.MapPost("/receipts/{id:guid}/reconcile",(Guid condo,Guid id,BankMatchInput input,IResidentFinanceService s,CancellationToken ct)=>s.MatchReceipt(condo,id,input,true,ct));
        g.MapGet("/balances",(Guid condo,Guid? period,DateOnly? asOf,IResidentFinanceService s,CancellationToken ct)=>s.ResidentBalances(condo,period,asOf,ct));
    }
}
