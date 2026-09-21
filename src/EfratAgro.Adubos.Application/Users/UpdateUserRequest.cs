namespace EfratAgro.Adubos.Application.Users;

public sealed record UpdateUserRequest(
    string DisplayName,
    string Email,
    string Role,
    bool IsActive);
