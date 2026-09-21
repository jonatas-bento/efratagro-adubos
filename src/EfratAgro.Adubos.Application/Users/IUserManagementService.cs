namespace EfratAgro.Adubos.Application.Users;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserSummaryDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<UserSummaryDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        Guid userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task UnlockAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
