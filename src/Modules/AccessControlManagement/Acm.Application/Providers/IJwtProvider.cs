using System.Security.Claims;

namespace Acm.Application.Providers;

public interface IJwtProvider
{
    (string token, DateTime duration) GenerateJwt(IReadOnlyCollection<Claim> claims,
        TimeSpan tokenDuration, string secret);
}