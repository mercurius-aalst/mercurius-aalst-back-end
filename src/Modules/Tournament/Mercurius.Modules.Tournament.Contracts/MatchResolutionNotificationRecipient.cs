using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record MatchResolutionNotificationRecipient(
    MatchResolutionNotificationRecipientKind Kind,
    UserId? UserId);
