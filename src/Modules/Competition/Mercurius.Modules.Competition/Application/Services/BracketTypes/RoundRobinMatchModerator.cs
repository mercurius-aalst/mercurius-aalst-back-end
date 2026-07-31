using Mercurius.Modules.Competition.Domain;

namespace Mercurius.Modules.Competition.Application.Services.BracketTypes;

internal sealed class RoundRobinMatchModerator : IMatchModerator
{
    public void DeterminePlacements(Game game)
    {
        switch (game.ParticipationMode)
        {
            case ParticipationMode.Individual:
                DeterminePlacements(game, game.GetActiveRegisteredUserIds().ToList(), participant => new Placement
                {
                    GameId = game.Id,
                    Users = [new PlacementUser { UserId = participant }],
                    Place = 0
                });
                break;
            case ParticipationMode.Team:
                DeterminePlacements(game, game.GetActiveRegisteredTeamIds().ToList(), participant => new Placement
                {
                    GameId = game.Id,
                    Teams = [new PlacementTeam { TeamId = participant }],
                    Place = 0
                });
                break;
        }
    }

    public IEnumerable<Match> GenerateMatchesForGame(Game game)
    {
        return game.ParticipationMode switch
        {
            ParticipationMode.Individual => GenerateMatchesForGame(
                game,
                game.GetActiveRegisteredUserIds().ToList(),
                (match, participant1, participant2) => match.SetIndividualParticipants(participant1, participant2)),
            ParticipationMode.Team => GenerateMatchesForGame(
                game,
                game.GetActiveRegisteredTeamIds().ToList(),
                (match, participant1, participant2) => match.SetTeamParticipants(participant1, participant2)),
            _ => throw new InvalidOperationException($"Unsupported participation mode {game.ParticipationMode}.")
        };
    }

    private static void DeterminePlacements(
        Game game,
        List<Guid> participants,
        Func<Guid, Placement> createPlacement)
    {
        if (participants.Count == 0)
            throw new Exception("No participants in the game to determine placements.");

        var winCounts = participants.ToDictionary(
            participant => participant,
            participant => game.Matches.Count(match => match.GetWinnerId() == participant));

        var ordered = participants
            .OrderByDescending(participant => winCounts[participant])
            .ThenBy(participant => participant)
            .ToList();

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            for (int j = i + 1; j < ordered.Count; j++)
            {
                var participant1 = ordered[i];
                var participant2 = ordered[j];
                var participant1Id = participant1;
                var participant2Id = participant2;

                if (winCounts[participant1Id] != winCounts[participant2Id])
                    continue;

                var match = game.Matches.FirstOrDefault(m =>
                    ((m.GetParticipant1Id() == participant1Id && m.GetParticipant2Id() == participant2Id) ||
                     (m.GetParticipant1Id() == participant2Id && m.GetParticipant2Id() == participant1Id)) &&
                    m.GetWinnerId().HasValue);

                if (match is not null && match.GetWinnerId() == participant2Id && i < j)
                {
                    ordered[i] = participant2;
                    ordered[j] = participant1;
                }
            }
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            var placement = createPlacement(ordered[i]);
            placement.Place = i + 1;
            game.Placements.Add(placement);
        }
    }

    private static IEnumerable<Match> GenerateMatchesForGame(
        Game game,
        List<Guid> participants,
        Action<Match, Guid, Guid> assignParticipants)
    {
        var matches = new List<Match>();
        var rotation = participants.Select(participant => (Guid?)participant).ToList();
        if (rotation.Count % 2 != 0)
            rotation.Add(null);

        int totalParticipants = rotation.Count;
        int totalRounds = totalParticipants - 1;
        int matchesPerRound = totalParticipants / 2;
        int matchNumber = 0;

        for (int round = 1; round <= totalRounds; round++)
        {
            for (int i = 0; i < matchesPerRound; i++)
            {
                var participant1 = rotation[i];
                var participant2 = rotation[rotation.Count - 1 - i];

                if (participant1 == null || participant2 == null)
                    continue;

                var match = new Match
                {
                    GameId = game.Id,
                    RoundNumber = round,
                    MatchNumber = matchNumber++,
                    BracketType = game.BracketType,
                    Format = game.Format,
                    ParticipationMode = game.ParticipationMode
                };

                assignParticipants(match, participant1.Value, participant2.Value);
                matches.Add(match);
            }

            var last = rotation[rotation.Count - 1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, last);
        }

        return matches;
    }
}

