using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Discovery.Domain;
using Platform.Eventing;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class CompetitionSearchProjectionHandler :
    IModuleEventHandler<GameCreatedIntegrationEvent>,
    IModuleEventHandler<GameUpdatedIntegrationEvent>,
    IModuleEventHandler<GameCanceledIntegrationEvent>,
    IModuleEventHandler<GameDeletedIntegrationEvent>
{
    private readonly SearchDocumentProjector _projector;

    public CompetitionSearchProjectionHandler(SearchDocumentProjector projector)
    {
        _projector = projector;
    }

    public string ConsumerName => "discovery-search-competition-v1";

    public Task HandleAsync(
        GameCreatedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.GameId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        GameUpdatedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.GameId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        GameCanceledIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.GameId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        GameDeletedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default)
    {
        return _projector.MarkDeletedAsync(
            SearchDocumentTypes.Game,
            payload.GameId.Value.ToString(),
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }

    private Task UpsertAsync(
        Guid gameId,
        string name,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        return _projector.UpsertAsync(
            SearchDocumentTypes.Game,
            gameId.ToString(),
            name,
            "Game",
            imageUrl: null,
            $"/games/{gameId}",
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }
}
