using Mercurius.Modules.Tournament.Contracts;
using Platform.Realtime;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface ITournamentRealtimePublisher
{
    Task RosterConfirmationChangedAsync(
        Guid teamId,
        Guid rosterMemberId,
        Guid affectedUserId,
        string status,
        CancellationToken cancellationToken = default);
}

internal sealed class TournamentRealtimePublisher : ITournamentRealtimePublisher
{
    private readonly IRealtimePublisher _realtimePublisher;

    public TournamentRealtimePublisher(IRealtimePublisher realtimePublisher)
    {
        _realtimePublisher = realtimePublisher;
    }

    public Task RosterConfirmationChangedAsync(
        Guid teamId,
        Guid rosterMemberId,
        Guid affectedUserId,
        string status,
        CancellationToken cancellationToken = default)
    {
        return _realtimePublisher.PublishAsync(
            new RealtimePublishRequest<TournamentRosterConfirmationChangedEvent>(
                "TournamentRosterConfirmationChanged",
                new TournamentRosterConfirmationChangedEvent(
                    teamId,
                    rosterMemberId,
                    affectedUserId,
                    status),
                [$"user:{affectedUserId:N}", $"team:{teamId:N}"]),
            cancellationToken);
    }
}
