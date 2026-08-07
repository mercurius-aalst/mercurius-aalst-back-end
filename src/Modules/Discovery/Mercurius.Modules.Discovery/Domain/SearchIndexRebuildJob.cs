namespace Mercurius.Modules.Discovery.Domain;

internal sealed class SearchIndexRebuildJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SearchIndexRebuildJobStatus Status { get; set; } = SearchIndexRebuildJobStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Error { get; set; }
}
