namespace Mercurius.Modules.Competition.Contracts;

public sealed record RegistrationEligibility(
    bool Eligible,
    IReadOnlyList<string> ReasonCodes);
