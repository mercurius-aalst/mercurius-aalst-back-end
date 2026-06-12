namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class CurrentUserTournamentRegistrationStateDTO
{
    public Guid GameId { get; set; }
    public TournamentRegistrationDTO? IndividualRegistration { get; set; }
    public TournamentRosterMemberDTO? PendingRosterConfirmation { get; set; }
    public TournamentRegistrationDTO? ActiveTeamRegistration { get; set; }
    public IReadOnlyList<TournamentRegistrationDTO> CaptainManagedRegistrations { get; set; } = [];
    public bool CanRegisterIndividual { get; set; }
    public bool CanConfirmRoster { get; set; }
    public bool CanUnregister { get; set; }
}
