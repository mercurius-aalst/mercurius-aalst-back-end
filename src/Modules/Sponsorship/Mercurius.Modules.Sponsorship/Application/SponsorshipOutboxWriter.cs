using Mercurius.Modules.Sponsorship.Infrastructure;
using Platform.Eventing;

namespace Mercurius.Modules.Sponsorship.Application;

internal sealed class SponsorshipOutboxWriter
{
    private readonly ISponsorshipDbContext _dbContext;
    private readonly IModuleEventPublisher _moduleEventPublisher;

    public SponsorshipOutboxWriter(
        ISponsorshipDbContext dbContext,
        IModuleEventPublisher moduleEventPublisher)
    {
        _dbContext = dbContext;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task SaveAndPublishAsync<TPayload>(
        Func<TPayload> createPayload,
        CancellationToken cancellationToken = default)
        where TPayload : notnull
    {
        if (!string.Equals(
                _dbContext.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal))
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _moduleEventPublisher.Publish(createPayload());
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _moduleEventPublisher.Publish(createPayload());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
