using EfratAgro.Adubos.Infrastructure.Identity;

namespace EfratAgro.Adubos.Api.Authentication;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(
        ApplicationUser user,
        IReadOnlyCollection<string> roles);
}
