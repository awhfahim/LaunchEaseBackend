namespace Acm.Application.Features.AccessControlFeatures.Interfaces;

public interface IAuthCryptographyService
{
    public Task<int> GetSecureTokenAsync();
    public Task<int> GetSecureTokenAsync(int lowerBound, int upperBound);
    public Task<string> HashPasswordAsync(string plainText);
    public Task<bool> VerifyPasswordAsync(string plainText, string hash);
}
