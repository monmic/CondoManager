using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using CondoManager.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CondoManager.Infrastructure;

public record DevelopmentMessage(Guid Id, string Recipient, string Kind, string Link, Guid? ActorUserId, Guid? TenantId, DateTimeOffset CreatedAt);
public sealed class DevelopmentMailbox(IConfiguration config, TimeProvider clock) : ISecurityMessageSender
{
    private readonly ConcurrentQueue<DevelopmentMessage> entries = new();
    public bool IsAvailable => true;
    public Task Send(SecurityMessage message, CancellationToken ct)
    {
        entries.Enqueue(new(Guid.NewGuid(), message.Recipient, message.Kind, SecurityMessageLinks.Create(config, message),
            message.ActorUserId, message.TenantId, clock.GetUtcNow()));
        while (entries.Count > 200) entries.TryDequeue(out _);
        return Task.CompletedTask;
    }
    public IReadOnlyList<DevelopmentMessage> ForUser(Guid userId, string email, Guid? tenantId, bool studioAdmin) =>
        entries.Where(x => x.CreatedAt > clock.GetUtcNow().AddHours(-48) &&
            (x.Kind == "reset" ? string.Equals(x.Recipient, email, StringComparison.OrdinalIgnoreCase) :
            studioAdmin && x.ActorUserId == userId && x.TenantId == tenantId)).ToArray();
}
public sealed class SmtpSecurityMessageSender(IConfiguration config, IHostEnvironment environment) : ISecurityMessageSender
{
    public bool IsAvailable => !string.IsNullOrWhiteSpace(config["Smtp:Host"]) && !string.IsNullOrWhiteSpace(config["Smtp:From"]);
    public async Task Send(SecurityMessage message, CancellationToken ct)
    {
        if (!IsAvailable) throw new SecurityDeliveryUnavailableException();
        var link = SecurityMessageLinks.Create(config, message, !environment.IsDevelopment());
        using var mail = new MailMessage(config["Smtp:From"]!, message.Recipient) {
            Subject = message.Kind == "invite" ? "Invito a CondoManager" : "Recupero password CondoManager",
            Body = $"Apri questo link per continuare: {link}\nIl link e personale, monouso e ha una scadenza. Se non atteso, ignoralo."
        };
        using var client = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587)) { EnableSsl = true, UseDefaultCredentials = false };
        if (!string.IsNullOrEmpty(config["Smtp:Username"])) client.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
        await client.SendMailAsync(mail, ct);
    }
}
internal static class SecurityMessageLinks
{
    public static string Create(IConfiguration config, SecurityMessage message, bool requireHttps = false)
    {
        var root = config["Security:PublicWebUrl"] ?? (requireHttps ? "" : "https://localhost:7262");
        if (!Uri.TryCreate(root, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Configurare Security:PublicWebUrl con un URL HTTPS attendibile.");
        return uri.AbsoluteUri.TrimEnd('/') + "/#" + message.Kind + "=" + Uri.EscapeDataString(message.Token);
    }
}