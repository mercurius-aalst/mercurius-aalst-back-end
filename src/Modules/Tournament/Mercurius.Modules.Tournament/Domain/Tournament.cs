using Mercurius.Modules.Shared.Exceptions;

namespace Mercurius.Modules.Tournament.Domain;

internal sealed class Tournament
{
    private const int MaxAverageGameDurationMinutes = 1440;
    internal const int MaximumTeamSize = 50;

    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public int AverageGameDurationMinutes { get; set; }
    public int RoundBreakDurationMinutes { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public TournamentStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public int? TeamSize { get; set; }
    public IList<Placement> Placements { get; set; } = [];
    public IList<Match> Matches { get; set; } = [];
    public IList<TournamentRegistration> TournamentRegistrations { get; set; } = [];
    public string? ImageUrl { get; set; }

    public Tournament(
        string name,
        BracketType bracketType,
        GameFormat format,
        GameFormat finalsFormat,
        ParticipationMode participationMode,
        int? teamSize,
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        Status = TournamentStatus.Scheduled;
        ParticipationMode = participationMode;
        SetTeamSize(teamSize);
        SetScheduleConfiguration(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes);
    }

    public Tournament(
        string name,
        BracketType bracketType,
        GameFormat format,
        GameFormat finalsFormat,
        ParticipationMode participationMode,
        int? teamSize = null)
        : this(name, bracketType, format, finalsFormat, participationMode, teamSize, DateTime.UtcNow, 30, 10)
    {
    }

    public Tournament()
    {
    }

    public void Update(
        string name,
        BracketType bracketType,
        GameFormat format,
        GameFormat finalsFormat,
        ParticipationMode participationMode,
        int? teamSize,
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        if (Status is TournamentStatus.InProgress or TournamentStatus.Completed)
            throw new ValidationException("Tournament cannot be updated when it's in progress or completed.");
        if (ParticipationMode != participationMode && (Matches.Count != 0 || HasRegistrations()))
            throw new ValidationException("Participation mode cannot be changed once registration or match generation has started.");
        if (Matches.Count != 0 && ScheduleConfigurationChanged(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes))
            throw new ValidationException("Schedule configuration cannot be changed once match generation has started.");
        if (TeamSizeChanged(teamSize) && (Matches.Count != 0 || HasRegistrations()))
            throw new ValidationException("Team size cannot be changed once registration or match generation has started.");

        Name = name;
        BracketType = bracketType;
        Format = format;
        FinalsFormat = finalsFormat;
        ParticipationMode = participationMode;
        SetTeamSize(teamSize);
        SetScheduleConfiguration(plannedStartTime, averageGameDurationMinutes, roundBreakDurationMinutes);
    }

    public void Cancel()
    {
        if (Status == TournamentStatus.Completed)
            throw new ValidationException("Tournament cannot be canceled when it's already completed.");
        Status = TournamentStatus.Canceled;
    }

    public void Start()
    {
        if (Status != TournamentStatus.Scheduled)
            throw new ValidationException("Tournament has to be scheduled to be able to start");
        if (GetRegisteredParticipantCount() < 2)
            throw new ValidationException("At least 2 participants required.");

        StartTime = DateTime.UtcNow;
        Status = TournamentStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != TournamentStatus.InProgress)
            throw new ValidationException("Tournament has to be in progress to be able to complete");

        EndTime = DateTime.UtcNow;
        Status = TournamentStatus.Completed;
    }

    public void Reset()
    {
        if (Status is not (TournamentStatus.Completed or TournamentStatus.Canceled))
            throw new ValidationException("Tournament has to be completed or canceled to be able to reset");

        Status = TournamentStatus.Scheduled;
        StartTime = DateTime.MinValue;
        EndTime = DateTime.MinValue;
        EstimatedEndTime = null;
        Matches.Clear();
        Placements.Clear();
    }

    public int GetRegisteredParticipantCount()
    {
        var activeRegistrations = TournamentRegistrations
            .Where(registration => registration.Status == TournamentRegistrationStatus.Active);

        return ParticipationMode switch
        {
            ParticipationMode.Individual => activeRegistrations.Count(registration => registration.Kind == TournamentRegistrationKind.Individual),
            ParticipationMode.Team => activeRegistrations.Count(registration => registration.Kind == TournamentRegistrationKind.Team),
            _ => 0
        };
    }

    public IReadOnlyList<Guid> GetActiveRegisteredUserIds()
    {
        return TournamentRegistrations
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Individual &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.UserId.HasValue)
            .Select(registration => registration.UserId!.Value)
            .ToList();
    }

    public IReadOnlyList<Guid> GetActiveRegisteredTeamIds()
    {
        return TournamentRegistrations
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Team &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.TeamId.HasValue)
            .Select(registration => registration.TeamId!.Value)
            .ToList();
    }

    private void SetScheduleConfiguration(
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        if (plannedStartTime == DateTime.MinValue)
            throw new ValidationException("Planned tournament start time is required.");
        if (averageGameDurationMinutes <= 0)
            throw new ValidationException("Average tournament duration must be greater than zero.");
        if (averageGameDurationMinutes > MaxAverageGameDurationMinutes)
            throw new ValidationException($"Average tournament duration cannot exceed {MaxAverageGameDurationMinutes} minutes.");
        if (roundBreakDurationMinutes <= 0)
            throw new ValidationException("Round break duration must be greater than zero.");

        PlannedStartTime = plannedStartTime;
        AverageGameDurationMinutes = averageGameDurationMinutes;
        RoundBreakDurationMinutes = roundBreakDurationMinutes;
    }

    private void SetTeamSize(int? teamSize)
    {
        if (ParticipationMode == ParticipationMode.Team)
        {
            if (!teamSize.HasValue || teamSize.Value <= 0)
                throw new ValidationException("Team tournaments require a team size greater than zero.");
            if (teamSize.Value > MaximumTeamSize)
                throw new ValidationException($"Team tournament size cannot exceed {MaximumTeamSize}.");

            TeamSize = teamSize.Value;
            return;
        }

        TeamSize = null;
    }

    private bool TeamSizeChanged(int? teamSize)
    {
        var normalizedTeamSize = ParticipationMode == ParticipationMode.Team ? teamSize : null;
        return TeamSize != normalizedTeamSize;
    }

    private bool HasRegistrations() => TournamentRegistrations.Count != 0;

    private bool ScheduleConfigurationChanged(
        DateTime plannedStartTime,
        int averageGameDurationMinutes,
        int roundBreakDurationMinutes)
    {
        return PlannedStartTime != plannedStartTime
               || AverageGameDurationMinutes != averageGameDurationMinutes
               || RoundBreakDurationMinutes != roundBreakDurationMinutes;
    }
}
