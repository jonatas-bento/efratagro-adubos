namespace EfratAgro.Adubos.Application.Inventory;

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    string SupplierName,
    decimal Quantity);
