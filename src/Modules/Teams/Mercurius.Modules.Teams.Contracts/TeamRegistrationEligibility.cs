namespace Mercurius.Modules.Teams.Contracts;

public sealed record TeamRegistrationEligibility(
    bool Eligible,
    IReadOnlyList<string> ReasonCodes);
