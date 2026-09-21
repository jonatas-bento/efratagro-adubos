namespace EfratAgro.Adubos.Application.DataQuality;

public interface IDataQualityService
{
    Task<DataQualityDto> GetAsync(
        CancellationToken cancellationToken = default);

    Task<DataQualityDto>
        SetOperationalTrustedFromAsync(
            DateOnly? trustedFrom,
            CancellationToken cancellationToken = default);
}
