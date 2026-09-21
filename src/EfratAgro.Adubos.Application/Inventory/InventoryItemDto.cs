namespace EfratAgro.Adubos.Application.Inventory;

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    string SupplierName,
    bool IsActive,
    decimal Quantity)
{
    public decimal ReservedQuantity { get; init; }

    public decimal AvailableQuantity { get; init; }
}
