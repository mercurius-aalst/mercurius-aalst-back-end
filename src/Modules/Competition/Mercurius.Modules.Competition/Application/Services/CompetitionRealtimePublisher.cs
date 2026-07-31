using Mercurius.Modules.Competition.Contracts;
using Platform.Realtime;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface ICompetitionRealtimePublisher
{
    Task RosterConfirmationChangedAsync(
        Guid teamId,
        Guid rosterMemberId,
        Guid affectedUserId,
        string status,
        CancellationToken cancellationToken = default);
}

internal sealed class CompetitionRealtimePublisher : ICompetitionRealtimePublisher
{
    private readonly IRealtimePublisher _realtimePublisher;

    public CompetitionRealtimePublisher(IRealtimePublisher realtimePublisher)
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
