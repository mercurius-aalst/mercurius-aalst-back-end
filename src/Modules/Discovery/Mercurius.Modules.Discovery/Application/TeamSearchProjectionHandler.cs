using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Teams.Contracts;
using Platform.Eventing;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class TeamSearchProjectionHandler :
    IModuleEventHandler<TeamCreatedIntegrationEvent>,
    IModuleEventHandler<TeamRenamedIntegrationEvent>,
    IModuleEventHandler<TeamDeletedIntegrationEvent>
{
    private readonly SearchDocumentProjector _projector;

    public TeamSearchProjectionHandler(SearchDocumentProjector projector)
    {
        _projector = projector;
    }

    public string ConsumerName => "discovery-search-teams-v1";

    public Task HandleAsync(
        TeamCreatedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.TeamId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        TeamRenamedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.TeamId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        TeamDeletedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default)
    {
        return _projector.MarkDeletedAsync(
            SearchDocumentTypes.Team,
            payload.TeamId.Value.ToString(),
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }

    private Task UpsertAsync(
        Guid teamId,
        string name,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        return _projector.UpsertAsync(
            SearchDocumentTypes.Team,
            teamId.ToString(),
            name,
            "Team",
            imageUrl: null,
            $"/teams/{Uri.EscapeDataString(name)}",
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }
}
