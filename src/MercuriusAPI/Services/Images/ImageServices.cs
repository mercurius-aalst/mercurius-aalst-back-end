using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.Images
{
    public class ImageService : IImageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public ImageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("AzureBlobStorage")["ConnectionString"];
            _containerName = configuration.GetSection("AzureBlobStorage")["ContainerName"];

            if(string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("AzureBlobStorage:ConnectionString is not configured in appsettings.json.");
            }
            if(string.IsNullOrEmpty(_containerName))
            {
                throw new ArgumentNullException("AzureBlobStorage:ContainerName is not configured in appsettings.json.");
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if(string.IsNullOrEmpty(fileUrl))
                return;

            var uri = new Uri(fileUrl);
            var fileName = Path.GetFileName(uri.LocalPath);

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // Delete the blob if it exists
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);

            using(var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            return blobClient.Uri.ToString();
        }
    }
}
