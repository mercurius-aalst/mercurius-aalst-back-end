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
                    match.Participant1 = leafIndex * 2 < shuffled.Count ? shuffled[leafIndex * 2] : null;
                    match.Participant2 = leafIndex * 2 + 1 < shuffled.Count ? shuffled[leafIndex * 2 + 1] : null;
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
    }
}
