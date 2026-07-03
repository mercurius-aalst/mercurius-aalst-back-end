namespace Mercurius.LAN.API.Extensions;

public static class UsernameExtensions
{
    public static string NormalizeUsername(this string username)
    {
        return Mercurius.Modules.Identity.Services.UserProfileValidationHelper.NormalizeUsername(username);
    }
}
