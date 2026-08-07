namespace Mercurius.Modules.Discovery.Domain;

internal sealed class SearchIndexRebuildDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public short TypeOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Route { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public long SourceVersion { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
