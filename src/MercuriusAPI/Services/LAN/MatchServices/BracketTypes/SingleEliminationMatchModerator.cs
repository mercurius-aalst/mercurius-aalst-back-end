using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices.Helpers;
using System.Linq;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class SingleEliminationMatchModerator : IMatchModerator
    {
        public void DeterminePlacements(Game game)
        {
            if(game.Matches.Count == 0)
                return;

            // 1. Final match: assign 1st
            var finalMatch = game.Matches
                .OrderByDescending(m => m.RoundNumber)
                .ThenByDescending(m => m.MatchNumber)
                .FirstOrDefault();
            if(finalMatch.Winner is null)
                throw new ValidationException("Final match has no winner assigned.");

            game.Placements.Add(new Placement
            {
                GameId = game.Id,
                Place = 1,
                Participants = [finalMatch.Winner]
            });

            var matchesOrderedAndGroupedByRound = game.Matches
                .OrderByDescending(m => m.RoundNumber)
                .ThenByDescending(m => m.MatchNumber)
                .GroupBy(m => m.RoundNumber);
            // 2. All other losers, by round (descending)
            int place = 2;

            foreach(var roundGrouping in matchesOrderedAndGroupedByRound)
            {
                var losersThisRound = roundGrouping.Where(m => m.LoserId != null).Select(m => m.Loser).ToList();
                if(losersThisRound.Any())
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

        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();

            int participantCount = game.Participants.Count();
            var participants = game.Participants.OrderBy(_ => Guid.NewGuid()).ToList();
            int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(participantCount)));
            int totalMatches = nextPowerOfTwo - 1;

            int totalRounds = (int)Math.Ceiling(Math.Log2(participantCount));
            int firstRoundMatchCount = nextPowerOfTwo / 2;

            var shuffled = participants.OrderBy(_ => Guid.NewGuid()).ToList();

            int[] slotOrder = SeedingHelper.GenerateBracketSlotOrder(nextPowerOfTwo);
            var slots = new Participant[firstRoundMatchCount * 2];
            for(int i = 0; i < shuffled.Count; i++)
                slots[slotOrder[i]] = shuffled[i];

            int matchNumber = 1;
            int previousRound = totalRounds + 1; // We're working top down in match generation

            for(int i = 0; i < totalMatches; i++)
            {
                // Determine round number
                int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));
                int matchesInThisRound = nextPowerOfTwo / (1 << (round - 1));
                int firstMatchIndex = totalMatches - matchesInThisRound;

                // If calculation a new round reset matchNumber, otherwise increase
                if (round < previousRound)
                    matchNumber = 1;

                var match = new Match
                {
                    GameId = game.Id,
                    RoundNumber = round,
                    BracketType = game.BracketType,
                    Format = i == 0 ? game.FinalsFormat : game.Format,
                    MatchNumber = matchNumber,
                    ParticipantType = game.ParticipantType
                };

                // Assign participants only to first-round matches (the leaves)
                int firstRoundStart = totalMatches - firstRoundMatchCount;
                if(i >= firstRoundStart)
                {
                    int leafIndex = i - firstRoundStart;
                    match.Participant1 = slots[leafIndex * 2];
                    match.Participant2 = slots[leafIndex * 2 + 1];
                    match.TryAssignByeWin();
                }

                matches.Add(match);

                matchNumber++;
                previousRound = round;
            }

            for(int i = 0; i < matches.Count; i++)
            {
                var current = matches[i];

                if(current.RoundNumber == 1)
                    continue;

                int childMatchIndex1 = (i * 2) + 1;
                int childMatchIndex2 = (i * 2) + 2;

                matches[childMatchIndex1].WinnerNextMatch = current;
                matches[childMatchIndex2].WinnerNextMatch = current;
            }

            matches.AssignByeWinnersNextMatch();
            return matches;
        }
    }
}
