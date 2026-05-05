namespace Auth.Module.Models;

public record OidcUserInfo(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? GivenName,
    string? FamilyName);
