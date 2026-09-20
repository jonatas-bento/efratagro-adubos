namespace EfratAgro.Adubos.Api.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string TokenType);
