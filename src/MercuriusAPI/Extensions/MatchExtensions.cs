using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Extensions;

public static class MatchExtensions
{
    public static void AssignByeWinnersNextMatch(this IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            if (match.WinnerNextMatch == null)
                continue;

            var targetMatch = match.WinnerNextMatch;

            if (!match.HasWinner() && match.Participant1IsBYE && match.Participant2IsBYE)
            {
                if (match.MatchNumber % 2 != 0)
                    targetMatch.Participant1IsBYE = true;
                else
                    targetMatch.Participant2IsBYE = true;
            }

            if (!match.HasWinner())
                continue;

            switch (match.ParticipationMode)
            {
                case ParticipationMode.Individual:
                    if (match.MatchNumber % 2 != 0)
                        targetMatch.SetParticipant1(match.UserWinner);
                    else
                        targetMatch.SetParticipant2(match.UserWinner);
                    break;
                case ParticipationMode.Team:
                    if (match.MatchNumber % 2 != 0)
                        targetMatch.SetParticipant1(match.TeamWinner);
                    else
                        targetMatch.SetParticipant2(match.TeamWinner);
                    break;
            }
        }

        foreach (var match in matches)
        {
            match.TryAssignByeWin();
        }
    }
}

