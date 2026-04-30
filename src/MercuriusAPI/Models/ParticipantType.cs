namespace Mercurius.LAN.API.Models;

public enum ParticipantType
{
    Team = 0,
    Player = 1
}

public enum ParticipationMode
{
    Team = 0,
    Individual = 1
}

public static class ParticipationModeMappings
{
    public static ParticipantType ToParticipantType(this ParticipationMode participationMode)
    {
        return participationMode switch
        {
            ParticipationMode.Team => ParticipantType.Team,
            ParticipationMode.Individual => ParticipantType.Player,
            _ => throw new ArgumentOutOfRangeException(nameof(participationMode), participationMode, "Unsupported participation mode.")
        };
    }

    public static ParticipationMode ToParticipationMode(this ParticipantType participantType)
    {
        return participantType switch
        {
            ParticipantType.Team => ParticipationMode.Team,
            ParticipantType.Player => ParticipationMode.Individual,
            _ => throw new ArgumentOutOfRangeException(nameof(participantType), participantType, "Unsupported participant type.")
        };
    }
}
