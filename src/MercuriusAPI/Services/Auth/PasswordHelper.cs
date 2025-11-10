using System.Security.Cryptography;

namespace MercuriusAPI.Services.Auth;

public static class PasswordHelper
{
    private const int _saltSize = 16; // 128 bit
    private const int _keySize = 32; // 256 bit
    private const int _iterations = 100_000;

    public static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var rng = RandomNumberGenerator.Create();
        salt = new byte[_saltSize];
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations, HashAlgorithmName.SHA256);
        hash = pbkdf2.GetBytes(_keySize);
    }

    public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _iterations, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(_keySize);
        return computedHash.SequenceEqual(hash);
    }
}
