using EfratAgro.Adubos.Application.DataQuality;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Operations;
using EfratAgro.Adubos.Infrastructure.Common;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.DataQuality;

public sealed class DataQualityService
    : IDataQualityService
{
    private readonly AdubosDbContext _dbContext;

    public DataQualityService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataQualityDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var settings =
            await _dbContext
                .Set<OperationalDataSettings>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        OperationalDataSettings.SingletonId,
                    cancellationToken);

        return await BuildAsync(
            settings,
            cancellationToken);
    }

    public async Task<DataQualityDto>
        SetOperationalTrustedFromAsync(
            DateOnly? trustedFrom,
            CancellationToken cancellationToken = default)
    {
        if (trustedFrom.HasValue)
        {
            var inventoryHistoryAvailableFrom =
                await GetInventoryHistoryAvailableFromAsync(
                    cancellationToken);

            var today =
                DateOnly.FromDateTime(
                    BusinessDate.Today);

            DataQualityRules
                .ValidateOperationalTrustedFrom(
                    trustedFrom.Value,
                    inventoryHistoryAvailableFrom,
                    today);
        }

        var settings =
            await _dbContext
                .Set<OperationalDataSettings>()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        OperationalDataSettings.SingletonId,
                    cancellationToken);

        if (settings is null)
        {
            settings =
                OperationalDataSettings.Create(
                    DateTime.UtcNow);

            _dbContext
                .Set<OperationalDataSettings>()
                .Add(settings);
        }

        DateTime? trustedFromUtc =
            trustedFrom.HasValue
                ? BusinessDate.StartOfDayUtc(
                    trustedFrom.Value)
                : null;

        settings.SetOperationalTrustedFrom(
            trustedFromUtc,
            DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return await BuildAsync(
            settings,
            cancellationToken);
    }

    private async Task<DataQualityDto> BuildAsync(
        OperationalDataSettings? settings,
        CancellationToken cancellationToken)
    {
        var inventoryHistoryAvailableFrom =
            await GetInventoryHistoryAvailableFromAsync(
                cancellationToken);

        DateOnly? operationalTrustedFrom =
            settings?
                .OperationalTrustedFromUtc
                is DateTime trustedUtc
                    ? BusinessDate.ToLocalDate(
                        NormalizeUtc(
                            trustedUtc))
                    : null;

        DateTime? settingsUpdatedAtUtc =
            settings is null
                ? null
                : NormalizeUtc(
                    settings.UpdatedAtUtc);

        return new DataQualityDto(
            inventoryHistoryAvailableFrom,
            operationalTrustedFrom,
            settingsUpdatedAtUtc);
    }

    private async Task<DateOnly?>
        GetInventoryHistoryAvailableFromAsync(
            CancellationToken cancellationToken)
    {
        var inventoryBaselineUtc =
            await _dbContext
                .InventoryMovements
                .AsNoTracking()
                .Where(
                    x =>
                        x.Type ==
                        InventoryMovementType.OpeningBalance)
                .MinAsync(
                    x =>
                        (DateTime?)
                        x.OccurredAtUtc,
                    cancellationToken);

        return inventoryBaselineUtc.HasValue
            ? BusinessDate.ToLocalDate(
                NormalizeUtc(
                    inventoryBaselineUtc.Value))
            : null;
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
    }
}
