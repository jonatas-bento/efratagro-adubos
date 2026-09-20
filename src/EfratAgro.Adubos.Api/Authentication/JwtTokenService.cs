using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EfratAgro.Adubos.Api.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options)
    : IJwtTokenService
{
    private readonly JwtOptions _options =
        options.Value;

    public JwtTokenResult CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        if (user.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Cannot create an access token for a user without an id.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException(
                "Cannot create an access token for a user without an email.");
        }

        var userName =
            string.IsNullOrWhiteSpace(user.UserName)
                ? user.Email
                : user.UserName;

        var signingKey =
            JwtSecurity.CreateSigningKey(
                _options);

        var now =
            DateTime.UtcNow;

        var expiresAtUtc =
            now.AddMinutes(
                _options.AccessTokenMinutes);

        var claims =
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new(
                    JwtRegisteredClaimNames.Email,
                    user.Email),

                new(
                    JwtRegisteredClaimNames.UniqueName,
                    userName),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

        claims.AddRange(
            roles
                .Where(role =>
                    !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.Ordinal)
                .Select(role =>
                    new Claim(
                        JwtClaimNames.Role,
                        role)));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAtUtc,
                signingCredentials: credentials);

        var serializedToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new JwtTokenResult(
            serializedToken,
            expiresAtUtc);
    }
}
