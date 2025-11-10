using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices.Helpers;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes;

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

        if (finalMatch.Winner is null)
            throw new ValidationException("Final match has no winner assigned.");

        game.Placements.Add(new Placement
        {
            GameId = game.Id,
            Place = 1,
            Participants = [finalMatch.Winner]
        });

        // Assign placements to other participants based on their elimination round
        var matchesOrderedAndGroupedByRound = game.Matches
            .OrderByDescending(m => m.RoundNumber)
            .ThenByDescending(m => m.MatchNumber)
            .GroupBy(m => m.RoundNumber);

        int place = 2;

        foreach (var roundGrouping in matchesOrderedAndGroupedByRound)
        {
            var losersThisRound = roundGrouping.Where(m => m.LoserId != null).Select(m => m.Loser).ToList();
            if (losersThisRound.Any())
            {
                game.Placements.Add(new Placement
                {
                    GameId = game.Id,
                    Place = place,
                    Participants = losersThisRound
                });
                place += losersThisRound.Count;
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
        var matches = new List<Match>();

        int participantCount = game.Participants.Count();
        var participants = game.Participants.OrderBy(_ => Guid.NewGuid()).ToList();
        int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(participantCount)));
        int totalMatches = nextPowerOfTwo - 1;

        int totalRounds = (int)Math.Ceiling(Math.Log2(participantCount));
        int firstRoundMatchCount = nextPowerOfTwo / 2;

        // Shuffle participants and assign them to slots
        int[] slotOrder = SeedingHelper.GenerateBracketSlotOrder(nextPowerOfTwo);
        var slots = new Participant[firstRoundMatchCount * 2];
        for (int i = 0; i < participants.Count; i++)
            slots[slotOrder[i]] = participants[i];

        int matchNumber = 1;
        int previousRound = totalRounds + 1;

        // Generate matches for all rounds
        for (int i = 0; i < totalMatches; i++)
        {
            // Calculate the round number for the current match
            int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));

            // Reset match number if a new round starts
            if (round < previousRound)
                matchNumber = 1;
            else
                matchNumber++;

            // Create a new match
            var match = new Match
            {
                GameId = game.Id,
                RoundNumber = round,
                MatchNumber = matchNumber,
                BracketType = BracketType.SingleElimination,
                Format = game.Format,
                ParticipantType = game.ParticipantType
            };

            // Assign participants to first-round matches
            if (i >= totalMatches - firstRoundMatchCount)
            {
                int leafIndex = i - (totalMatches - firstRoundMatchCount);
                match.Participant1 = slots[leafIndex * 2];
                match.Participant2 = slots[leafIndex * 2 + 1];

                // Handle BYE participants
                match.SetParticipantBYEs(match.Participant1 is null, match.Participant2 is null);
                match.TryAssignByeWin();
            }

            previousRound = round;
            matches.Add(match);
        }

        // Link matches to their next matches
        for (int i = 0; i < matches.Count; i++)
        {
            var current = matches[i];

            // Skip first-round matches as they have no children
            if (current.RoundNumber == 1)
                continue;

            // Calculate indices of child matches
            int childMatchIndex1 = (i * 2) + 1;
            int childMatchIndex2 = (i * 2) + 2;

            // Link child matches to the current match
            if (childMatchIndex1 < matches.Count)
                matches[childMatchIndex1].WinnerNextMatch = current;
            if (childMatchIndex2 < matches.Count)
                matches[childMatchIndex2].WinnerNextMatch = current;
        }

        // Assign BYE winners to their next matches
        matches.AssignByeWinnersNextMatch();

        return matches;
    }
}
