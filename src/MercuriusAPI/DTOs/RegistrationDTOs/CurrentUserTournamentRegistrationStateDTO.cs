namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class CurrentUserTournamentRegistrationStateDTO
{
    public Guid GameId { get; set; }
    public TournamentRegistrationDTO? IndividualRegistration { get; set; }
    public TournamentRosterMemberDTO? PendingRosterSelection { get; set; }
    public TournamentRegistrationDTO? ActiveTeamRegistration { get; set; }
    public IReadOnlyList<TournamentRegistrationDTO> CaptainManagedRegistrations { get; set; } = [];
    public bool CanRegisterIndividual { get; set; }
    public bool CanRespondToRosterSelection { get; set; }
    public bool CanUnregister { get; set; }
}
