using MercuriusAPI.Exceptions;
using MercuriusAPI.Services.LAN.GameServices;

namespace MercuriusAPI.Models.LAN;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string AcademicSeason { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipantType ParticipantType { get; set; }

    public IList<Placement> Placements { get; set; } = [];

    public IList<Match> Matches { get; set; } = new List<Match>();
    public IList<Participant> Participants { get; set; } = [];

    public string RegisterFormUrl { get; set; }
    public string? ImageUrl { get; set; }

    public Game(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipantType participantType, string registerFormUrl)
    {
        Name = name;
        AcademicSeason = AcademicSeasonHelper.GetCurrent();
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        Status = GameStatus.Scheduled;
        ParticipantType = participantType;
        RegisterFormUrl = registerFormUrl;
        Placements = new List<Placement>();
    }



    public Game()
    {
    }

    public void Update(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, string registerFormUrl)
    {
        if (Status == GameStatus.InProgress || Status == GameStatus.Completed)
            throw new ValidationException("Game cannot be updated when it's in progress or completed.");
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        RegisterFormUrl = registerFormUrl;
    }
    public void Cancel()
    {
        if (Status == GameStatus.Completed)
            throw new ValidationException("Game cannot be canceled when it's already completed.");
        Status = GameStatus.Canceled;
    }

    public void Start()
    {
        if (Status != GameStatus.Scheduled)
            throw new ValidationException("Game has to be scheduled to be able to start");
        if (Participants.Count < 2)
            throw new ValidationException("At least 2 participants required.");
        StartTime = DateTime.UtcNow;
        Status = GameStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != GameStatus.InProgress)
            throw new ValidationException("Game has to be in progress to be able to complete");
        EndTime = DateTime.UtcNow;
        Status = GameStatus.Completed;
    }

    public void Reset()
    {
        if (Status != GameStatus.Completed && Status != GameStatus.Canceled)
            throw new ValidationException("Game has to be completed or canceled to be able to reset");
        Status = GameStatus.Scheduled;
        StartTime = DateTime.MinValue;
        EndTime = DateTime.MinValue;
        Matches.Clear();
        Participants.Clear();
        Placements.Clear();
    }

    public void AddParticipant(Participant participant)
    {
        var expectedType = GetParticipantType();
        if (participant.GetType() != expectedType)
            throw new ValidationException($"This game only accepts {nameof(expectedType)}s as participants.");
        if (Status != GameStatus.Scheduled)
            throw new ValidationException("Game must be scheduled for registrations.");
        Participants.Add(participant);
    }
    public void RemoveParticipant(Participant participant)
    {
        if (Status != GameStatus.Scheduled)
            throw new ValidationException("Game must be scheduled for participant changes");
        if (!Participants.Any(p => p.Id == participant.Id))
            throw new NotFoundException($"{nameof(Participant)} not found for game {Name}");
        Participants.Remove(participant);
    }

    private Type GetParticipantType()
    {
        return ParticipantType switch
        {
            ParticipantType.Player => typeof(Player),
            ParticipantType.Team => typeof(Team),
            _ => typeof(Participant)
        };
    }
}