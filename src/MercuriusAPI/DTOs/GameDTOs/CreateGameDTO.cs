using Mercurius.LAN.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class CreateGameDTO
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipantType ParticipantType { get; set; }
    [Required]
    public IFormFile Image { get; set; }
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string RegisterFormUrl { get; set; }
}

