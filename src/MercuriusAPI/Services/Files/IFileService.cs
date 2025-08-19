using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Files
{
    public interface IFileService
    {
        Task<string> SaveImageAsync(IFormFile image);
    }
}