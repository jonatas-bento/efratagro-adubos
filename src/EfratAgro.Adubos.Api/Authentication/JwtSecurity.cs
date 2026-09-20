using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EfratAgro.Adubos.Api.Authentication;

internal static class JwtSecurity
{
    public static SymmetricSecurityKey CreateSigningKey(
        JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer was not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience was not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException(
                "JWT signing key was not configured.");
        }

        var keyBytes =
            Encoding.UTF8.GetBytes(
                options.SigningKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 256 bits.");
        }

        if (options.AccessTokenMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be between 1 and 60 minutes.");
        }

        return new SymmetricSecurityKey(
            keyBytes);
    }
}
