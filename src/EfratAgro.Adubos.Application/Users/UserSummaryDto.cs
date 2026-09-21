namespace EfratAgro.Adubos.Application.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    int AccessFailedCount,
    DateTimeOffset? LockoutEnd);
