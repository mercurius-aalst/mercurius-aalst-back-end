namespace Mercurius.Modules.Discovery.Domain;

internal sealed class SearchDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Route { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public long SourceVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
