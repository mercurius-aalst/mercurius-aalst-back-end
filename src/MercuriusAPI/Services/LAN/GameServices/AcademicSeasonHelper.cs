namespace MercuriusAPI.Services.LAN.GameServices
{
    public static class AcademicSeasonHelper
    {
        public static string GetCurrent()
        {
            var now = DateTime.UtcNow;
            if(now.Month < 7)
                return $"{now.Year - 1}-{now.Year}";
            else
                return $"{now.Year}-{now.Year + 1}";
        }
    }
}
