using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;

namespace Mercurius.Modules.Competition.Application;

internal sealed record RegistrationMappingContext(
    IReadOnlyDictionary<UserId, UserProfileSummary> Users,
    IReadOnlyDictionary<TeamId, TeamRosterSnapshot> Teams);
