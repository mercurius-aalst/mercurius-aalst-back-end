using System.Text.RegularExpressions;

namespace Mercurius.LAN.API.Services.UserServices;

public static class UserProfileValidationHelper
{
    public static bool IsUsernameValid(string username)
    {
        return Regex.IsMatch(username, "^[a-zA-Z0-9]{3,32}$");
    }
}
