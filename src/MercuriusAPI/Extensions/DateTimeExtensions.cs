namespace Mercurius.LAN.API.Extensions
{
    public static class DateTimeExtensions
    {
        extension(DateTime dateTime)
        {
            public DateTime EnsureUtc()
            {
                return dateTime.Kind switch
                { 
                    DateTimeKind.Utc => dateTime,
                    DateTimeKind.Local => dateTime.ToUniversalTime(),

                    DateTimeKind.Unspecified => throw new InvalidOperationException(
                        "Received a DateTime without timezone information. Send UTC with 'Z' or use DateTimeOffset."),

                    _ => throw new ArgumentOutOfRangeException(nameof(dateTime))
                };
            }
        }    
    }
}
