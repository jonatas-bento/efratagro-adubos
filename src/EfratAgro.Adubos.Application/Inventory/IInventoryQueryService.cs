namespace EfratAgro.Adubos.Application.Inventory;

public interface IInventoryQueryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(
        string? search,
        CancellationToken cancellationToken = default);
}
