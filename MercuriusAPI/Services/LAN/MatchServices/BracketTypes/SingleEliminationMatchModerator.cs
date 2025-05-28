using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using System.Linq;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class SingleEliminationMatchModerator : IMatchModerator
    {
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

            int[] slotOrder = GenerateBracketSlotOrder(nextPowerOfTwo);
            var slots = new Participant[firstRoundMatchCount * 2];
            for(int i = 0; i < shuffled.Count; i++)
                slots[slotOrder[i]] = shuffled[i];

            int matchNumber = 1;
            int previousRound = totalRounds + 1; //We're working top down in match generation

            for(int i = 0; i < totalMatches; i++)
            {
                // Determine round number
                int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));
                int matchesInThisRound = nextPowerOfTwo / (1 << (round - 1));
                int firstMatchIndex = totalMatches - matchesInThisRound;
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

                //If calculation a new round reset matchNumber, otherwise increase
                if(round < previousRound)
                    matchNumber = 1;
                else
                    matchNumber++;
                previousRound = round;

                matches.Add(match);
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

        // For 8 slots: [0, 7, 3, 4, 2, 5, 1, 6]
        private int[] GenerateBracketSlotOrder(int slotCount)
        {
            int[] result = new int[slotCount];
            int half = slotCount / 2;

            int middleLeft = half - 1;
            int middleRight = half;

            result[0] = 0;
            result[1] = slotCount - 1;

            for(int i = 2; i < slotCount; i++)
            {
                if(i % 2 == 0)
                {
                    result[i] = middleLeft;
                    middleLeft--;
                }
                else
                {
                    result[i] = middleRight;
                    middleRight++;
                }
            }

            return result;
        }
    }
}
