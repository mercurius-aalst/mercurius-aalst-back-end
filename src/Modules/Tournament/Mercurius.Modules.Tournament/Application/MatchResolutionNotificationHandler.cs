using Mercurius.Modules.Tournament.Contracts;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;
using ContractRecipientKind = Mercurius.Modules.Tournament.Contracts.MatchResolutionNotificationRecipientKind;
using DomainRecipientKind = Mercurius.Modules.Tournament.Domain.MatchResolutionNotificationRecipientKind;

namespace Mercurius.Modules.Tournament.Application;

internal sealed class MatchResolutionNotificationHandler : IModuleEventHandler<MatchResolutionRequiredIntegrationEvent>
{
    internal const string ConsumerNameValue = "tournament-match-resolution-notification";
    private readonly ITournamentDbContext _dbContext;

    public MatchResolutionNotificationHandler(ITournamentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string ConsumerName => ConsumerNameValue;

    public async Task HandleAsync(
        MatchResolutionRequiredIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.MatchResolutionNotifications.Local.Any(notification => notification.Id == context.MessageId) ||
            await _dbContext.MatchResolutionNotifications
                .AnyAsync(notification => notification.Id == context.MessageId, cancellationToken))
        {
            return;
        }

        var recipient = payload.GetRecipient();
        _dbContext.MatchResolutionNotifications.Add(new MatchResolutionNotification
        {
            Id = context.MessageId,
            MatchId = payload.MatchId.Value,
            TournamentId = payload.TournamentId.Value,
            RecipientUserId = recipient.UserId?.Value,
            RecipientKind = recipient.Kind == ContractRecipientKind.AssignedAdmin
                ? DomainRecipientKind.AssignedAdmin
                : DomainRecipientKind.GlobalAdmin,
            OccurredAtUtc = context.OccurredAtUtc,
            CreatedAtUtc = context.OccurredAtUtc
        });
    }
}
