namespace EfratAgro.Adubos.Application.Finance;

public interface IReceivableQueryService
{
    Task<ReceivablesResult> GetAsync(
        bool openOnly,
        int take,
        CancellationToken cancellationToken = default);
}
