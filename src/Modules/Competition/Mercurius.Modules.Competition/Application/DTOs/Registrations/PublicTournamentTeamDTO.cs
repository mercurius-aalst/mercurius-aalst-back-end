namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

public class PublicTournamentTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }

    public PublicTournamentTeamDTO()
    {
    }

}
