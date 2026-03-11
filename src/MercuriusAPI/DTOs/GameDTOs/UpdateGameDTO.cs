using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class UpdateGameDTO
{
    public string Name { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public BracketType BracketType { get; set; }
    public IFormFile? Image { get; set; }
    public string RegisterFormUrl { get; set; }
}

