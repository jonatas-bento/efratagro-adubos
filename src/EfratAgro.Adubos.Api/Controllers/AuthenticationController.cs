using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EfratAgro.Adubos.Api.Authentication;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController
    : ControllerBase
{
    private const string InvalidCredentialsMessage =
        "Credenciais inválidas.";

    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly IJwtTokenService
        _tokenService;

    public AuthenticationController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        if (
            string.IsNullOrWhiteSpace(
                request.Email) ||
            string.IsNullOrWhiteSpace(
                request.Password))
        {
            return BadRequest(
                new AuthErrorResponse(
                    "Email e senha são obrigatórios."));
        }

        var email =
            request.Email.Trim();

        var user =
            await _userManager
                .FindByEmailAsync(
                    email);

        if (user is null)
        {
            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return InvalidCredentials();
        }

        if (
            await _userManager
                .IsLockedOutAsync(
                    user))
        {
            return InvalidCredentials();
        }

        var passwordValid =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    request.Password);

        if (!passwordValid)
        {
            await _userManager
                .AccessFailedAsync(
                    user);

            return InvalidCredentials();
        }

        if (
            await _userManager
                .GetAccessFailedCountAsync(
                    user) > 0)
        {
            await _userManager
                .ResetAccessFailedCountAsync(
                    user);
        }

        var roles =
            await _userManager
                .GetRolesAsync(
                    user);

        var token =
            _tokenService.CreateToken(
                user,
                roles.ToArray());

        return Ok(
            new LoginResponse(
                token.AccessToken,
                token.ExpiresAtUtc,
                "Bearer"));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var subject =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var email =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Email);

        var name =
            User.FindFirstValue(
                JwtRegisteredClaimNames.UniqueName);

        if (
            !Guid.TryParse(
                subject,
                out var userId) ||
            string.IsNullOrWhiteSpace(
                email) ||
            string.IsNullOrWhiteSpace(
                name))
        {
            return Unauthorized();
        }

        var roles =
            User
                .FindAll(
                    JwtClaimNames.Role)
                .Select(
                    claim =>
                        claim.Value)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(
                    role =>
                        role)
                .ToArray();

        return Ok(
            new CurrentUserResponse(
                userId,
                email,
                name,
                roles));
    }

    private IActionResult InvalidCredentials()
    {
        return Unauthorized(
            new AuthErrorResponse(
                InvalidCredentialsMessage));
    }
}
