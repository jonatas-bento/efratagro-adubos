namespace EfratAgro.Adubos.Infrastructure.Identity;

public static class ApplicationRoles
{
    public const string Admin = "Admin";

    public const string Manager = "Manager";

    public const string Seller = "Seller";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        Manager,
        Seller
    ];
}