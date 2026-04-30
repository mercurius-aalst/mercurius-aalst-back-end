using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices.Helpers;

internal static class MatchModeParticipantHelper
{
    public static List<Participant> GetParticipantsForBracket(Game game)
    {
        var participants = game.ParticipationMode switch
        {
            ParticipationMode.Individual => game.Participants
                .OfType<Player>()
                .Cast<Participant>()
                .ToList(),
            ParticipationMode.Team => game.Participants
                .OfType<Team>()
                .Cast<Participant>()
                .ToList(),
            _ => throw new ValidationException($"Unsupported participation mode {game.ParticipationMode}.")
        };

        if (participants.Count != game.Participants.Count)
            throw new ValidationException($"Game participants are incompatible with {game.ParticipationMode} mode.");

        return participants;
    }

    public static void AssignParticipants(Match match, Participant? participant1, Participant? participant2)
    {
        switch (match.ParticipationMode)
        {
            case ParticipationMode.Individual:
                match.SetParticipants(AsPlayer(participant1), AsPlayer(participant2));
                return;
            case ParticipationMode.Team:
                match.SetParticipants(AsTeam(participant1), AsTeam(participant2));
                return;
            default:
                throw new ValidationException($"Unsupported participation mode {match.ParticipationMode}.");
        }
    }

    public static void AssignParticipant1(Match match, Participant? participant)
    {
        switch (match.ParticipationMode)
        {
            case ParticipationMode.Individual:
                match.SetParticipant1(AsPlayer(participant));
                return;
            case ParticipationMode.Team:
                match.SetParticipant1(AsTeam(participant));
                return;
            default:
                throw new ValidationException($"Unsupported participation mode {match.ParticipationMode}.");
        }
    }

    public static void AssignParticipant2(Match match, Participant? participant)
    {
        switch (match.ParticipationMode)
        {
            case ParticipationMode.Individual:
                match.SetParticipant2(AsPlayer(participant));
                return;
            case ParticipationMode.Team:
                match.SetParticipant2(AsTeam(participant));
                return;
            default:
                throw new ValidationException($"Unsupported participation mode {match.ParticipationMode}.");
        }
    }

    private static Player? AsPlayer(Participant? participant)
    {
        return participant switch
        {
            null => null,
            Player player => player,
            _ => throw new ValidationException("Team participant cannot be assigned to an individual match.")
        };
    }

    private static Team? AsTeam(Participant? participant)
    {
        return participant switch
        {
            null => null,
            Team team => team,
            _ => throw new ValidationException("Individual participant cannot be assigned to a team match.")
        };
    }
}
