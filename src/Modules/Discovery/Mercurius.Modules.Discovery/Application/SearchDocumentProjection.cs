namespace Mercurius.Modules.Discovery.Application;

internal sealed record SearchDocumentProjection(
    string EntityId,
    string Title,
    string Subtitle,
    string? ImageUrl,
    string Route);
