using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamSummary(
    TeamId Id,
    string Name,
    UserId? CaptainUserId,
    string? LogoUrl,
    bool IsDeleted);
