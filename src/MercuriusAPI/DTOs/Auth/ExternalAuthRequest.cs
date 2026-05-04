using Mercurius.LAN.API.Models.Auth;

namespace Mercurius.LAN.API.DTOs.Auth;

public class ExternalAuthRequest
{
    public ExternalAuthProvider Provider { get; set; }
    public string ProviderToken { get; set; } = string.Empty;
}
