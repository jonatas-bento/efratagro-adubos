namespace EfratAgro.Adubos.Api.Authentication;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string Name,
    IReadOnlyCollection<string> Roles);
