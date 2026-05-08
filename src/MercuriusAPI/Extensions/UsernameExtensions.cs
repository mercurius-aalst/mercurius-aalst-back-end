namespace Mercurius.LAN.API.Extensions;

public static class UsernameExtensions
{
    public static string NormalizeUsername(this string username)
    {
        return Services.UserServices.UserProfileValidationHelper.NormalizeUsername(username);
    }
}
