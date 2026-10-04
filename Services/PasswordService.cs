using System.Security.Cryptography;

namespace AIClassroom.Services;

public class PasswordService
{
    private static readonly Lazy<string> _dummy = new(() => HashValue(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))));

    /// <summary>Valid-format hash of a random value, used to equalise timing when the account does not exist.</summary>
    public string DummyHash => _dummy.Value;

    private static string HashValue(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public string Hash(string value) => HashValue(value);

    public bool Verify(string value, string stored)
    {
        var parts = stored.Split('.', 2);
        if (parts.Length != 2) return false;
        byte[] salt, expected;
        try { salt = Convert.FromBase64String(parts[0]); expected = Convert.FromBase64String(parts[1]); }
        catch (FormatException) { return false; }
        var actual = Rfc2898DeriveBytes.Pbkdf2(value, salt, 120_000, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
