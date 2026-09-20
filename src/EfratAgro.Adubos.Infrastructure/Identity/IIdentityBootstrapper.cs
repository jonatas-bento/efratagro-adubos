namespace EfratAgro.Adubos.Infrastructure.Identity;

public interface IIdentityBootstrapper
{
    Task BootstrapAsync(
        string adminEmail,
        string adminPassword,
        CancellationToken cancellationToken = default);
}
