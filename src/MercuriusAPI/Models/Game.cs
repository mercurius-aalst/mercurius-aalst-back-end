using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Models;

public class Game
{
    private const int MaxAverageGameDurationMinutes = 1440;

    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public int AverageGameDurationMinutes { get; set; }
    public int RoundBreakDurationMinutes { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }

    public IList<Placement> Placements { get; set; } = [];
    public GameSponsorPlacement? SponsorPlacement { get; set; }

    public IList<Match> Matches { get; set; } = new List<Match>();
    public IList<User> RegisteredUsers { get; set; } = [];
    public IList<Team> RegisteredTeams { get; set; } = [];

    public string RegisterFormUrl { get; set; }
    public string? ImageUrl { get; set; }

    public Game(
        string name,
        BracketType bracketType,
        GameFormat format,
        GameFormat finalsFormat,
        ParticipationMode participationMode,
        string registerFormUrl,
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        Status = GameStatus.Scheduled;
        ParticipationMode = participationMode;
        RegisterFormUrl = registerFormUrl;
        SetScheduleConfiguration(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes);
        Placements = new List<Placement>();
    }

    public Game(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipationMode participationMode, string registerFormUrl)
        : this(name, bracketType, format, finalsFormat, participationMode, registerFormUrl, DateTime.UtcNow, 30, 10)
    {
    }



    public Game()
    {
    }

    public void Update(
        string name,
        BracketType bracketType,
        GameFormat format,
        GameFormat finalsFormat,
        ParticipationMode participationMode,
        string registerFormUrl,
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        if (Status == GameStatus.InProgress || Status == GameStatus.Completed)
            throw new ValidationException("Game cannot be updated when it's in progress or completed.");
        if (ParticipationMode != participationMode && Matches.Any())
            throw new ValidationException("Participation mode cannot be changed once match generation has started.");
        if (Matches.Any() && ScheduleConfigurationChanged(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes))
            throw new ValidationException("Schedule configuration cannot be changed once match generation has started.");
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        ParticipationMode = participationMode;
        RegisterFormUrl = registerFormUrl;
        SetScheduleConfiguration(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes);
    }

    public void Update(string name, BracketType bracketType, GameFormat format, GameFormat finalsFormat, ParticipationMode participationMode, string registerFormUrl)
    {
        var plannedStart = PlannedStartTime == DateTime.MinValue ? DateTime.UtcNow : PlannedStartTime;
        var averageMinutes = AverageGameDurationMinutes <= 0 ? 30 : AverageGameDurationMinutes;
        var breakMinutes = RoundBreakDurationMinutes <= 0 ? 10 : RoundBreakDurationMinutes;

        Update(name, bracketType, format, finalsFormat, participationMode, registerFormUrl, plannedStart, averageMinutes, breakMinutes);
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
        if (GetRegisteredParticipantCount() < 2)
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
        EstimatedEndTime = null;
        Matches.Clear();
        RegisteredUsers.Clear();
        RegisteredTeams.Clear();
        Placements.Clear();
    }

    public void RegisterUser(User user)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("This game only accepts individual registrations.");
        if (RegisteredUsers.Any(p => p.Id == user.Id))
            throw new ValidationException("User is already registered for this game.");
        RegisteredUsers.Add(user);
    }

    public void RegisterTeam(Team team)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("This game only accepts team registrations.");
        if (RegisteredTeams.Any(t => t.Id == team.Id))
            throw new ValidationException("Team is already registered for this game.");
        RegisteredTeams.Add(team);
    }

    public void RemoveUser(Guid userId)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Individual)
            throw new ValidationException("This game only accepts individual registrations.");
        var user = RegisteredUsers.FirstOrDefault(p => p.Id == userId);
        if (user is null)
            throw new NotFoundException($"{nameof(User)} not found for game {Name}");
        RegisteredUsers.Remove(user);
    }

    public void RemoveTeam(Guid teamId)
    {
        EnsureScheduledRegistrationState();
        if (ParticipationMode != ParticipationMode.Team)
            throw new ValidationException("This game only accepts team registrations.");
        var team = RegisteredTeams.FirstOrDefault(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found for game {Name}");
        RegisteredTeams.Remove(team);
    }

    public int GetRegisteredParticipantCount()
    {
        return ParticipationMode switch
        {
            ParticipationMode.Individual => RegisteredUsers.Count,
            ParticipationMode.Team => RegisteredTeams.Count,
            _ => 0
        };
    }

    private void EnsureScheduledRegistrationState()
    {
        if (Status != GameStatus.Scheduled)
            throw new ValidationException("Game must be scheduled for registrations.");
    }

    private void SetScheduleConfiguration(DateTime plannedStartTime, int averageGameDurationMinutes, int roundBreakDurationMinutes)
    {
        if (plannedStartTime == DateTime.MinValue)
            throw new ValidationException("Planned tournament start time is required.");
        if (averageGameDurationMinutes <= 0)
            throw new ValidationException("Average game duration must be greater than zero.");
        if (averageGameDurationMinutes > MaxAverageGameDurationMinutes)
            throw new ValidationException($"Average game duration cannot exceed {MaxAverageGameDurationMinutes} minutes.");
        if (roundBreakDurationMinutes <= 0)
            throw new ValidationException("Round break duration must be greater than zero.");
        PlannedStartTime = plannedStartTime;
        AverageGameDurationMinutes = averageGameDurationMinutes;
        RoundBreakDurationMinutes = roundBreakDurationMinutes;
    }

    private bool ScheduleConfigurationChanged(DateTime plannedStartTime, int averageGameDurationMinutes, int roundBreakDurationMinutes)
    {
        return PlannedStartTime != plannedStartTime
               || AverageGameDurationMinutes != averageGameDurationMinutes
               || RoundBreakDurationMinutes != roundBreakDurationMinutes;
    }
}
