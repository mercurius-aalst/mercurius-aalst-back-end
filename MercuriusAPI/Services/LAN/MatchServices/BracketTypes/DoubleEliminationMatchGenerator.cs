using MercuriusAPI.Models.LAN;
using System.Linq;

namespace MercuriusAPI.Services.LAN.MatchServices.BracketTypes
{
    public class DoubleEliminationMatchGenerator : IMatchGenerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            var matches = new List<Match>();
            GenerateUpperBracketMatches(game, game.Participants, matches);
            AssignByeWinnersNextMatch(matches);
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
                        MatchNumber = i+1,
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
                RoundNumber = -1,
                MatchNumber = matches.Max(m => m.MatchNumber) + 1,
                Format = game.FinalsFormat,
                BracketType = game.BracketType,
                ParticipantType = game.ParticipantType,
                IsLowerBracketMatch = false
            };
            matches.Add(grandFinalMatch);
        }

        private void AssignByeWinnersNextMatch(IEnumerable<Match> matches)
        {
            var matchesByRound = matches
        .Where(m => !m.IsLowerBracketMatch)
        .GroupBy(m => m.RoundNumber)
        .OrderBy(g => g.Key)
        .ToList();

            if(matchesByRound.Count < 2)
                return; // No second round to assign to

            var round1 = matchesByRound[0].OrderBy(m => m.MatchNumber).ToList();
            var round2 = matchesByRound[1].OrderBy(m => m.MatchNumber).ToList();

            for(int i = 0; i < round1.Count; i ++)
            {
                var match1 = round1[i];

                var targetMatch = round2[i / 2];

                if(match1.Winner != null)
                {
                    if(match1.MatchNumber % 2 != 0)
                        targetMatch.Participant1 = match1.Winner;
                    else
                        targetMatch.Participant2 = match1.Winner;
                }
            }
        }
    }
}
