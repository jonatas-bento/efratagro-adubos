namespace EfratAgro.Adubos.Api.Authentication;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
