using Mercurius.LAN.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class TeamInviteDTO
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
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
        UserId = teamInvite.UserId;
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
    public int UserId { get; set; }
}

public class RespondTeamInviteDTO
{
    [Required]
    public bool Accept { get; set; }
}
