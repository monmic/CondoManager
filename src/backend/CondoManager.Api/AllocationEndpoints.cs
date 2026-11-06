using CondoManager.Application;
namespace CondoManager.Api;
public static class AllocationEndpoints
{
    public static void MapAllocations(this WebApplication app)
    {
        var g=app.MapGroup("/api/allocations/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
        g.AddEndpointFilter(async(context,next)=> {var access=context.HttpContext.RequestServices.GetRequiredService<ICurrentTenant>();access.RequireRead();if(!HttpMethods.IsGet(context.HttpContext.Request.Method) && !HttpMethods.IsHead(context.HttpContext.Request.Method) && !context.HttpContext.Request.Path.Value!.EndsWith("/preview",StringComparison.Ordinal)) access.RequireWrite();return await next(context);});
        g.MapGet("/rules",(Guid condo,IAllocationService s,CancellationToken ct)=>s.Rules(condo,ct));
        g.MapPost("/rules",(Guid condo,RuleInput input,IAllocationService s,CancellationToken ct)=>s.SaveRule(condo,null,input,ct));
        g.MapPut("/rules/{id:guid}",(Guid condo,Guid id,RuleInput input,IAllocationService s,CancellationToken ct)=>s.SaveRule(condo,id,input,ct));
        g.MapPost("/rules/{id:guid}/activate",(Guid condo,Guid id,IAllocationService s,CancellationToken ct)=>s.ActivateRule(condo,id,ct));
        g.MapGet("/groups",(Guid condo,IAllocationService s,CancellationToken ct)=>s.Groups(condo,ct));
        g.MapPost("/groups",(Guid condo,GroupInput input,IAllocationService s,CancellationToken ct)=>s.SaveGroup(condo,null,input,ct));
        g.MapPut("/groups/{id:guid}",(Guid condo,Guid id,GroupInput input,IAllocationService s,CancellationToken ct)=>s.SaveGroup(condo,id,input,ct));
        g.MapGet("/readings",(Guid condo,Guid? period,IAllocationService s,CancellationToken ct)=>s.Readings(condo,period,ct));
        g.MapPost("/readings",(Guid condo,ReadingInput input,IAllocationService s,CancellationToken ct)=>s.AddReading(condo,input,ct));
        g.MapGet("/consumptions",(Guid condo,Guid period,string type,string unitOfMeasure,DateOnly from,DateOnly to,IAllocationService s,CancellationToken ct)=>s.Consumptions(condo,period,type,unitOfMeasure,from,to,ct));
        g.MapGet("/assignments",(Guid condo,IAllocationService s,CancellationToken ct)=>s.Assignments(condo,ct));
        g.MapPut("/assignments",(Guid condo,AssignmentInput input,IAllocationService s,CancellationToken ct)=>s.SaveAssignment(condo,input,ct));
        g.MapPost("/preview",(Guid condo,AllocationPreviewInput input,IAllocationService s,CancellationToken ct)=>s.Preview(condo,input,ct));
        g.MapPost("/confirm",(Guid condo,AllocationConfirmInput input,IAllocationService s,CancellationToken ct)=>s.Confirm(condo,input,ct));
        g.MapGet("/runs",(Guid condo,Guid? period,IAllocationService s,CancellationToken ct)=>s.Runs(condo,period,ct));
        g.MapGet("/runs/{id:guid}",(Guid condo,Guid id,IAllocationService s,CancellationToken ct)=>s.Run(condo,id,ct));
    }
}
