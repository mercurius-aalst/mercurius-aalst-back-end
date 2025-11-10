using MercuriusAPI.Models.LAN;
using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.DTOs.LAN.TeamDTOs;

public class TeamInviteDTO
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public TeamInviteDTO()
    {

    }

    public TeamInviteDTO(TeamInvite teamInvite)
    {
        Id = teamInvite.Id;
        TeamId = teamInvite.TeamId;
        PlayerId = teamInvite.PlayerId;
        Status = teamInvite.Status.ToString();
        CreatedAt = teamInvite.CreatedAt;
        RespondedAt = teamInvite.RespondedAt;
    }
}

public class CreateTeamInviteDTO
{
    [Required]
    public int TeamId { get; set; }
    [Required]
    public int PlayerId { get; set; }
}

public class RespondTeamInviteDTO
{
    [Required]
    public bool Accept { get; set; }
}