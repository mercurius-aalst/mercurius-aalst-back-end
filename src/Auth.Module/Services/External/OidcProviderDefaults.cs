namespace Auth.Module.Services.External;

public static class OidcProviderDefaults
{
    public static OidcProviderOptions Apply(string providerName, OidcProviderOptions options)
    {
        if (!string.Equals(providerName, "google", StringComparison.OrdinalIgnoreCase))
            return options;

        if (string.IsNullOrWhiteSpace(options.MetadataAddress))
            options.MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";

        if (options.ValidIssuers.Count == 0)
        {
            options.ValidIssuers =
            [
                "https://accounts.google.com",
                "accounts.google.com"
            ];
        }

        if (options.AdditionalAuthorizationParameters.Count == 0)
        {
            options.AdditionalAuthorizationParameters = new Dictionary<string, string>
            {
                ["access_type"] = "offline",
                ["prompt"] = "consent"
            };
        }

        return options;
    }
}
