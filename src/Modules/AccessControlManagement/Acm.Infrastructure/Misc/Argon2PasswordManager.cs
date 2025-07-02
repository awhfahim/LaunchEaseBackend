using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Acm.Infrastructure.Misc;

public static class Argon2PasswordManager
{
    private static async Task<string> HashPasswordAsPhcFormat(string password, byte[] salt)
    {
        using var argon2Id = new Argon2id(Encoding.UTF8.GetBytes(password));

        argon2Id.DegreeOfParallelism = 1;
        argon2Id.Iterations = 2;
        argon2Id.MemorySize = 19456;
        argon2Id.Salt = salt;

        var hash = await argon2Id.GetBytesAsync(32);

        const string id = "argon2id";
        const int version = 19;
        var parameters = new Dictionary<string, object>
        {
            { "m", argon2Id.MemorySize },
            { "t", argon2Id.Iterations },
            { "p", argon2Id.DegreeOfParallelism },
        };

        return PhcFormatter.Serialize(id, version, parameters, argon2Id.Salt, hash);
    }

    public static Task<string> HashPasswordAsPhcFormat(string password)
    {
        return HashPasswordAsPhcFormat(password, RandomNumberGenerator.GetBytes(16));
    }

    public static async Task<bool> Verify(string password, string storedHash)
    {
        var deserialized = PhcFormatter.Deserialize(storedHash);
        var newHash = await HashPasswordAsPhcFormat(password, deserialized.salt);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(newHash));
    }
}
