namespace Mercurius.Modules.Discovery.Domain;

internal static class SearchDocumentTypes
{
    public const string User = "user";
    public const string Team = "team";
    public const string Tournament = "tournament";
    public const string Sponsor = "sponsor";

    public static short GetTypeOrder(string entityType) => entityType switch
    {
        User => 0,
        Team => 1,
        Tournament => 2,
        Sponsor => 3,
        _ => throw new InvalidOperationException($"Unsupported search document type '{entityType}'.")
    };
}
