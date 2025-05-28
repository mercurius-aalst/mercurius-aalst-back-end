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
            GenerateLowerBracketMatches(game, game.Participants, matches);
            GenerateGrandFinalMatch(game, matches);
            matches = AssignNextMatchesForDoubleElimination(matches).ToList();
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
        private IEnumerable<Match> AssignNextMatchesForDoubleElimination(List<Match> matches)
        {
            var updatedMatches = new List<Match>();
            // Separate upper and lower bracket matches and grand final for convenience
            var uBMatches = matches.Where(m => !m.IsLowerBracketMatch && m.RoundNumber < matches.Max(x => x.RoundNumber)).OrderByDescending(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var lBMatches = matches.Where(m => m.IsLowerBracketMatch).OrderBy(m => m.RoundNumber).ThenBy(m => m.MatchNumber).ToList();
            var grandFinal = matches.Single(m => !m.IsLowerBracketMatch && m.RoundNumber == matches.Max(x => x.RoundNumber));

            //Set navigation properties for Upper bracket matches
            for(int i = 0; i < uBMatches.Count; i++)
            {
                var current = uBMatches[i];

                //UB Final
                if(current == uBMatches.LastOrDefault())
                {
                    current.LoserNextMatch = lBMatches.LastOrDefault();
                }                
                else
                {
                    int targetLBRoundNumber = (current.RoundNumber - 1) * 2 + 1;
                    int targetLBMatchNumber;
                    //UB Round 1
                    if(current.RoundNumber == 1)
                        targetLBMatchNumber = (int)Math.Ceiling((double)current.MatchNumber / 2);
                    //All other UB Rounds
                    else
                    {
                        int numMatchesInCurrentUBRound = (int)Math.Pow(2, uBMatches.Where(m => m.RoundNumber == current.RoundNumber).Count() - current.RoundNumber);

                        targetLBMatchNumber = numMatchesInCurrentUBRound - current.MatchNumber + 1;
                    }

                    current.LoserNextMatch = lBMatches.FirstOrDefault(m => m.RoundNumber == targetLBRoundNumber && m.MatchNumber == targetLBMatchNumber);
                }

                //Already first round, so can't go deeper to set parents
                if(current.RoundNumber == 1)
                    continue;

                int childMatchIndex1 = (i * 2) + 1;
                int childMatchIndex2 = (i * 2) + 2;

                uBMatches[childMatchIndex1].WinnerNextMatch = current;
                uBMatches[childMatchIndex2].WinnerNextMatch = current;
              
            }

            foreach(var currentLBMatch in lBMatches)
            {
                // LB Final winner goes to Grand Final
                if(currentLBMatch == lBMatches.LastOrDefault())
                {
                    currentLBMatch.WinnerNextMatch = grandFinal;
                    continue;
                }

                int nextLBRoundNumber = currentLBMatch.RoundNumber + 1;
                int nextLBMatchNumber;


                //Consolidation round winner (lb winner vs lb winner)
                if(currentLBMatch.RoundNumber % 2 != 0)
                    nextLBMatchNumber = currentLBMatch.MatchNumber;
                //Entry round winner (lb winner vs ub loser)
                else
                    nextLBMatchNumber = (int)Math.Ceiling((double)currentLBMatch.MatchNumber / 2);

                currentLBMatch.WinnerNextMatch = lBMatches.FirstOrDefault(m => m.RoundNumber == nextLBRoundNumber && m.MatchNumber == nextLBMatchNumber);

            }


            // 3) Grand final: no next matches
            grandFinal.WinnerNextMatch = null;
            grandFinal.LoserNextMatch = null;

            updatedMatches.AddRange(uBMatches);
            updatedMatches.AddRange(lBMatches);
            updatedMatches.Add(grandFinal);

            return updatedMatches;
        }   
    }
}
