namespace Mercurius.Modules.Tournament.Contracts;

public sealed record RegistrationEligibility(
    bool Eligible,
    IReadOnlyList<string> ReasonCodes);
