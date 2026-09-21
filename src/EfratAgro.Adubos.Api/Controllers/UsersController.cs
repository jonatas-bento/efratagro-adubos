using EfratAgro.Adubos.Application.Users;
using EfratAgro.Adubos.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfratAgro.Adubos.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(
    Roles =
        ApplicationRoles.Admin)]
public sealed class UsersController
    : ControllerBase
{
    private readonly IUserManagementService
        _users;

    public UsersController(
        IUserManagementService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _users.GetAllAsync(
                cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _users.CreateAsync(
                    request,
                    cancellationToken);

            return Created(
                $"/api/users/{result.Id}",
                result);
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(
                await _users.UpdateAsync(
                    userId,
                    request,
                    cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _users.ResetPasswordAsync(
                userId,
                request,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    [HttpPost("{userId:guid}/unlock")]
    public async Task<IActionResult> Unlock(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _users.UnlockAsync(
                userId,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(
                new
                {
                    error = ex.Message
                });
        }
        catch (Exception ex)
            when (
                ex is ArgumentException or
                InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }
}
