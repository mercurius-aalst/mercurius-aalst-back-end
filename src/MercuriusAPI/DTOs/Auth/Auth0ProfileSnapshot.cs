namespace Mercurius.LAN.API.DTOs.Auth;

public sealed record Auth0ProfileSnapshot(string? Email, bool? EmailVerified, bool HasPasswordResetIdentity);
