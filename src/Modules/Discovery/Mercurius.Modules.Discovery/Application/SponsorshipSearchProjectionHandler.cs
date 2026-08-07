using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Sponsorship.Contracts.V1;
using Platform.Eventing;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class SponsorshipSearchProjectionHandler :
    IModuleEventHandler<SponsorCreated>,
    IModuleEventHandler<SponsorUpdated>,
    IModuleEventHandler<SponsorDeleted>
{
    private readonly SearchDocumentProjector _projector;

    public SponsorshipSearchProjectionHandler(SearchDocumentProjector projector)
    {
        _projector = projector;
    }

    public string ConsumerName => "discovery-search-sponsorship-v1";

    public Task HandleAsync(
        SponsorCreated payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.SponsorId.Value, payload.Name, payload.LogoUrl, context, cancellationToken);

    public Task HandleAsync(
        SponsorUpdated payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default) =>
        UpsertAsync(payload.SponsorId.Value, payload.Name, payload.LogoUrl, context, cancellationToken);

    public Task HandleAsync(
        SponsorDeleted payload,
        ModuleEventContext context,
        CancellationToken cancellationToken = default)
    {
        return _projector.MarkDeletedAsync(
            SearchDocumentTypes.Sponsor,
            payload.SponsorId.Value.ToString(),
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }

    private Task UpsertAsync(
        int sponsorId,
        string name,
        string imageUrl,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        return _projector.UpsertAsync(
            SearchDocumentTypes.Sponsor,
            sponsorId.ToString(),
            name,
            "Sponsor",
            imageUrl,
            $"/sponsors/{sponsorId}",
            context.OccurredAtUtc.Ticks,
            context.OccurredAtUtc,
            cancellationToken);
    }
}
