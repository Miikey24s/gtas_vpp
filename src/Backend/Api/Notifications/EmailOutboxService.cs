using System.Security.Cryptography;
using System.Text;
using gtas_vpp_be.Model.Notifications;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Notifications;

public interface IEmailOutboxService
{
    Task<Guid> EnqueueAsync(
        string memberCompanyCode,
        AccountEmailMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailOutboxMessage>> GetDueAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default);
}

public sealed class EmailOutboxService(VPPContext context) : IEmailOutboxService
{
    private readonly VPPContext _context = context;

    public async Task<Guid> EnqueueAsync(
        string memberCompanyCode,
        AccountEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var key = ComputeDeduplicationKey(memberCompanyCode, message);
        var existing = await _context.Set<EmailOutboxMessage>()
            .FirstOrDefaultAsync(item => item.DeduplicationKey == key, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var now = DateTime.UtcNow;
        var entity = new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = memberCompanyCode,
            Recipient = message.Recipient,
            Subject = message.Subject,
            DeduplicationKey = key,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Status = "Pending",
            NextAttemptAtUtc = now,
            CreatedAtUtc = now
        };
        _context.Set<EmailOutboxMessage>().Add(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(entity).State = EntityState.Detached;
            var winner = await _context.Set<EmailOutboxMessage>()
                .AsNoTracking()
                .SingleAsync(item => item.DeduplicationKey == key, cancellationToken);
            return winner.Id;
        }
    }

    public async Task<IReadOnlyList<EmailOutboxMessage>> GetDueAsync(
        DateTime nowUtc,
        int take,
        CancellationToken cancellationToken = default)
        => await _context.Set<EmailOutboxMessage>()
            .Where(item => item.Status == "Pending" && item.NextAttemptAtUtc <= nowUtc)
            .OrderBy(item => item.NextAttemptAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task MarkSentAsync(Guid id, DateTime sentAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<EmailOutboxMessage>().SingleAsync(item => item.Id == id, cancellationToken);
        entity.Status = "Sent";
        entity.SentAtUtc = sentAtUtc;
        entity.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<EmailOutboxMessage>().SingleAsync(item => item.Id == id, cancellationToken);
        entity.Status = "Pending";
        entity.AttemptCount++;
        entity.LastAttemptAtUtc = DateTime.UtcNow;
        entity.NextAttemptAtUtc = nextAttemptAtUtc;
        entity.LastError = error.Trim().Length <= 1000 ? error.Trim() : error.Trim()[..1000];
        await _context.SaveChangesAsync(cancellationToken);
    }

    internal static string ComputeDeduplicationKey(string companyCode, AccountEmailMessage message)
    {
        var canonical = $"{companyCode.Trim()}|{message.Recipient.Trim().ToLowerInvariant()}|{message.Subject.Trim()}|{message.TextBody}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed class OutboxAccountEmailSender(
    IEmailOutboxService outbox,
    ICurrentMemberCompanyProvider companyProvider) : IAccountEmailSender
{
    private readonly IEmailOutboxService _outbox = outbox;
    private readonly ICurrentMemberCompanyProvider _companyProvider = companyProvider;

    public async Task SendAsync(AccountEmailMessage message, CancellationToken cancellationToken = default)
    {
        await _outbox.EnqueueAsync(_companyProvider.MemberCompanyCode, message, cancellationToken);
    }
}

public interface ICurrentMemberCompanyProvider
{
    string MemberCompanyCode { get; }
}

public sealed class DefaultMemberCompanyProvider : ICurrentMemberCompanyProvider
{
    public string MemberCompanyCode => "77500";
}

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AccountEmailOptions> options,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AccountEmailOptions _options = options.Value;
    private readonly ILogger<EmailOutboxWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_options.Enabled)
            {
                await ProcessDueAsync(stoppingToken);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();
        var sender = scope.ServiceProvider.GetRequiredService<SmtpAccountEmailSender>();
        var due = await outbox.GetDueAsync(DateTime.UtcNow, 20, cancellationToken);
        foreach (var item in due)
        {
            try
            {
                await sender.SendAsync(new AccountEmailMessage(
                    item.Recipient, item.Subject, item.TextBody, item.HtmlBody), cancellationToken);
                await outbox.MarkSentAsync(item.Id, DateTime.UtcNow, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var delayMinutes = Math.Min(60, Math.Pow(2, Math.Min(item.AttemptCount, 6)));
                await outbox.MarkFailedAsync(
                    item.Id,
                    exception.Message,
                    DateTime.UtcNow.AddMinutes(delayMinutes),
                    cancellationToken);
                _logger.LogWarning(exception, "Email outbox delivery failed for {OutboxId}; retry scheduled.", item.Id);
            }
        }
    }
}
