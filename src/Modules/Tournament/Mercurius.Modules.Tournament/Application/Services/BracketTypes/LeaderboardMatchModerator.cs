using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Tournament.Domain;

namespace Mercurius.Modules.Tournament.Application.Services.BracketTypes;

internal sealed class LeaderboardMatchModerator : IMatchModerator
{
    public IEnumerable<Match> GenerateMatchesForTournament(TournamentAggregate tournament) => [];

    public void EnsureCanComplete(TournamentAggregate tournament)
    {
        if (tournament.GetLeaderboardRanking().Count == 0)
            throw new ValidationException("A leaderboard tournament requires at least one valid recorded result before completion.");
    }

    public void DeterminePlacements(TournamentAggregate tournament)
    {
        EnsureCanComplete(tournament);
        foreach (var group in tournament.GetLeaderboardRanking().GroupBy(row => row.Rank))
        {
            tournament.Placements.Add(new Placement
            {
                TournamentId = tournament.Id,
                Place = group.Key,
                LeaderboardParticipants = group
                    .Select(row => new PlacementLeaderboardParticipant { LeaderboardParticipantId = row.Participant.Id })
                    .ToList()
            });
        }
    }
}
