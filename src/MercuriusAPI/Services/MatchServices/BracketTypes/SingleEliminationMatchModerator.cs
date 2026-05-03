using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Extensions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.MatchServices.Helpers;

namespace Mercurius.LAN.API.Services.MatchServices.BracketTypes;

/// <summary>
/// Handles the generation and management of matches for a single-elimination tournament.
/// </summary>
public class SingleEliminationMatchModerator : IMatchModerator
{
    /// <summary>
    /// Determines the placements of participants in the tournament.
    /// </summary>
    /// <param name="game">The game for which placements are to be determined.</param>
    public void DeterminePlacements(Game game)
    {
        if (game.Matches.Count == 0)
            return;

        // Assign 1st place to the winner of the final match
        var finalMatch = game.Matches
            .OrderByDescending(m => m.RoundNumber)
            .ThenByDescending(m => m.MatchNumber)
            .FirstOrDefault();

        if (finalMatch is null)
            return;

        switch (game.ParticipationMode)
        {
            case ParticipationMode.Individual:
                if (finalMatch.UserWinner is null)
                    throw new ValidationException("Final match has no winner assigned.");

                game.Placements.Add(new Placement
                {
                    GameId = game.Id,
                    Place = 1,
                    Users = [finalMatch.UserWinner]
                });
                break;
            case ParticipationMode.Team:
                if (finalMatch.TeamWinner is null)
                    throw new ValidationException("Final match has no winner assigned.");

                game.Placements.Add(new Placement
                {
                    GameId = game.Id,
                    Place = 1,
                    Teams = [finalMatch.TeamWinner]
                });
                break;
        }

        var matchesOrderedAndGroupedByRound = game.Matches
            .OrderByDescending(m => m.RoundNumber)
            .ThenByDescending(m => m.MatchNumber)
            .GroupBy(m => m.RoundNumber);

        int place = 2;

        foreach (var roundGrouping in matchesOrderedAndGroupedByRound)
        {
            switch (game.ParticipationMode)
            {
                case ParticipationMode.Individual:
                    var userLosers = roundGrouping.Where(m => m.UserLoser != null).Select(m => m.UserLoser!).ToList();
                    if (userLosers.Any())
                    {
                        game.Placements.Add(new Placement
                        {
                            GameId = game.Id,
                            Place = place,
                            Users = userLosers
                        });
                        place += userLosers.Count;
                    }
                    break;
                case ParticipationMode.Team:
                    var teamLosers = roundGrouping.Where(m => m.TeamLoser != null).Select(m => m.TeamLoser!).ToList();
                    if (teamLosers.Any())
                    {
                        game.Placements.Add(new Placement
                        {
                            GameId = game.Id,
                            Place = place,
                            Teams = teamLosers
                        });
                        place += teamLosers.Count;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Generates all matches for a given game in a single-elimination format.
    /// </summary>
    /// <param name="game">The game for which matches are to be generated.</param>
    /// <returns>A collection of matches for the game.</returns>
    public IEnumerable<Match> GenerateMatchesForGame(Game game)
    {
        return game.ParticipationMode switch
        {
            ParticipationMode.Individual => GenerateMatchesForGame(game, game.RegisteredUsers.OrderBy(_ => Guid.NewGuid()).ToList(), (match, p1, p2) => match.SetParticipants(p1, p2)),
            ParticipationMode.Team => GenerateMatchesForGame(game, game.RegisteredTeams.OrderBy(_ => Guid.NewGuid()).ToList(), (match, p1, p2) => match.SetParticipants(p1, p2)),
            _ => throw new ValidationException($"Unsupported participation mode {game.ParticipationMode}.")
        };
    }

    private IEnumerable<Match> GenerateMatchesForGame<TParticipant>(Game game, IReadOnlyList<TParticipant> participants, Action<Match, TParticipant?, TParticipant?> assignParticipants)
        where TParticipant : class
    {
        var matches = new List<Match>();
        int participantCount = participants.Count;
        int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(participantCount)));
        int totalMatches = nextPowerOfTwo - 1;
        int totalRounds = (int)Math.Ceiling(Math.Log2(participantCount));
        int firstRoundMatchCount = nextPowerOfTwo / 2;

        int[] slotOrder = SeedingHelper.GenerateBracketSlotOrder(nextPowerOfTwo);
        var slots = new TParticipant?[firstRoundMatchCount * 2];
        for (int i = 0; i < participants.Count; i++)
            slots[slotOrder[i]] = participants[i];

        int matchNumber = 1;
        int previousRound = totalRounds + 1;

        for (int i = 0; i < totalMatches; i++)
        {
            int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));

            if (round < previousRound)
                matchNumber = 1;
            else
                matchNumber++;

            var match = new Match
            {
                GameId = game.Id,
                RoundNumber = round,
                MatchNumber = matchNumber,
                BracketType = BracketType.SingleElimination,
                Format = game.Format,
                ParticipationMode = game.ParticipationMode
            };

            if (i >= totalMatches - firstRoundMatchCount)
            {
                int leafIndex = i - (totalMatches - firstRoundMatchCount);
                assignParticipants(match, slots[leafIndex * 2], slots[leafIndex * 2 + 1]);
                match.SetParticipantBYEs(!match.HasParticipant1(), !match.HasParticipant2());
                match.TryAssignByeWin();
            }

            previousRound = round;
            matches.Add(match);
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var current = matches[i];

            if (current.RoundNumber == 1)
                continue;

            int childMatchIndex1 = (i * 2) + 1;
            int childMatchIndex2 = (i * 2) + 2;

            if (childMatchIndex1 < matches.Count)
                matches[childMatchIndex1].WinnerNextMatch = current;
            if (childMatchIndex2 < matches.Count)
                matches[childMatchIndex2].WinnerNextMatch = current;
        }

        matches.AssignByeWinnersNextMatch();

        return matches;
    }
}

