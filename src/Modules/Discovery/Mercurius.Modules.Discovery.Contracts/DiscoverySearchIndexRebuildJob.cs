namespace Mercurius.Modules.Discovery.Contracts;

public sealed record DiscoverySearchIndexRebuildJob(
    Guid Id,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Error);
