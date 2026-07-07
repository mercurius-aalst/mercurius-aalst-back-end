namespace Mercurius.Modules.Identity.DTOs;

public sealed record Auth0ProfileSnapshot(string? Email, bool? EmailVerified, bool HasPasswordResetIdentity);
