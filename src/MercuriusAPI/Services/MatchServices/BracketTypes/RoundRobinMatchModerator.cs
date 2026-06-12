using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices.BracketTypes;

public class RoundRobinMatchModerator : IMatchModerator
{
    public void DeterminePlacements(Game game)
    {
        switch (game.ParticipationMode)
        {
            case ParticipationMode.Individual:
                DeterminePlacements(game, game.GetActiveRegisteredUsers().ToList(), participant => participant.Id, participant => new Placement
                {
                    GameId = game.Id,
                    Users = [participant],
                    Place = 0
                });
                break;
            case ParticipationMode.Team:
                DeterminePlacements(game, game.GetActiveRegisteredTeams().ToList(), participant => participant.Id, participant => new Placement
                {
                    GameId = game.Id,
                    Teams = [participant],
                    Place = 0
                });
                break;
        }
    }

    public IEnumerable<Match> GenerateMatchesForGame(Game game)
    {
        return game.ParticipationMode switch
        {
            ParticipationMode.Individual => GenerateMatchesForGame(game, game.GetActiveRegisteredUsers().ToList(), (match, p1, p2) => match.SetParticipants(p1, p2)),
            ParticipationMode.Team => GenerateMatchesForGame(game, game.GetActiveRegisteredTeams().ToList(), (match, p1, p2) => match.SetParticipants(p1, p2)),
            _ => throw new InvalidOperationException($"Unsupported participation mode {game.ParticipationMode}.")
        };
    }

    private void DeterminePlacements<TParticipant>(Game game, List<TParticipant> participants, Func<TParticipant, Guid> getId, Func<TParticipant, Placement> createPlacement)
        where TParticipant : class
    {
        if (participants.Count == 0)
            throw new Exception("No participants in the game to determine placements.");

        var winCounts = participants.ToDictionary(
            getId,
            participant => game.Matches.Count(m => m.GetWinnerId() == getId(participant)));

        var ordered = participants
            .OrderByDescending(participant => winCounts[getId(participant)])
            .ThenBy(getId)
            .ToList();

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            for (int j = i + 1; j < ordered.Count; j++)
            {
                var participant1 = ordered[i];
                var participant2 = ordered[j];
                var participant1Id = getId(participant1);
                var participant2Id = getId(participant2);

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

    private IEnumerable<Match> GenerateMatchesForGame<TParticipant>(Game game, List<TParticipant> participants, Action<Match, TParticipant, TParticipant> assignParticipants)
        where TParticipant : class
    {
        var matches = new List<Match>();
        var rotation = new List<TParticipant?>(participants);
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

                assignParticipants(match, participant1, participant2);
                matches.Add(match);
            }

            var last = rotation[rotation.Count - 1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, last);
        }

        return matches;
    }
}

