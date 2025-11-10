using System.Text.RegularExpressions;

namespace MercuriusAPI.Services.Auth;

public static class ValidationHelper
{
    public static bool IsPasswordStrong(string password)
    {
        // At least 8 chars, 1 upper, 1 lower, 1 digit, 1 special
        return password.Length >= 8 &&
               Regex.IsMatch(password, @"[A-Z]") &&
               Regex.IsMatch(password, @"[a-z]") &&
               Regex.IsMatch(password, @"\d") &&
               Regex.IsMatch(password, @"[^a-zA-Z\d]");
    }

    public static bool IsUsernameValid(string username)
    {
        // Only allow alphanumeric usernames, 3-32 chars
        return Regex.IsMatch(username, @"^[a-zA-Z0-9]{3,32}$");
    }

    public static bool IsPasswordSame(string oldPassword, string newPassword)
    {
        // Check if passwords match
        return string.Equals(oldPassword, newPassword, StringComparison.Ordinal);
    }
}
