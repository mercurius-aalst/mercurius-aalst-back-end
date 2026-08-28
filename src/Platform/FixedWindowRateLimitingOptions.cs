namespace Platform;

public sealed class FixedWindowRateLimitingOptions
{
    public int GlobalPermitLimit { get; init; } = 120;

    public int PolicyPermitLimit { get; init; } = 30;

    public TimeSpan Window { get; init; } = TimeSpan.FromSeconds(60);

    public required string UnconditionalPolicyName { get; init; }

    public required string ConditionalPolicyName { get; init; }

    public required string ConditionalQueryParameterName { get; init; }

    public string UserIdentifierClaimType { get; init; } = "sub";

    public string RejectionMessage { get; init; } = "Too many requests. Please try again later.";
}
