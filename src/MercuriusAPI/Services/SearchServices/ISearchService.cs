using Mercurius.LAN.API.DTOs.SearchDTOs;

namespace Mercurius.LAN.API.Services.SearchServices;

public interface ISearchService
{
    Task<SearchResponseDTO> SearchAsync(string? query, string? cursor, int pageSize, CancellationToken cancellationToken = default);
}
