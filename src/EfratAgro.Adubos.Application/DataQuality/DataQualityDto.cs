namespace EfratAgro.Adubos.Application.DataQuality;

public sealed record DataQualityDto(
    DateOnly? InventoryHistoryAvailableFrom,
    DateOnly? OperationalTrustedFrom,
    DateTime? SettingsUpdatedAtUtc);
