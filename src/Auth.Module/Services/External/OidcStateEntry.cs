namespace Auth.Module.Services.External;

public class OidcStateEntry
{
    public string State { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public string CodeVerifier { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
