namespace EfratAgro.Adubos.LegacyImporter.Models;

public sealed record WarehouseImportRow(
    string Supplier,
    string Product,
    decimal Quantity,
    string? SafraRaw,
    string? PriceRaw,
    int ExcelRow);
