namespace Mercurius.Modules.Identity.DTOs;

internal sealed record Auth0ProfileSnapshot(string? Email, bool? EmailVerified, bool HasPasswordResetIdentity);
