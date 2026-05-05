using System.Text.Json.Serialization;

namespace Auth.Module.Services.External;

public class OidcTokenResponse
{
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;
}
