using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Extensions.LAN
{
    public static class MatchExtensions
    {
        public static void AssignByeWinnersNextMatch(this IEnumerable<Match> matches)
        {
            foreach(var match in matches)
            {
                if(match.Winner == null || match.WinnerNextMatch == null)
                    continue;

                var targetMatch = match.WinnerNextMatch;

                if(match.MatchNumber % 2 != 0)
                    targetMatch.Participant1 = match.Winner;
                else
                    targetMatch.Participant2 = match.Winner;
            }
        }
    }
}
