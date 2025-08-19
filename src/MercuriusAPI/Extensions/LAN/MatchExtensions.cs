using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Extensions.LAN
{
    public static class MatchExtensions
    {
        public static void AssignByeWinnersNextMatch(this IEnumerable<Match> matches)
        {
            foreach(var match in matches)
            {
                if(match.WinnerNextMatch == null)
                    continue;

                var targetMatch = match.WinnerNextMatch;

                if(match.Winner == null && match.Participant1IsBYE && match.Participant2IsBYE)
                {
                    if(match.MatchNumber % 2 != 0)
                        targetMatch.Participant1IsBYE = true;
                    else
                        targetMatch.Participant2IsBYE = true;
                }

                if(match.MatchNumber % 2 != 0)
                    targetMatch.Participant1 = match.Winner;
                else
                    targetMatch.Participant2 = match.Winner;
            }

            foreach(var match in matches)
            {

                match.TryAssignByeWin();
            }
        }
    }
}
