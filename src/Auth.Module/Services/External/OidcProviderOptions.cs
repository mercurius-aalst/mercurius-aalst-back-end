namespace Auth.Module.Services.External;

public class OidcProviderOptions
{
    public const string SectionName = "ExternalAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string MetadataAddress { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = ["openid", "email", "profile"];
    public List<string> ValidIssuers { get; set; } = [];
    public Dictionary<string, string> AdditionalAuthorizationParameters { get; set; } = [];

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ClientId);
}
