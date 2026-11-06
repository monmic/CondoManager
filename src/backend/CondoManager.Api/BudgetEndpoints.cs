using CondoManager.Application;
namespace CondoManager.Api;
public static class BudgetEndpoints
{
    public static void MapBudgets(this WebApplication app)
    {
        var g=app.MapGroup("/api/budgets/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
        g.MapGet("/",(Guid condo,Guid? period,IBudgetService s,CancellationToken ct)=>s.List(condo,period,ct));
        g.MapPost("/",(Guid condo,BudgetInput input,IBudgetService s,CancellationToken ct)=>s.Save(condo,null,input,ct));
        g.MapPut("/{id:guid}",(Guid condo,Guid id,BudgetInput input,IBudgetService s,CancellationToken ct)=>s.Save(condo,id,input,ct));
        g.MapPost("/{id:guid}/propose",(Guid condo,Guid id,IBudgetService s,CancellationToken ct)=>s.Propose(condo,id,ct));
        g.MapPost("/{id:guid}/preview",(Guid condo,Guid id,IBudgetService s,CancellationToken ct)=>s.Preview(condo,id,ct));
        g.MapPost("/{id:guid}/approve",(Guid condo,Guid id,BudgetApprovalInput input,IBudgetService s,CancellationToken ct)=>s.Approve(condo,id,input,ct));
    }
}
