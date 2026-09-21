namespace EfratAgro.Adubos.Application.Users;

public sealed record CreateUserRequest(
    string DisplayName,
    string Email,
    string Password,
    string Role);
