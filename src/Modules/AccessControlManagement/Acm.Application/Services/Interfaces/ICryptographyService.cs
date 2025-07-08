using System.Security.Claims;

namespace Acm.Application.Services.Interfaces;

public interface ICryptographyService
{
    Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims,
        CancellationToken cancellationToken = default);

    Task<List<Claim>> GetUserClaimsForTenantAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
}