using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Services.Images
{
    public class FileService : IFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly Dictionary<string, string>? _containers;

        public FileService(IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("ConnectionStrings")["AzureBlobStorage"];
            _containers = configuration.GetSection("AzureBlobStorage:Containers").Get<Dictionary<string, string>>();

            if(string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("AzureBlobStorage:ConnectionString is not configured in appsettings.json.");
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task DeleteFileAsync<T>(string fileUrl)
        {
            if(string.IsNullOrEmpty(fileUrl))
                return;

            var uri = new Uri(fileUrl);
            var fileName = Path.GetFileName(uri.LocalPath);

            var containerClient = GetContainerClientForType<T>();
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> UploadFileAsync<T>(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            var containerClient = GetContainerClientForType<T>();

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);

            using(var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            return blobClient.Uri.ToString();
        }

        private BlobContainerClient GetContainerClientForType<T>()
        {
            string entityTypeName = typeof(T).Name;

            if(!_containers.TryGetValue(entityTypeName, out string? containerName))
            {
                throw new ValidationException($"No blob storage container mapping found for entity type '{entityTypeName}'. Please configure 'AzureBlobStorage:ContainerMappings:{entityTypeName}' in appsettings.json.");
            }

            return _blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}
