using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    /// <summary>
    /// Handles the generation and management of matches for a double-elimination tournament.
    /// </summary>
    public class DoubleEliminationMatchModerator : IMatchModerator
    {
        /// <summary>
        /// Generates all matches for a given game in a double-elimination format.
        /// </summary>
        /// <param name="game">The game for which matches are to be generated.</param>
        /// <returns>A collection of matches for the game.</returns>
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();

            // Generate upper and lower bracket matches in parallel
            Parallel.Invoke(
                () => GenerateUpperBracketMatches(game, game.Participants, matches),
                () => GenerateLowerBracketMatches(game, matches)
            );

            // Generate the grand final match
            GenerateGrandFinalMatch(game, matches);

            // Assign next matches for double elimination and handle BYE winners
            int maxRoundNumber = matches.Max(x => x.RoundNumber);
            matches = AssignNextMatchesForDoubleElimination(matches, maxRoundNumber).ToList();
            matches.AssignByeWinnersNextMatch();

            return matches;
        }

        /// <summary>
        /// Generates matches for the upper bracket of the tournament.
        /// </summary>
        /// <param name="game">The game for which matches are to be generated.</param>
        /// <param name="participants">The participants in the tournament.</param>
        /// <param name="matches">The list to which generated matches will be added.</param>
        private void GenerateUpperBracketMatches(Game game, IEnumerable<Participant> participants, List<Match> matches)
        {
            int participantCount = game.Participants.Count();
            int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(participantCount)));
            int totalMatches = nextPowerOfTwo - 1;
            int totalRounds = (int)Math.Ceiling(Math.Log2(participantCount));
            int firstRoundMatchCount = nextPowerOfTwo / 2;

            var slots = AssignParticipantsToSlots(participants, firstRoundMatchCount, nextPowerOfTwo);

            int matchNumber = 1;
            int previousRound = totalRounds + 1;

            for (int i = 0; i < totalMatches; i++)
            {
                int round = CalculateRoundNumber(nextPowerOfTwo, i);

                if (round < previousRound)
                    matchNumber = 1;
                else
                    matchNumber++;

                var match = CreateMatch(game, round, matchNumber);

                if (i >= totalMatches - firstRoundMatchCount)
                {
                    AssignParticipantsToMatch(slots, match, i, totalMatches, firstRoundMatchCount);
                }

                previousRound = round;
                matches.Add(match);
            }
        }

        /// <summary>
        /// Assigns participants to slots for the first round of the tournament.
        /// </summary>
        /// <param name="participants">The participants in the tournament.</param>
        /// <param name="firstRoundMatchCount">The number of matches in the first round.</param>
        /// <param name="nextPowerOfTwo">The next power of two greater than or equal to the number of participants.</param>
        /// <returns>An array of participants assigned to slots.</returns>
        private Participant[] AssignParticipantsToSlots(IEnumerable<Participant> participants, int firstRoundMatchCount, int nextPowerOfTwo)
        {
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

            return slots;
        }

        /// <summary>
        /// Calculates the round number for a given match index.
        /// </summary>
        /// <param name="nextPowerOfTwo">The next power of two greater than or equal to the number of participants.</param>
        /// <param name="matchIndex">The index of the match.</param>
        /// <returns>The round number for the match.</returns>
        private int CalculateRoundNumber(int nextPowerOfTwo, int matchIndex)
        {
            return (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(matchIndex + 1));
        }

        /// <summary>
        /// Creates a new match for the tournament.
        /// </summary>
        /// <param name="game">The game for which the match is being created.</param>
        /// <param name="round">The round number of the match.</param>
        /// <param name="matchNumber">The match number within the round.</param>
        /// <returns>A new match object.</returns>
        private Match CreateMatch(Game game, int round, int matchNumber)
        {
            return new Match
            {
                GameId = game.Id,
                RoundNumber = round,
                BracketType = game.BracketType,
                Format = game.Format,
                MatchNumber = matchNumber,
                ParticipantType = game.ParticipantType
            };
        }

        /// <summary>
        /// Assigns participants to a match for the first round.
        /// </summary>
        /// <param name="slots">The array of participants assigned to slots.</param>
        /// <param name="match">The match to which participants are being assigned.</param>
        /// <param name="matchIndex">The index of the match.</param>
        /// <param name="totalMatches">The total number of matches in the tournament.</param>
        /// <param name="firstRoundMatchCount">The number of matches in the first round.</param>
        private void AssignParticipantsToMatch(Participant[] slots, Match match, int matchIndex, int totalMatches, int firstRoundMatchCount)
        {
            int firstRoundStart = totalMatches - firstRoundMatchCount;
            int leafIndex = matchIndex - firstRoundStart;
            match.Participant1 = slots[leafIndex * 2];
            match.Participant2 = slots[leafIndex * 2 + 1];

            match.SetParticipantBYEs(match.Participant1 is null, match.Participant2 is null);
            match.TryAssignByeWin();
        }

        /// <summary>
        /// Generates matches for the lower bracket of the tournament.
        /// </summary>
        /// <param name="game">The game for which matches are to be generated.</param>
        /// <param name="matches">The list to which generated matches will be added.</param>
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

        /// <summary>
        /// Generates the grand final match for the tournament.
        /// </summary>
        /// <param name="game">The game for which the grand final match is being generated.</param>
        /// <param name="matches">The list to which the grand final match will be added.</param>
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

        /// <summary>
        /// Links matches for a double-elimination tournament.
        /// </summary>
        /// <param name="matches">The list of matches in the tournament.</param>
        /// <param name="maxRoundNumber">The maximum round number in the tournament.</param>
        /// <returns>A collection of matches with linked next matches.</returns>
        private IEnumerable<Match> AssignNextMatchesForDoubleElimination(List<Match> matches, int maxRoundNumber)
        {
            // Separate matches and sort them in an ascending order for easier linking
            var uBMatches = matches.Where(m => !m.IsLowerBracketMatch && m.RoundNumber < maxRoundNumber).OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var lBMatches = matches.Where(m => m.IsLowerBracketMatch).OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var grandFinal = matches.Single(m => !m.IsLowerBracketMatch && m.RoundNumber == maxRoundNumber);

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

        /// <summary>
        /// Propagates BYE status to the next matches in the tournament.
        /// </summary>
        /// <param name="currentUBMatch">The current upper bracket match.</param>
        /// <param name="nextUBMatch">The next upper bracket match.</param>
        /// <param name="nextLBMatch">The next lower bracket match.</param>
        private void PropagateBYEStatus(Match currentUBMatch, Match? nextUBMatch, Match? nextLBMatch)
        {
            if (currentUBMatch.Participant1IsBYE && currentUBMatch.Participant2IsBYE)
            {
                nextLBMatch?.SetParticipantBYEs(nextLBMatch.MatchNumber % 2 != 0, nextLBMatch.MatchNumber % 2 == 0);
                if (nextUBMatch != null)
                {
                    nextUBMatch.SetParticipantBYEs(currentUBMatch.MatchNumber % 2 != 0, currentUBMatch.MatchNumber % 2 == 0);
                }
            }
            else if (currentUBMatch.Participant1IsBYE || currentUBMatch.Participant2IsBYE)
            {
                nextLBMatch?.SetParticipantBYEs(currentUBMatch.RoundNumber != 1 || currentUBMatch.MatchNumber % 2 != 0,
                                                currentUBMatch.RoundNumber == 1 && currentUBMatch.MatchNumber % 2 == 0);
            }
        }

        /// <summary>
        /// Links the final matches in the tournament.
        /// </summary>
        /// <param name="uBMatches">The list of upper bracket matches.</param>
        /// <param name="lBMatches">The list of lower bracket matches.</param>
        /// <param name="grandFinal">The grand final match.</param>
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

        /// <summary>
        /// Determines the placements of participants in the tournament.
        /// </summary>
        /// <param name="game">The game for which placements are to be determined.</param>
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
