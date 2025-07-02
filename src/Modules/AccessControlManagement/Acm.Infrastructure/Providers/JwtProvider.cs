using System.Security.Claims;
using System.Text;
using Acm.Application.Providers;
using Common.Application.Providers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Acm.Infrastructure.Providers;

public class JwtProvider : IJwtProvider
{
    private readonly IDateTimeProvider _dateTimeProvider;
    public JwtProvider(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public (string token, DateTime duration) GenerateJwt(IReadOnlyCollection<Claim> claims,
        TimeSpan tokenDuration, string secret)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var finalTokenDuration = _dateTimeProvider.CurrentUtcTime.Add(tokenDuration);

        var descriptor = new SecurityTokenDescriptor
        {
            IssuedAt = _dateTimeProvider.CurrentUtcTime,
            Expires = finalTokenDuration,
            SigningCredentials = credentials
        };

        if (claims.Count != 0)
        {
            descriptor.Subject = new ClaimsIdentity(claims);
        }

        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return (handler.CreateToken(descriptor), finalTokenDuration);
    }
}