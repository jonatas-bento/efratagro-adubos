using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace EfratAgro.Adubos.Api.Authentication;

public static class AuthenticationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEfratAgroAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup("/api/auth")
                .WithTags("Authentication");

        group
            .MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group
            .MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(
                new AuthErrorResponse(
                    "Email e senha são obrigatórios."));
        }

        var user =
            await userManager.FindByEmailAsync(
                request.Email.Trim());

        if (user is null)
        {
            return InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentials();
        }

        var passwordIsValid =
            await userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!passwordIsValid)
        {
            await userManager.AccessFailedAsync(user);

            return InvalidCredentials();
        }

        if (await userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles =
            await userManager.GetRolesAsync(user);

        var token =
            tokenService.CreateToken(
                user,
                roles.ToArray());

        return Results.Ok(
            new LoginResponse(
                token.AccessToken,
                token.ExpiresAtUtc,
                "Bearer"));
    }

    private static IResult GetCurrentUser(
        ClaimsPrincipal principal)
    {
        var subject =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var email =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Email);

        var name =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.UniqueName);

        if (!Guid.TryParse(subject, out var userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(name))
        {
            return Results.Unauthorized();
        }

        var roles =
            principal
                .FindAll(JwtClaimNames.Role)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(role => role)
                .ToArray();

        return Results.Ok(
            new CurrentUserResponse(
                userId,
                email,
                name,
                roles));
    }

    private static IResult InvalidCredentials()
        => Results.Json(
            new AuthErrorResponse(
                "Credenciais inválidas."),
            statusCode:
                StatusCodes.Status401Unauthorized);
}
