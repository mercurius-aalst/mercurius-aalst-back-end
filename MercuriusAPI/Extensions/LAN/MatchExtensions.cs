using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Extensions.LAN
{
    public static class MatchExtensions
    {
        public static void AssignByeWinnersNextMatch(this IEnumerable<Match> matches)
        {
            var matchesByRound = matches
       .Where(m => !m.IsLowerBracketMatch)
       .GroupBy(m => m.RoundNumber)
       .OrderBy(g => g.Key)
       .ToList();

            if(matchesByRound.Count < 2)
                return;

            var round1 = matchesByRound[0].OrderBy(m => m.MatchNumber).ToList();
            var round2 = matchesByRound[1].OrderBy(m => m.MatchNumber).ToList();

            for(int i = 0; i < round1.Count; i++)
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
