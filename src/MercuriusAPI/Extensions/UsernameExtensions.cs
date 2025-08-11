using System.Text.RegularExpressions;

namespace MercuriusAPI.Extensions
{
    public static class UsernameExtensions
    {
        public static string Normalize(this string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));

            // Convert to lowercase and trim whitespace
            username = username.ToLowerInvariant().Trim();

            // Optionally, remove unwanted characters (e.g., special characters)
            // Uncomment the following line if needed:
            // username = Regex.Replace(username, "[^a-z0-9]", "");

            return username;
        }
    }
}