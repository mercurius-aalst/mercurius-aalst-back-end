namespace Auth.Module.Services.External;

public class GoogleOidcOptions
{
    public const string SectionName = "ExternalAuth:Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string MetadataAddress { get; set; } = "https://accounts.google.com/.well-known/openid-configuration";
}
