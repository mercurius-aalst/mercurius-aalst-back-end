using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamInviteSummary(
    TeamInviteId Id,
    TeamId TeamId,
    string TeamName,
    string? TeamLogoUrl,
    UserId UserId,
    string? Username,
    TeamInviteStatus Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
