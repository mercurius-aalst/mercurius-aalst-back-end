using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices.Helpers;
using System.Linq;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class DoubleEliminationMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();
            GenerateUpperBracketMatches(game, game.Participants, matches);
            GenerateLowerBracketMatches(game, matches);
            GenerateGrandFinalMatch(game, matches);
            matches = AssignNextMatchesForDoubleElimination(matches).ToList();
            matches.AssignByeWinnersNextMatch();
            return matches;
        }
        private void GenerateUpperBracketMatches(Game game, IEnumerable<Participant> participants, List<Match> matches)
        {
            int participantCount = game.Participants.Count();
            int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(participantCount)));
            int totalMatches = nextPowerOfTwo - 1;

            int totalRounds = (int)Math.Ceiling(Math.Log2(participantCount));
            int firstRoundMatchCount = nextPowerOfTwo / 2;

            var shuffled = participants.OrderBy(_ => Guid.NewGuid()).ToList();
            int[] slotOrder = SeedingHelper.GenerateBracketSlotOrder(nextPowerOfTwo);
            var slots = new Participant[firstRoundMatchCount * 2];

            // Assign participants to slots based on the shuffled list
            for (int i = 0; i < shuffled.Count; i++)
                slots[slotOrder[i]] = shuffled[i];

            // Efficiently distribute BYEs to avoid BYE vs BYE matches
            for (int i = 0; i < slots.Length; i += 2)
            {
                if (slots[i] == null && slots[i + 1] == null)
                {
                    // Find a participant from a later slot to avoid BYE vs BYE
                    for (int j = i + 2; j < slots.Length; j++)
                    {
                        if (slots[j] != null)
                        {
                            slots[i] = slots[j]; // Move the participant to the current slot
                            slots[j] = null; // Clear the original slot to avoid duplication
                            break;
                        }
                    }
                }
            }

            int matchNumber = 1;
            int previousRound = totalRounds + 1; // We're working top down in match generation

            for (int i = 0; i < totalMatches; i++)
            {
                // Determine round number
                int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));
                int matchesInThisRound = nextPowerOfTwo / (1 << (round - 1));
                int firstMatchIndex = totalMatches - matchesInThisRound;

                // If calculation a new round reset matchNumber, otherwise increase
                if (round < previousRound)
                    matchNumber = 1;
                else
                    matchNumber++;

                var match = new Match
                {
                    GameId = game.Id,
                    RoundNumber = round,
                    BracketType = game.BracketType,
                    Format = game.Format,
                    MatchNumber = matchNumber,
                    ParticipantType = game.ParticipantType
                };

                // Assign participants only to first-round matches (the leaves)
                int firstRoundStart = totalMatches - firstRoundMatchCount;
                if (i >= firstRoundStart)
                {
                    int leafIndex = i - firstRoundStart;
                    match.Participant1 = slots[leafIndex * 2];
                    match.Participant2 = slots[leafIndex * 2 + 1];

                    match.SetParticipantBYEs(match.Participant1 is null, match.Participant2 is null);
                    match.TryAssignByeWin();
                }

                previousRound = round;

                matches.Add(match);
            }
        }

        private void GenerateLowerBracketMatches(Game game, List<Match> matches)
        {
            var upperBracketMatches = matches.Where(m => !m.IsLowerBracketMatch).ToList();
            if (!upperBracketMatches.Any())
                return;

            int upperBracketRounds = upperBracketMatches.Max(m => m.RoundNumber);
            int totalLBRounds = (upperBracketRounds - 1) * 2;

            // Dynamically calculate the number of matches for the first round of the lower bracket
            int matchesThisRound = (int)Math.Pow(2, upperBracketRounds - 2);

            for (int round = 1; round <= totalLBRounds; round++)
            {
                for (int i = 0; i < matchesThisRound; i++)
                {
                    var match = new Match
                    {
                        GameId = game.Id,
                        RoundNumber = round,
                        MatchNumber = i + 1,
                        Format = game.Format,
                        BracketType = game.BracketType,
                        ParticipantType = game.ParticipantType,
                        IsLowerBracketMatch = true
                    };

                    matches.Add(match);
                }

                // Adjust the match count for the next round.
                // The number of matches halves every two rounds (one entry round and one consolidation round).
                if (round % 2 == 0)
                {
                    matchesThisRound /= 2;
                }
            }
        }

        private void GenerateGrandFinalMatch(Game game, List<Match> matches)
        {
            var grandFinalMatch = new Match
            {
                GameId = game.Id,
                RoundNumber = matches.Max(m => m.RoundNumber) + 1,
                MatchNumber = 1,
                Format = game.FinalsFormat,
                BracketType = game.BracketType,
                ParticipantType = game.ParticipantType,
                IsLowerBracketMatch = false
            };
            matches.Add(grandFinalMatch);
        }
        private IEnumerable<Match> AssignNextMatchesForDoubleElimination(List<Match> matches)
        {
            // Separate matches and sort them in an ascending order for easier linking
            var uBMatches = matches.Where(m => !m.IsLowerBracketMatch && m.RoundNumber < matches.Max(x => x.RoundNumber)).OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var lBMatches = matches.Where(m => m.IsLowerBracketMatch).OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var grandFinal = matches.Single(m => !m.IsLowerBracketMatch && m.RoundNumber == matches.Max(x => x.RoundNumber));

            // Link Upper Bracket matches
            foreach (var currentUBMatch in uBMatches)
            {
                // Find the match in the next UB round where the winner will go
                var nextUBMatch = uBMatches.FirstOrDefault(m =>
                    m.RoundNumber == currentUBMatch.RoundNumber + 1 &&
                    m.MatchNumber == (int)Math.Ceiling((double)currentUBMatch.MatchNumber / 2));

                currentUBMatch.WinnerNextMatch = nextUBMatch;

                // Link UB losers to the correct LB match
                int targetLBRoundNumber = (currentUBMatch.RoundNumber <= 2)
                    ? currentUBMatch.RoundNumber
                    : (currentUBMatch.RoundNumber - 1) * 2;

                int nextLBMatchNumber = (currentUBMatch.RoundNumber == 1)
                    ? (int)Math.Ceiling((double)currentUBMatch.MatchNumber / 2)
                    : currentUBMatch.MatchNumber;

                var nextLBMatch = lBMatches.FirstOrDefault(m =>
                    m.RoundNumber == targetLBRoundNumber &&
                    m.MatchNumber == nextLBMatchNumber);

                PropagateBYEStatus(currentUBMatch, nextUBMatch, nextLBMatch);

                currentUBMatch.LoserNextMatch = nextLBMatch;
            }

            // Link Lower Bracket matches
            foreach (var currentLBMatch in lBMatches)
            {
                int nextLBMatchNumber = (currentLBMatch.RoundNumber % 2 != 0)
                    ? currentLBMatch.MatchNumber
                    : (int)Math.Ceiling((double)currentLBMatch.MatchNumber / 2);

                var nextLBMatch = lBMatches.FirstOrDefault(m =>
                    m.RoundNumber == currentLBMatch.RoundNumber + 1 &&
                    m.MatchNumber == nextLBMatchNumber);

                currentLBMatch.WinnerNextMatch = nextLBMatch;

                // Propagate BYE status for BYE vs BYE matches
                if (currentLBMatch.Participant1IsBYE && currentLBMatch.Participant2IsBYE && nextLBMatch != null)
                {
                    nextLBMatch.Participant2IsBYE = true;
                }
            }

            // Link the Final matches
            LinkFinalMatches(uBMatches, lBMatches, grandFinal);

            return uBMatches.Concat(lBMatches).Append(grandFinal);
        }

        private void PropagateBYEStatus(Match currentUBMatch, Match? nextUBMatch, Match? nextLBMatch)
        {
            if (currentUBMatch.Participant1IsBYE && currentUBMatch.Participant2IsBYE)
            {
                nextLBMatch?.SetParticipantBYEs(true, false);
                if (nextUBMatch != null)
                {
                    if (currentUBMatch.MatchNumber % 2 != 0)
                        nextUBMatch.Participant1IsBYE = true;
                    else
                        nextUBMatch.Participant2IsBYE = true;
                }
            }
            else if (currentUBMatch.Participant1IsBYE || currentUBMatch.Participant2IsBYE)
            {
                if(currentUBMatch.RoundNumber == 1 && currentUBMatch.MatchNumber % 2 != 0)
                    nextLBMatch.Participant1IsBYE = true;
                else
                    nextLBMatch.Participant2IsBYE = true;
            }
        }

        private void LinkFinalMatches(List<Match> uBMatches, List<Match> lBMatches, Match grandFinal)
        {
            var ubFinal = uBMatches.LastOrDefault(m => m.RoundNumber == uBMatches.Max(ub => ub.RoundNumber));
            var lbFinal = lBMatches.LastOrDefault();

            if (ubFinal != null)
            {
                ubFinal.WinnerNextMatch = grandFinal;
                ubFinal.LoserNextMatch = lbFinal;
            }
            if (lbFinal != null)
            {
                lbFinal.WinnerNextMatch = grandFinal;
            }

            // Grand final has no next matches
            grandFinal.WinnerNextMatch = null;
            grandFinal.LoserNextMatch = null;
        }

        public void DeterminePlacements(Game game)
        {

            // 1. Grand final (last upper bracket match)
            var grandFinal = game.Matches
                .Where(m => !m.IsLowerBracketMatch)
                .OrderByDescending(m => m.RoundNumber)
                .ThenByDescending(m => m.MatchNumber)
                .FirstOrDefault();

            if (grandFinal.Winner is null)
                throw new ValidationException("Grand final match has no winner assigned. Cannot determine placements.");
            if (grandFinal.Loser is null)
                throw new ValidationException("Grand final match has no loser assigned. Cannot determine placements.");

            game.Placements.Add(new Placement
            {
                GameId = game.Id,
                Participants = [grandFinal.Winner],
                Place = 1
            });
            game.Placements.Add(new Placement
            {
                GameId = game.Id,
                Participants = [grandFinal.Loser],
                Place = 2
            });

            // 2. Lower bracket (after gf, it's simply ranking by elimination (after third place, placings can be grouped in front end: 4-5, 6-7,)
            var lowerBracket = game.Matches
                .Where(m => m.IsLowerBracketMatch)
                .OrderByDescending(m => m.RoundNumber)
                .ThenByDescending(m => m.MatchNumber)
                .GroupBy(m => m.RoundNumber);

            int place = 3;
            foreach (var roundGrouping in lowerBracket)
            {
                var losersThisRound = roundGrouping.Where(m => m.LoserId != null).Select(m => m.Loser).ToList();
                if (losersThisRound.Any())
                {
                    game.Placements.Add(new Placement
                    {
                        Place = place,
                        GameId = game.Id,
                        Participants = losersThisRound
                    });
                    place += losersThisRound.Count;
                }
            }
        }
    }
}
