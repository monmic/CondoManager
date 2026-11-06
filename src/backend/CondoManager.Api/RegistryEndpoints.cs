using CondoManager.Application;
using CondoManager.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CondoManager.Api;
public static class RegistryEndpoints
{
    public static void MapRegistry(this WebApplication app)
    {
        var g = app.MapGroup("/api").RequireAuthorization("TenantMember");
        g.AddEndpointFilter(async (context, next) =>
        {
            var access = context.HttpContext.RequestServices.GetRequiredService<ICurrentTenant>();
            access.RequireRead();
            if (!HttpMethods.IsGet(context.HttpContext.Request.Method) && !HttpMethods.IsHead(context.HttpContext.Request.Method)) access.RequireWrite();
            return await next(context);
        });
        g.MapGet("/condominiums", (IRegistryService s, CancellationToken ct) => s.Condominiums(ct));
        g.MapGet("/condominiums/{id:guid}", (Guid id, IRegistryService s, CancellationToken ct) => s.Condominium(id, ct));
        g.MapPost("/condominiums", async (CondominiumInput input, IRegistryService s, CancellationToken ct) => {
            var row = await s.SaveCondominium(null, input, ct); return Results.Created($"/api/condominiums/{row.Id}", row);
        });
        g.MapPut("/condominiums/{id:guid}", (Guid id, CondominiumInput input, IRegistryService s, CancellationToken ct) => s.SaveCondominium(id, input, ct));
        g.MapDelete("/condominiums/{id:guid}", async (Guid id, IRegistryService s, CancellationToken ct) => {
            await s.DeactivateCondominium(id, ct); return Results.NoContent();
        });
        g.MapGet("/condominiums/{condominiumId:guid}/structure", (Guid condominiumId, IRegistryService s, CancellationToken ct) => s.Structure(condominiumId, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/buildings", (Guid condominiumId, BuildingInput input, IRegistryService s, CancellationToken ct) => s.SaveBuilding(condominiumId, null, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/buildings/{id:guid}", (Guid condominiumId, Guid id, BuildingInput input, IRegistryService s, CancellationToken ct) => s.SaveBuilding(condominiumId, id, input, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/staircases", (Guid condominiumId, StaircaseInput input, IRegistryService s, CancellationToken ct) => s.SaveStaircase(condominiumId, null, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/staircases/{id:guid}", (Guid condominiumId, Guid id, StaircaseInput input, IRegistryService s, CancellationToken ct) => s.SaveStaircase(condominiumId, id, input, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/units", (Guid condominiumId, PropertyUnitInput input, IRegistryService s, CancellationToken ct) => s.SaveUnit(condominiumId, null, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/units/{id:guid}", (Guid condominiumId, Guid id, PropertyUnitInput input, IRegistryService s, CancellationToken ct) => s.SaveUnit(condominiumId, id, input, ct));
        g.MapGet("/people", (IRegistryService s, CancellationToken ct) => s.People(ct));
        g.MapPost("/people", (PersonInput input, IRegistryService s, CancellationToken ct) => s.SavePerson(null, input, ct));
        g.MapPut("/people/{id:guid}", (Guid id, PersonInput input, IRegistryService s, CancellationToken ct) => s.SavePerson(id, input, ct));
        g.MapGet("/condominiums/{condominiumId:guid}/associations", (Guid condominiumId, IRegistryService s, CancellationToken ct) => s.Associations(condominiumId, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/units/{unitId:guid}/associations", (Guid condominiumId, Guid unitId, AssociationInput input, IRegistryService s, CancellationToken ct) => s.Associate(condominiumId, unitId, input, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/associations/{id:guid}/end", async (Guid condominiumId, Guid id, EndAssociationInput input, IRegistryService s, CancellationToken ct) => {
            await s.EndAssociation(condominiumId, id, input.ValidTo, ct); return Results.NoContent();
        });
        g.MapGet("/condominiums/{condominiumId:guid}/allocation-tables", (Guid condominiumId, IRegistryService s, CancellationToken ct) => s.Tables(condominiumId, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/allocation-tables", (Guid condominiumId, AllocationTableInput input, IRegistryService s, CancellationToken ct) => s.SaveTable(condominiumId, null, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/allocation-tables/{id:guid}", (Guid condominiumId, Guid id, AllocationTableInput input, IRegistryService s, CancellationToken ct) => s.SaveTable(condominiumId, id, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/allocation-tables/{id:guid}/entries/{unitId:guid}", (Guid condominiumId, Guid id, Guid unitId, AllocationEntryInput input, IRegistryService s, CancellationToken ct) => s.SetEntry(condominiumId, id, unitId, input.Value, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/allocation-tables/{id:guid}/activate", (Guid condominiumId, Guid id, IRegistryService s, CancellationToken ct) => s.ActivateTable(condominiumId, id, ct));
        g.MapDelete("/condominiums/{condominiumId:guid}/allocation-tables/{id:guid}", async (Guid condominiumId, Guid id, IRegistryService s, CancellationToken ct) => {
            await s.ArchiveTable(condominiumId, id, ct); return Results.NoContent();
        });
        g.MapGet("/condominiums/{condominiumId:guid}/accounting-periods", (Guid condominiumId, IRegistryService s, CancellationToken ct) => s.Periods(condominiumId, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/accounting-periods", (Guid condominiumId, AccountingPeriodInput input, IRegistryService s, CancellationToken ct) => s.SavePeriod(condominiumId, null, input, ct));
        g.MapPut("/condominiums/{condominiumId:guid}/accounting-periods/{id:guid}", (Guid condominiumId, Guid id, AccountingPeriodInput input, IRegistryService s, CancellationToken ct) => s.SavePeriod(condominiumId, id, input, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/accounting-periods/{id:guid}/open", (Guid condominiumId, Guid id, IRegistryService s, CancellationToken ct) => s.TransitionPeriod(condominiumId, id, false, ct));
        g.MapPost("/condominiums/{condominiumId:guid}/accounting-periods/{id:guid}/close", (Guid condominiumId, Guid id, IRegistryService s, CancellationToken ct) => s.TransitionPeriod(condominiumId, id, true, ct));
    }
}