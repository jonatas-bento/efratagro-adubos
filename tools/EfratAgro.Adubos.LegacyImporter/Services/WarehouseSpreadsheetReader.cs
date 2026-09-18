using ClosedXML.Excel;
using EfratAgro.Adubos.LegacyImporter.Models;

namespace EfratAgro.Adubos.LegacyImporter.Services;

public sealed class WarehouseSpreadsheetReader
{
    private static readonly SupplierBlock[] Blocks =
    [
        new("HERINGER", 2, 3, 4, 5),
        new("FERTIPAR", 7, 8, 9, 10),
        new("REAL", 12, 13, 14, 15),
        new("DIVERSOS", 17, 18, 19, 20),
        new("EQUILÍBRIO", 22, 23, 24, 25)
    ];

    public IReadOnlyList<WarehouseImportRow> Read(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Spreadsheet file was not found.",
                filePath);
        }

        using var workbook =
            new XLWorkbook(filePath);

        var worksheet =
            workbook.Worksheet("ARMAZÉM");

        var result =
            new List<WarehouseImportRow>();

        foreach (var block in Blocks)
        {
            ReadBlock(
                worksheet,
                block,
                result);
        }

        return result;
    }

    private static void ReadBlock(
        IXLWorksheet worksheet,
        SupplierBlock block,
        ICollection<WarehouseImportRow> result)
    {
        const int firstDataRow = 6;
        const int lastPossibleRow = 31;

        for (var rowNumber = firstDataRow;
             rowNumber <= lastPossibleRow;
             rowNumber++)
        {
            var productCell =
                worksheet.Cell(
                    rowNumber,
                    block.ProductColumn);

            var product =
                productCell
                    .GetString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(product))
            {
                continue;
            }

            if (product.Equals(
                    "Total",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var quantityCell =
                worksheet.Cell(
                    rowNumber,
                    block.QuantityColumn);

            decimal quantity = 0;

            if (!quantityCell.IsEmpty())
            {
                if (!quantityCell.TryGetValue<decimal>(
                        out quantity))
                {
                    throw new InvalidOperationException(
                        $"Invalid quantity at ARMAZÉM " +
                        $"row {rowNumber}, " +
                        $"column {block.QuantityColumn} " +
                        $"for product '{product}'.");
                }
            }

            var safraRaw =
                worksheet
                    .Cell(
                        rowNumber,
                        block.SafraColumn)
                    .GetFormattedString()
                    .Trim();

            var priceRaw =
                worksheet
                    .Cell(
                        rowNumber,
                        block.PriceColumn)
                    .GetFormattedString()
                    .Trim();

            var sourceCell =
                productCell.Address?.ToString();

            if (string.IsNullOrWhiteSpace(sourceCell))
            {
                throw new InvalidOperationException(
                    $"Unable to determine source cell " +
                    $"for ARMAZÉM row {rowNumber}.");
            }

            sourceCell =
                sourceCell.Replace(
                    "$",
                    string.Empty);

            result.Add(
                new WarehouseImportRow(
                    block.Supplier,
                    product,
                    quantity,
                    string.IsNullOrWhiteSpace(safraRaw)
                        ? null
                        : safraRaw,
                    string.IsNullOrWhiteSpace(priceRaw)
                        ? null
                        : priceRaw,
                    rowNumber,
                    sourceCell));
        }
    }

    private sealed record SupplierBlock(
        string Supplier,
        int ProductColumn,
        int QuantityColumn,
        int SafraColumn,
        int PriceColumn);
}
