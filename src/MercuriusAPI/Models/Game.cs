using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }

    public IList<Placement> Placements { get; set; } = [];

    public IList<Match> Matches { get; set; } = new List<Match>();
    public IList<Participant> Participants { get; set; } = [];

    public string RegisterFormUrl { get; set; }
    public string? ImageUrl { get; set; }

    public Game(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipationMode participationMode, string registerFormUrl)
    {
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        Status = GameStatus.Scheduled;
        ParticipationMode = participationMode;
        RegisterFormUrl = registerFormUrl;
        Placements = new List<Placement>();
    }



    public Game()
    {
    }

    public void Update(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipationMode participationMode, string registerFormUrl)
    {
        if (Status == GameStatus.InProgress || Status == GameStatus.Completed)
            throw new ValidationException("Game cannot be updated when it's in progress or completed.");
        if (ParticipationMode != participationMode && Matches.Any())
            throw new ValidationException("Participation mode cannot be changed once match generation has started.");
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        ParticipationMode = participationMode;
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

    public void RegisterUser(User user)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("This game only accepts individual registrations.");
        if (Participants.Any(p => p.Id == user.Id))
            throw new ValidationException("User is already registered for this game.");
        Participants.Add(user);
    }

    public void RegisterTeam(Team team)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("This game only accepts team registrations.");
        if (Participants.Any(p => p.Id == team.Id))
            throw new ValidationException("Team is already registered for this game.");
        Participants.Add(team);
    }

    public void RemoveUser(int userId)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("This game only accepts individual registrations.");
        var user = Participants.OfType<User>().FirstOrDefault(p => p.Id == userId);
        if (user is null)
            throw new NotFoundException($"{nameof(User)} not found for game {Name}");
        Participants.Remove(user);
    }

    public void RemoveTeam(int teamId)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("This game only accepts team registrations.");
        var team = Participants.OfType<Team>().FirstOrDefault(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found for game {Name}");
        Participants.Remove(team);
    }

    private void EnsureScheduledRegistrationState()
    {
        if (Status != GameStatus.Scheduled)
            throw new ValidationException("Game must be scheduled for registrations.");
    }
}
