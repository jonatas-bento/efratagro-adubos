using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Customers;

namespace EfratAgro.Adubos.LegacyImporter.Commercial;

public sealed class CommercialImportPlanner
{
    public CommercialImportPlan Create(
        IReadOnlyList<CommercialImportRow> rows,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<Product> products,
        IReadOnlyList<Customer> customers)
    {
        var suppliersByCanonical =
            suppliers
                .GroupBy(
                    supplier =>
                        LegacyText.CanonicalName(
                            supplier.Name))
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray(),
                    StringComparer.Ordinal);

        var supplierCanonicalById =
            suppliers.ToDictionary(
                supplier => supplier.Id,
                supplier =>
                    LegacyText.CanonicalName(
                        supplier.Name));

        var existingProductKeys =
            products
                .Where(
                    product =>
                        supplierCanonicalById.ContainsKey(
                            product.SupplierId))
                .Select(
                    product =>
                        BuildProductKey(
                            supplierCanonicalById[
                                product.SupplierId],
                            product.Name))
                .ToHashSet(
                    StringComparer.Ordinal);

        var legacyProductsToCreate =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.Product))
                .Select(
                    row =>
                        new
                        {
                            Supplier =
                                row.Sheet,

                            OriginalProduct =
                                row.Product,

                            ResolvedProduct =
                                LegacyProductAliases
                                    .ResolveProductName(
                                        row.Sheet,
                                        row.Product)
                        })
                .GroupBy(
                    item =>
                        BuildProductKey(
                            LegacyText.CanonicalName(
                                item.Supplier),
                            item.ResolvedProduct),
                    StringComparer.Ordinal)
                .Where(
                    group =>
                        !existingProductKeys.Contains(
                            group.Key))
                .Select(
                    group =>
                    {
                        var first =
                            group.First();

                        return
                            new CommercialLegacyProductPlan(
                                first.Supplier,
                                first.OriginalProduct);
                    })
                .OrderBy(
                    product =>
                        product.Supplier)
                .ThenBy(
                    product =>
                        product.Product)
                .ToArray();

        var databaseCustomerGroups =
            customers
                .Where(
                    customer =>
                        !string.IsNullOrWhiteSpace(
                            customer.Name))
                .GroupBy(
                    customer =>
                        LegacyText.CanonicalName(
                            customer.Name),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    StringComparer.Ordinal);

        var unkeyed =
            rows
                .Where(
                    row =>
                        string.IsNullOrWhiteSpace(
                            row.Document)
                        ||
                        !row.SaleDate.HasValue)
                .ToArray();

        var transactionGroups =
            rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.Document)
                        &&
                        row.SaleDate.HasValue)
                .GroupBy(
                    row =>
                        BuildTransactionKey(
                            row.SaleDate!.Value,
                            row.Document),
                    StringComparer.Ordinal)
                .OrderBy(
                    group =>
                        group.Key);

        var safe =
            new List<CommercialTransactionPlan>();

        var review =
            new List<CommercialTransactionPlan>();

        foreach (var group in transactionGroups)
        {
            var items =
                group.ToArray();

            var issues =
                items
                    .SelectMany(
                        item =>
                            item.Issues)
                    .ToHashSet(
                        StringComparer.Ordinal);

            foreach (var item in items)
            {
                var supplierKey =
                    LegacyText.CanonicalName(
                        item.Sheet);

                if (
                    !suppliersByCanonical.TryGetValue(
                        supplierKey,
                        out var supplierMatches)
                    ||
                    supplierMatches.Length != 1)
                {
                    issues.Add(
                        "SUPPLIER_NOT_FOUND_OR_AMBIGUOUS");
                }
            }

            var canonicalCustomers =
                items
                    .Select(
                        item =>
                            item.CanonicalCustomer)
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Distinct(
                        StringComparer.Ordinal)
                    .ToArray();

            if (canonicalCustomers.Length == 0)
            {
                issues.Add(
                    "TRANSACTION_CUSTOMER_MISSING");
            }
            else if (canonicalCustomers.Length > 1)
            {
                issues.Add(
                    "TRANSACTION_CUSTOMER_CONFLICT");
            }
            else
            {
                var canonicalCustomer =
                    canonicalCustomers[0];

                if (
                    databaseCustomerGroups.TryGetValue(
                        canonicalCustomer,
                        out var databaseMatches)
                    &&
                    databaseMatches > 1)
                {
                    issues.Add(
                        "CUSTOMER_DB_AMBIGUOUS");
                }
            }

            var customerName =
                items
                    .Select(
                        item =>
                            item.Customer)
                    .FirstOrDefault(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                ??
                string.Empty;

            var canonicalName =
                canonicalCustomers.Length == 1
                    ? canonicalCustomers[0]
                    : string.Empty;

            var first =
                items[0];

            var transaction =
                new CommercialTransactionPlan(
                    group.Key,
                    first.Document.Trim(),
                    first.SaleDate!.Value.Date,
                    customerName,
                    canonicalName,
                    items,
                    issues
                        .OrderBy(x => x)
                        .ToArray());

            if (transaction.IsSafe)
            {
                safe.Add(
                    transaction);
            }
            else
            {
                review.Add(
                    transaction);
            }
        }

        return new CommercialImportPlan(
            rows,
            unkeyed,
            safe,
            review,
            legacyProductsToCreate);
    }

    public static string BuildTransactionKey(
        DateTime transactionDate,
        string document)
    {
        return
            $"{transactionDate:yyyy-MM-dd}|" +
            $"{LegacyText.NormalizeDocument(document)}";
    }

    private static string BuildProductKey(
        string canonicalSupplier,
        string product)
    {
        return
            $"{canonicalSupplier}|" +
            $"{LegacyText.NormalizeExact(product)}";
    }
}
