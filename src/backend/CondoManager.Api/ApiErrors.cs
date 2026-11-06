using CondoManager.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CondoManager.Api;
public static class ApiErrors
{
    public static async Task Handle(HttpContext context, Exception exception)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return;
        PostgresException? pg = null;
        for (Exception? cause = exception; cause != null; cause = cause.InnerException) if (cause is PostgresException found) { pg = found; break; }
        var (status, message) = exception switch
        {
            AccessDeniedException => (403, "Accesso non consentito per lo studio o il ruolo corrente."),
            SecurityDeliveryUnavailableException => (503, "Invio email non disponibile. Contattare l'amministratore."),
            NotFoundException => (404, exception.Message),
            TenantUnavailableException => (503, "Tenant non configurato."),
            ArgumentException => (400, exception.Message),
            BadHttpRequestException => (400, "Richiesta non valida."),
            DbUpdateConcurrencyException => (409, "La risorsa e stata modificata o non appartiene allo studio."),
            _ when pg?.SqlState == PostgresErrorCodes.UniqueViolation => (409, "Codice o codice fiscale gia presente nel relativo ambito."),
            _ when pg?.SqlState == PostgresErrorCodes.ForeignKeyViolation => (409, "Relazione non valida o risorsa ancora utilizzata."),
            _ when pg?.SqlState == PostgresErrorCodes.ExclusionViolation => (409, "Intervallo sovrapposto a dati esistenti."),
            _ when pg?.SqlState == PostgresErrorCodes.CheckViolation => (400, "Vincoli dei dati non rispettati."),
            _ when pg?.SqlState == "55000" => (409, "Dato storico o attivato non modificabile."),
            NpgsqlException => (503, "Database temporaneamente non disponibile."),
            InvalidOperationException => (409, exception.Message),
            _ => (500, "Errore interno. Consultare i log del server.")
        };
        if (status >= 500) context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ApiErrors").LogError(exception, "Errore API {TraceId}", context.TraceIdentifier);
        await Results.Problem(statusCode: status, title: message, extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
    }
}