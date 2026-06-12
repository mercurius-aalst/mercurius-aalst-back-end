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
    [Required]
    public ParticipationMode? ParticipationMode { get; set; }
    [Required]
    public IFormFile Image { get; set; }
    public int? TeamSize { get; set; }
    [Required]
    public DateTime PlannedStartTime { get; set; }
    [Range(1, 1440)]
    public int AverageGameDurationMinutes { get; set; }
    [Range(1, int.MaxValue)]
    public int RoundBreakDurationMinutes { get; set; }
}

