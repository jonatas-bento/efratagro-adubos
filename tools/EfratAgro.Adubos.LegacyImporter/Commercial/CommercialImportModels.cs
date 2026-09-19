namespace EfratAgro.Adubos.LegacyImporter.Commercial;

public sealed record CommercialImportRow(
    string Sheet,
    int ExcelRow,
    string Product,
    string Document,
    string RawSaleDate,
    DateTime? SaleDate,
    string DateQuality,
    string Seller,
    string Address,
    string Customer,
    string CanonicalCustomer,
    decimal? Quantity,
    decimal? UnitPrice,
    string FinancialRaw,
    string TravaRaw,
    string DeliveryStatusRaw,
    string ObservationRaw,
    IReadOnlyList<string> Issues)
{
    public string SourceCell =>
        $"G{ExcelRow}";

    public decimal? LineTotal =>
        Quantity.HasValue &&
        UnitPrice.HasValue
            ? Quantity.Value *
              UnitPrice.Value
            : null;
}

public sealed record CommercialLegacyProductPlan(
    string Supplier,
    string Product);

public sealed record CommercialTransactionPlan(
    string TransactionKey,
    string DocumentNumber,
    DateTime TransactionDate,
    string CustomerName,
    string CanonicalCustomer,
    IReadOnlyList<CommercialImportRow> Items,
    IReadOnlyList<string> Issues)
{
    public bool IsSafe =>
        Issues.Count == 0;
}

public sealed record CommercialImportPlan(
    IReadOnlyList<CommercialImportRow> Rows,
    IReadOnlyList<CommercialImportRow> UnkeyedRows,
    IReadOnlyList<CommercialTransactionPlan> SafeTransactions,
    IReadOnlyList<CommercialTransactionPlan> ReviewTransactions,
    IReadOnlyList<CommercialLegacyProductPlan> LegacyProductsToCreate)
{
    public int SafeItemCount =>
        SafeTransactions.Sum(
            x => x.Items.Count);

    public int ReviewTransactionItemCount =>
        ReviewTransactions.Sum(
            x => x.Items.Count);

    public int ReviewRowCount =>
        ReviewTransactionItemCount +
        UnkeyedRows.Count;

    public int TransactionCount =>
        SafeTransactions.Count +
        ReviewTransactions.Count;

    public bool MatchesKnownBaseline =>
        Rows.Count == 788
        &&
        UnkeyedRows.Count == 4
        &&
        TransactionCount == 538
        &&
        SafeTransactions.Count == 522
        &&
        ReviewTransactions.Count == 16
        &&
        SafeItemCount == 744
        &&
        ReviewTransactionItemCount == 40
        &&
        ReviewRowCount == 44
        &&
        LegacyProductsToCreate.Count == 5;
}
