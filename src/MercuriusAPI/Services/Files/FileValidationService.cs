
using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Services.Files
{
    public class FileValidationService : IFileService
    {
        private readonly IFileService _innerService;
        private readonly IConfiguration _configuration;
        private readonly int _maxFileSizeInMB;


        public FileValidationService(IFileService innerService, IConfiguration configuration)
        {
            _innerService = innerService;
            _configuration = configuration;
            _maxFileSizeInMB = _configuration.GetValue<int>("FileStorage:MaxFileSizeInMB");

        }
        public Task<string> SaveImageAsync(IFormFile image)
        {
            if(image is null)                
                throw new ValidationException("No file provided");
            if(image.Length == 0)
                throw new ValidationException("Empty file provided");
            if(image.Length > _maxFileSizeInMB * 1024 * 1024)
                throw new ValidationException($"File too big, maximum file size is {_maxFileSizeInMB}MB");
            return _innerService.SaveImageAsync(image);
        }
    }
}
