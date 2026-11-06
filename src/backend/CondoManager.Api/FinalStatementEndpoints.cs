using CondoManager.Application;
namespace CondoManager.Api;
public static class FinalStatementEndpoints
{
 public static void MapFinalStatements(this WebApplication app)
 {
  var g=app.MapGroup("/api/final-statements/condominiums/{condo:guid}").RequireAuthorization("TenantMember");
  g.MapPost("/preview",(Guid condo,FinalInput input,IFinalStatementService s,CancellationToken ct)=>s.PreviewFinal(condo,input,ct));
  g.MapPost("/",(Guid condo,FinalInput input,IFinalStatementService s,CancellationToken ct)=>s.CreateFinal(condo,input,ct));
  g.MapGet("/",(Guid condo,Guid? period,IFinalStatementService s,CancellationToken ct)=>s.FinalStatements(condo,period,ct));
  g.MapPost("/{id:guid}/propose",(Guid condo,Guid id,IFinalStatementService s,CancellationToken ct)=>s.ProposeFinal(condo,id,ct));
  g.MapPost("/{id:guid}/approve",(Guid condo,Guid id,FinalApproval input,IFinalStatementService s,CancellationToken ct)=>s.ApproveFinal(condo,id,input,ct));
  g.MapPost("/{id:guid}/settlement-preview",(Guid condo,Guid id,SettlementPreviewInput input,IFinalStatementService s,CancellationToken ct)=>s.PreviewSettlement(condo,id,input.Date,ct));
  g.MapPost("/{id:guid}/settlement-confirm",(Guid condo,Guid id,SettlementConfirm input,IFinalStatementService s,CancellationToken ct)=>s.ConfirmSettlement(condo,id,input,ct));
  g.MapGet("/settlements",(Guid condo,Guid? period,IFinalStatementService s,CancellationToken ct)=>s.Settlements(condo,period,ct));
  g.MapPost("/settlements/{id:guid}/post",(Guid condo,Guid id,IFinalStatementService s,CancellationToken ct)=>s.PostSettlement(condo,id,ct));
  g.MapPost("/settlements/{id:guid}/reverse",(Guid condo,Guid id,ReversalInput input,IFinalStatementService s,CancellationToken ct)=>s.ReverseSettlement(condo,id,input,ct));
  g.MapGet("/periods/{period:guid}/opening",(Guid condo,Guid period,IFinalStatementService s,CancellationToken ct)=>s.GetOpening(condo,period,ct));
  g.MapPost("/periods/{period:guid}/opening",(Guid condo,Guid period,OpeningInput input,IFinalStatementService s,CancellationToken ct)=>s.SetOpening(condo,period,input,ct));
  g.MapGet("/periods/{period:guid}/review",(Guid condo,Guid period,IFinalStatementService s,CancellationToken ct)=>s.ReviewClosure(condo,period,ct));
  g.MapPost("/periods/{period:guid}/close",(Guid condo,Guid period,CloseInput input,IFinalStatementService s,CancellationToken ct)=>s.CloseAdministration(condo,period,input,ct));
  g.MapGet("/closures",(Guid condo,IFinalStatementService s,CancellationToken ct)=>s.Closures(condo,ct));
 }
}
