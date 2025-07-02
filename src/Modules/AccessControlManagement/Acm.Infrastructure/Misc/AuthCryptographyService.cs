using System.Security.Cryptography;
using Acm.Application.Features.AccessControlFeatures.Interfaces;

namespace Acm.Infrastructure.Misc;

public class AuthCryptographyService : IAuthCryptographyService
{
    public Task<int> GetSecureTokenAsync()
    {
        return Task.Run(() => RandomNumberGenerator.GetInt32(100000, 1000000));
    }

    public Task<int> GetSecureTokenAsync(int lowerBound, int upperBound)
    {
        upperBound += 1;
        return Task.Run(() => RandomNumberGenerator.GetInt32(lowerBound, upperBound));
    }

    public Task<string> HashPasswordAsync(string plainText)
    {
        return Argon2PasswordManager.HashPasswordAsPhcFormat(plainText);
    }

    public Task<bool> VerifyPasswordAsync(string plainText, string hash)
    {
        return Argon2PasswordManager.Verify(plainText, hash);
    }
}
