using Mercurius.Modules.Tournament.Contracts;
using Mercurius.Modules.Discovery.Domain;
using Platform.Eventing;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class TournamentSearchProjectionHandler :
    IModuleEventHandler<TournamentCreatedIntegrationEvent>,
    IModuleEventHandler<TournamentUpdatedIntegrationEvent>,
    IModuleEventHandler<TournamentCanceledIntegrationEvent>,
    IModuleEventHandler<TournamentDeletedIntegrationEvent>
{
    private readonly SearchDocumentProjector _projector;

    public TournamentSearchProjectionHandler(SearchDocumentProjector projector)
    {
        _projector = projector;
    }

    public string ConsumerName => "discovery-search-tournament-v1";

    public Task HandleAsync(
        TournamentCreatedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.TournamentId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        TournamentUpdatedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.TournamentId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        TournamentCanceledIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.TournamentId.Value, payload.Name, context, cancellationToken);

    public Task HandleAsync(
        TournamentDeletedIntegrationEvent payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default)
    {
        return _projector.MarkDeletedAsync(
            SearchDocumentTypes.Tournament,
            payload.TournamentId.Value.ToString(),
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }

    private Task UpsertAsync(
        Guid tournamentId,
        string name,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        return _projector.UpsertAsync(
            SearchDocumentTypes.Tournament,
            tournamentId.ToString(),
            name,
            "Tournament",
            imageUrl: null,
            $"/tournaments/{tournamentId}",
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }
}
