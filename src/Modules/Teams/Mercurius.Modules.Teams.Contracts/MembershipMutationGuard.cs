namespace Mercurius.Modules.Teams.Contracts;

public sealed record MembershipMutationGuard(
    bool CanMutate,
    IReadOnlyList<string> ReasonCodes);
