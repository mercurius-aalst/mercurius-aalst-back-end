using MercuriusAPI.Extensions.LAN;
using MercuriusAPI.Models.LAN;
using System.Linq;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class DoubleEliminationMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();
            GenerateUpperBracketMatches(game, game.Participants, matches);
            matches.AssignByeWinnersNextMatch();
            GenerateLowerBracketMatches(game, game.Participants, matches);
            GenerateGrandFinalMatch(game, matches);
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
            int matchNumber = 1;
            int previousRound = totalRounds + 1; //We're working top down in match generation

            for(int i = 0; i < totalMatches; i++)
            {
                // Determine round number
                int round = (int)Math.Floor(Math.Log2(nextPowerOfTwo)) - (int)Math.Floor(Math.Log2(i + 1));
                int matchesInThisRound = nextPowerOfTwo / (1 << (round - 1));
                int firstMatchIndex = totalMatches - matchesInThisRound;

                //If calculation a new round reset matchNumber, otherwise increase
                if(round < previousRound)
                    matchNumber = 1;
                else
                    matchNumber++;

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

                previousRound = round;

                matches.Add(match);
            }
        }
        private void GenerateLowerBracketMatches(Game game, IEnumerable<Participant> participants, List<Match> matches)
        {

            int upperBracketRounds = matches
                                         .Where(m => !m.IsLowerBracketMatch)
                                         .Max(m => m.RoundNumber);

            int totalLBRounds = (upperBracketRounds - 1) * 2;

            int matchesThisRound = matches
                .Count(m => !m.IsLowerBracketMatch && m.RoundNumber == 1) / 2;

            for(int round = 1; round <= totalLBRounds; round++)
            {
                for(int i = 0; i < matchesThisRound; i++)
                {
                    matches.Add(new Match
                    {
                        GameId = game.Id,
                        RoundNumber = round,
                        MatchNumber = i + 1,
                        Format = game.Format,
                        BracketType = game.BracketType,
                        ParticipantType = game.ParticipantType,
                        IsLowerBracketMatch = true
                    });
                }

                if(round < totalLBRounds / 2)
                {
                    matchesThisRound = (int)Math.Ceiling(matchesThisRound / 2.0) * 2;
                }
                else
                {
                    matchesThisRound = (int)Math.Ceiling(matchesThisRound / 2.0);
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

        public IEnumerable<Match> AssignParticipantsToNextMatch(Match previousMatch, Game game)
        {
            var returnList = new List<Match>();

            /*
             *Upper bracket: 
             *  Winner gets next match in upper bracket
             *    GF is last match in upper bracket
             *  Loser goes to lower bracket match with same round number
             *    For UB final: roundnumber + 1
             *Lower Bracket:
             *  Loser eliminated
             *  Winner goes next round in lower bracket
             *    For LB Final: same round number but upper bracket
            */

            if(previousMatch.IsLowerBracketMatch)
            {
                var targetLowerRoundMatch = AssignLoserParticipantNextMatch(previousMatch, game);
                if(targetLowerRoundMatch != null)
                    returnList.Add(targetLowerRoundMatch);
            }
            var targetUpperRoundMatch = AssignSingleElimParticipantNextMatch(previousMatch, game);
            if(targetUpperRoundMatch != null)
                returnList.Add(targetUpperRoundMatch);

            return returnList;
        }

        private Match AssignSingleElimParticipantNextMatch(Match previousMatch, Game game)
        {
            if(game.Matches.Any(m => m.RoundNumber > previousMatch.RoundNumber))
            {
                var nextUpperRoundMatches = game.Matches.Where(m => m.RoundNumber == previousMatch.RoundNumber + 1).OrderBy(m => m.MatchNumber).ToList();
                var targetUpperRoundMatch = nextUpperRoundMatches[previousMatch.MatchNumber / 2];

                if(previousMatch.MatchNumber % 2 != 0)
                    targetUpperRoundMatch.Participant1 = previousMatch.Winner;
                else
                    targetUpperRoundMatch.Participant2 = previousMatch.Winner;

                return targetUpperRoundMatch;
            }
            else
                return null;
        }

        private Match AssignLoserParticipantNextMatch(Match previousMatch, Game game)
        {
            var loser = previousMatch.WinnerId == previousMatch.Pariticipant1Id ? previousMatch.Participant1 : previousMatch.Participant2;
            throw new NotImplementedException();
        }
    }
}
