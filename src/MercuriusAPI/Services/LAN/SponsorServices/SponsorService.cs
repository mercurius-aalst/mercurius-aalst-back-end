using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.SponsorDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.Files;

namespace MercuriusAPI.Services.LAN.SponsorServices
{
    public class SponsorService : ISponsorService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly IFileService _fileService;

        public SponsorService(MercuriusDBContext dbContext, IFileService fileService)
        {
            _dbContext = dbContext;
            _fileService = fileService;
        }
        public IEnumerable<GetSponsorDTO> GetSponsors()
        {
            return _dbContext.Sponsors.Select(sp => new GetSponsorDTO(sp));
        }

        public async Task<GetSponsorDTO> GetSponsorByIdAsync(int id)
        {
            var sponsor = await _dbContext.Sponsors.FindAsync(id);
            if(sponsor is null)
                throw new NotFoundException($"Sponsor with ID {id} not found");
            return new GetSponsorDTO(sponsor);
        }

        public async Task<GetSponsorDTO> CreateSponsorAsync(CreateSponsorDTO sponsorDTO)
        {
            if(string.IsNullOrWhiteSpace(sponsorDTO.Name))
                throw new ValidationException("Sponsor name cannot be empty");

            var logoUrl = await _fileService.SaveImageAsync(sponsorDTO.Logo);
            var sponsor = new Sponsor
            {
                Name = sponsorDTO.Name,
                SponsorTier = sponsorDTO.SponsorTier,
                LogoUrl = logoUrl,
                InfoUrl = sponsorDTO.InfoUrl
            };
            _dbContext.Sponsors.Add(sponsor);
            await _dbContext.SaveChangesAsync();
            return new GetSponsorDTO(sponsor);
        }

        public async Task UpdateSponsorAsync(int id, UpdateSponsorDTO sponsorDTO)
        {
            var sponsor = await _dbContext.Sponsors.FindAsync(id);
            if(sponsor is null)
                throw new NotFoundException($"Sponsor with ID {id} not found");
            if(!string.IsNullOrWhiteSpace(sponsorDTO.Name))
                sponsor.Name = sponsorDTO.Name;

            if(sponsorDTO.Logo != null)
            {
                var logoUrl = await _fileService.SaveImageAsync(sponsorDTO.Logo);
                sponsor.LogoUrl = logoUrl;
            }
            sponsor.InfoUrl = sponsorDTO.InfoUrl;
            sponsor.SponsorTier = sponsorDTO.SponsorTier;
            _dbContext.Sponsors.Update(sponsor);
            await _dbContext.SaveChangesAsync();
        }

        public Task DeleteSponsorAsync(int id)
        {
            var sponsor = _dbContext.Sponsors.Find(id);
            if(sponsor is null)
                throw new NotFoundException($"Sponsor with ID {id} not found");
            _dbContext.Sponsors.Remove(sponsor);
            return _dbContext.SaveChangesAsync();
        }
    }
}
