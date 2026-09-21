using EfratAgro.Adubos.Application.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Identity;

public sealed class UserManagementService
    : IUserManagementService
{
    private readonly UserManager<ApplicationUser>
        _userManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<
        IReadOnlyList<UserSummaryDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        var users =
            await _userManager.Users
                .OrderBy(
                    x => x.DisplayName)
                .ThenBy(
                    x => x.Email)
                .ToListAsync(
                    cancellationToken);

        var result =
            new List<UserSummaryDto>(
                users.Count);

        foreach (var user in users)
        {
            result.Add(
                await ToDtoAsync(
                    user));
        }

        return result;
    }

    public async Task<UserSummaryDto>
        CreateAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
    {
        var displayName =
            NormalizeDisplayName(
                request.DisplayName);

        var email =
            NormalizeEmail(
                request.Email);

        ValidateRole(
            request.Role);

        if (
            string.IsNullOrWhiteSpace(
                request.Password))
        {
            throw new ArgumentException(
                "Informe a senha inicial.");
        }

        var existing =
            await _userManager
                .FindByEmailAsync(
                    email);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                "Já existe um usuário com este email.");
        }

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                DisplayName = displayName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                IsActive = true,
                LockoutEnabled = true
            };

        var createResult =
            await _userManager
                .CreateAsync(
                    user,
                    request.Password);

        EnsureSuccess(
            createResult);

        var roleResult =
            await _userManager
                .AddToRoleAsync(
                    user,
                    request.Role);

        if (!roleResult.Succeeded)
        {
            await _userManager
                .DeleteAsync(
                    user);

            EnsureSuccess(
                roleResult);
        }

        return await ToDtoAsync(
            user);
    }

    public async Task<UserSummaryDto>
        UpdateAsync(
            Guid userId,
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Usuário inválido.");
        }

        var displayName =
            NormalizeDisplayName(
                request.DisplayName);

        var email =
            NormalizeEmail(
                request.Email);

        ValidateRole(
            request.Role);

        var user =
            await _userManager
                .FindByIdAsync(
                    userId.ToString())
            ?? throw new KeyNotFoundException(
                "Usuário não encontrado.");

        var duplicateEmail =
            await _userManager
                .FindByEmailAsync(
                    email);

        if (
            duplicateEmail is not null &&
            duplicateEmail.Id != user.Id)
        {
            throw new InvalidOperationException(
                "Já existe um usuário com este email.");
        }

        var currentRoles =
            await _userManager
                .GetRolesAsync(
                    user);

        var isCurrentAdmin =
            currentRoles.Contains(
                ApplicationRoles.Admin,
                StringComparer.Ordinal);

        var removesActiveAdmin =
            isCurrentAdmin &&
            user.IsActive &&
            (
                !request.IsActive ||
                !string.Equals(
                    request.Role,
                    ApplicationRoles.Admin,
                    StringComparison.Ordinal)
            );

        if (removesActiveAdmin)
        {
            await EnsureAnotherActiveAdminAsync(
                user.Id);
        }

        user.DisplayName =
            displayName;

        user.IsActive =
            request.IsActive;

        user.Email =
            email;

        user.UserName =
            email;

        user.EmailConfirmed =
            true;

        var updateResult =
            await _userManager
                .UpdateAsync(
                    user);

        EnsureSuccess(
            updateResult);

        if (
            !currentRoles.Contains(
                request.Role,
                StringComparer.Ordinal))
        {
            var addRoleResult =
                await _userManager
                    .AddToRoleAsync(
                        user,
                        request.Role);

            EnsureSuccess(
                addRoleResult);
        }

        var rolesToRemove =
            currentRoles
                .Where(
                    role =>
                        !string.Equals(
                            role,
                            request.Role,
                            StringComparison.Ordinal))
                .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult =
                await _userManager
                    .RemoveFromRolesAsync(
                        user,
                        rolesToRemove);

            EnsureSuccess(
                removeResult);
        }

        await _userManager
            .UpdateSecurityStampAsync(
                user);

        return await ToDtoAsync(
            user);
    }

    public async Task ResetPasswordAsync(
        Guid userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Usuário inválido.");
        }

        if (
            string.IsNullOrWhiteSpace(
                request.NewPassword))
        {
            throw new ArgumentException(
                "Informe a nova senha.");
        }

        var user =
            await _userManager
                .FindByIdAsync(
                    userId.ToString())
            ?? throw new KeyNotFoundException(
                "Usuário não encontrado.");

        foreach (
            var validator
            in _userManager.PasswordValidators)
        {
            var validation =
                await validator.ValidateAsync(
                    _userManager,
                    user,
                    request.NewPassword);

            EnsureSuccess(
                validation);
        }

        user.PasswordHash =
            _userManager.PasswordHasher
                .HashPassword(
                    user,
                    request.NewPassword);

        var updateResult =
            await _userManager
                .UpdateAsync(
                    user);

        EnsureSuccess(
            updateResult);

        var stampResult =
            await _userManager
                .UpdateSecurityStampAsync(
                    user);

        EnsureSuccess(
            stampResult);

        var failedCountResult =
            await _userManager
                .ResetAccessFailedCountAsync(
                    user);

        EnsureSuccess(
            failedCountResult);

        var unlockResult =
            await _userManager
                .SetLockoutEndDateAsync(
                    user,
                    null);

        EnsureSuccess(
            unlockResult);
    }

    public async Task UnlockAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Usuário inválido.");
        }

        var user =
            await _userManager
                .FindByIdAsync(
                    userId.ToString())
            ?? throw new KeyNotFoundException(
                "Usuário não encontrado.");

        var lockoutResult =
            await _userManager
                .SetLockoutEndDateAsync(
                    user,
                    null);

        EnsureSuccess(
            lockoutResult);

        var resetResult =
            await _userManager
                .ResetAccessFailedCountAsync(
                    user);

        EnsureSuccess(
            resetResult);
    }

    private async Task<UserSummaryDto>
        ToDtoAsync(
            ApplicationUser user)
    {
        var roles =
            await _userManager
                .GetRolesAsync(
                    user);

        var role =
            roles
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .FirstOrDefault()
            ?? string.Empty;

        var lockedOut =
            await _userManager
                .IsLockedOutAsync(
                    user);

        var displayName =
            string.IsNullOrWhiteSpace(
                user.DisplayName)
                ? (
                    user.Email
                    ?? user.UserName
                    ?? "Usuário"
                )
                : user.DisplayName;

        return new UserSummaryDto(
            user.Id,
            displayName,
            user.Email
                ?? user.UserName
                ?? string.Empty,
            role,
            user.IsActive,
            lockedOut,
            user.AccessFailedCount,
            user.LockoutEnd);
    }

    private async Task
        EnsureAnotherActiveAdminAsync(
            Guid excludedUserId)
    {
        var admins =
            await _userManager
                .GetUsersInRoleAsync(
                    ApplicationRoles.Admin);

        var otherActiveAdmins =
            admins.Count(
                x =>
                    x.Id != excludedUserId &&
                    x.IsActive);

        if (otherActiveAdmins == 0)
        {
            throw new InvalidOperationException(
                "O último administrador ativo não pode ser desativado ou rebaixado.");
        }
    }

    private static string NormalizeDisplayName(
        string displayName)
    {
        if (
            string.IsNullOrWhiteSpace(
                displayName))
        {
            throw new ArgumentException(
                "Informe o nome do usuário.");
        }

        var normalized =
            displayName.Trim();

        if (normalized.Length > 120)
        {
            throw new ArgumentException(
                "O nome deve ter no máximo 120 caracteres.");
        }

        return normalized;
    }

    private static string NormalizeEmail(
        string email)
    {
        if (
            string.IsNullOrWhiteSpace(
                email))
        {
            throw new ArgumentException(
                "Informe o email do usuário.");
        }

        return email
            .Trim()
            .ToLowerInvariant();
    }

    private static void ValidateRole(
        string role)
    {
        if (
            !ApplicationRoles.All.Contains(
                role,
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Perfil de acesso inválido.");
        }
    }

    private static void EnsureSuccess(
        IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message =
            string.Join(
                " ",
                result.Errors
                    .Select(
                        error =>
                            error.Description));

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(
                message)
                ? "Não foi possível concluir a operação do usuário."
                : message);
    }
}
