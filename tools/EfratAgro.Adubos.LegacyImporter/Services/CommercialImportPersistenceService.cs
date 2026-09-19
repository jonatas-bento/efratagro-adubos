using System.Text.Json;
using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Customers;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Legacy;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.LegacyImporter.Commercial;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.LegacyImporter.Services;

public sealed class CommercialImportPersistenceService
{
    private readonly AdubosDbContext _dbContext;

    public CommercialImportPersistenceService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommercialImportResult> ImportAsync(
        string filePath,
        string fileHash,
        CommercialImportPlan plan,
        CancellationToken cancellationToken = default)
    {
        var alreadyImported =
            await _dbContext.LegacyImportBatches
                .AnyAsync(
                    batch =>
                        batch.SourceFileHash ==
                            fileHash
                        &&
                        batch.Scope ==
                            LegacyImportScope.CommercialHistory,
                    cancellationToken);

        if (alreadyImported)
        {
            throw new InvalidOperationException(
                "Commercial history from this spreadsheet has already been imported.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var batch =
                new LegacyImportBatch(
                    Path.GetFileName(
                        filePath),
                    fileHash,
                    LegacyImportScope.CommercialHistory);

            _dbContext.LegacyImportBatches.Add(
                batch);

            var suppliers =
                await _dbContext.Suppliers
                    .ToListAsync(
                        cancellationToken);

            var supplierByCanonical =
                suppliers.ToDictionary(
                    supplier =>
                        LegacyText.CanonicalName(
                            supplier.Name),
                    supplier => supplier,
                    StringComparer.Ordinal);

            var products =
                await _dbContext.Products
                    .ToListAsync(
                        cancellationToken);

            var supplierCanonicalById =
                suppliers.ToDictionary(
                    supplier => supplier.Id,
                    supplier =>
                        LegacyText.CanonicalName(
                            supplier.Name));

            var productIndex =
                products.ToDictionary(
                    product =>
                        BuildProductKey(
                            supplierCanonicalById[
                                product.SupplierId],
                            product.Name),
                    product => product,
                    StringComparer.Ordinal);

            var createdProducts =
                0;

            foreach (
                var legacyProduct
                in plan.LegacyProductsToCreate)
            {
                var supplierKey =
                    LegacyText.CanonicalName(
                        legacyProduct.Supplier);

                if (
                    !supplierByCanonical.TryGetValue(
                        supplierKey,
                        out var supplier))
                {
                    throw new InvalidOperationException(
                        $"Supplier '{legacyProduct.Supplier}' was not found.");
                }

                var productKey =
                    BuildProductKey(
                        supplierKey,
                        legacyProduct.Product);

                if (productIndex.ContainsKey(
                        productKey))
                {
                    continue;
                }

                var product =
                    new Product(
                        legacyProduct.Product,
                        supplier.Id,
                        legacyProduct.Product);

                product.Deactivate();

                _dbContext.Products.Add(
                    product);

                productIndex.Add(
                    productKey,
                    product);

                createdProducts++;
            }

            var existingCustomers =
                await _dbContext.Customers
                    .ToListAsync(
                        cancellationToken);

            var customerGroups =
                existingCustomers
                    .GroupBy(
                        customer =>
                            LegacyText.CanonicalName(
                                customer.Name),
                        StringComparer.Ordinal)
                    .ToArray();

            var customerIndex =
                customerGroups
                    .Where(
                        group =>
                            group.Count() == 1)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Single(),
                        StringComparer.Ordinal);

            var customersCreated =
                0;

            var reviewReasons =
                BuildReviewReasonIndex(
                    plan);

            var legacyRows =
                new Dictionary<string, LegacyImportRow>(
                    StringComparer.Ordinal);

            foreach (var row in plan.Rows)
            {
                var supplierKey =
                    LegacyText.CanonicalName(
                        row.Sheet);

                if (
                    !supplierByCanonical.TryGetValue(
                        supplierKey,
                        out var supplier))
                {
                    throw new InvalidOperationException(
                        $"Supplier '{row.Sheet}' was not found.");
                }

                var resolvedProductName =
                    LegacyProductAliases.ResolveProductName(
                        row.Sheet,
                        row.Product);

                var productKey =
                    BuildProductKey(
                        supplierKey,
                        resolvedProductName);

                productIndex.TryGetValue(
                    productKey,
                    out var product);

                var transactionKey =
                    row.SaleDate.HasValue
                    &&
                    !string.IsNullOrWhiteSpace(
                        row.Document)
                        ? CommercialImportPlanner
                            .BuildTransactionKey(
                                row.SaleDate.Value,
                                row.Document)
                        : null;

                var rawData =
                    JsonSerializer.Serialize(
                        new
                        {
                            sheet =
                                row.Sheet,

                            row =
                                row.ExcelRow,

                            sourceCell =
                                row.SourceCell,

                            supplier =
                                supplier.Name,

                            product =
                                row.Product,

                            productId =
                                product?.Id,

                            document =
                                row.Document,

                            saleDateRaw =
                                row.RawSaleDate,

                            saleDate =
                                row.SaleDate,

                            dateQuality =
                                row.DateQuality,

                            seller =
                                row.Seller,

                            address =
                                row.Address,

                            customer =
                                row.Customer,

                            canonicalCustomer =
                                row.CanonicalCustomer,

                            quantity =
                                row.Quantity,

                            unitPrice =
                                row.UnitPrice,

                            lineTotal =
                                row.LineTotal,

                            financialRaw =
                                row.FinancialRaw,

                            travaRaw =
                                row.TravaRaw,

                            deliveryStatusRaw =
                                row.DeliveryStatusRaw,

                            observationRaw =
                                row.ObservationRaw,

                            transactionKey
                        });

                var legacyRow =
                    new LegacyImportRow(
                        batch.Id,
                        row.Sheet,
                        row.ExcelRow,
                        row.SourceCell,
                        rawData);

                var rowKey =
                    BuildRowKey(
                        row);

                if (
                    reviewReasons.TryGetValue(
                        rowKey,
                        out var reviewReason))
                {
                    legacyRow.MarkForReview(
                        reviewReason);
                }

                _dbContext.LegacyImportRows.Add(
                    legacyRow);

                legacyRows.Add(
                    rowKey,
                    legacyRow);
            }

            var salesCreated =
                0;

            var saleItemsCreated =
                0;

            foreach (
                var transactionPlan
                in plan.SafeTransactions)
            {
                if (
                    !customerIndex.TryGetValue(
                        transactionPlan.CanonicalCustomer,
                        out var customer))
                {
                    customer =
                        new Customer(
                            transactionPlan.CustomerName,
                            phone: null);

                    _dbContext.Customers.Add(
                        customer);

                    customerIndex.Add(
                        transactionPlan.CanonicalCustomer,
                        customer);

                    customersCreated++;
                }

                var occurredAtUtc =
                    ToHistoricalUtc(
                        transactionPlan.TransactionDate);

                var sale =
                    Sale.CreateLegacy(
                        customer.Id,
                        occurredAtUtc);

                _dbContext.Sales.Add(
                    sale);

                foreach (
                    var itemPlan
                    in transactionPlan.Items)
                {
                    if (
                        !itemPlan.Quantity.HasValue
                        ||
                        !itemPlan.UnitPrice.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Safe transaction '{transactionPlan.TransactionKey}' contains an invalid item.");
                    }

                    var supplierKey =
                        LegacyText.CanonicalName(
                            itemPlan.Sheet);

                    var resolvedProductName =
                        LegacyProductAliases.ResolveProductName(
                            itemPlan.Sheet,
                            itemPlan.Product);

                    var productKey =
                        BuildProductKey(
                            supplierKey,
                            resolvedProductName);

                    if (
                        !productIndex.TryGetValue(
                            productKey,
                            out var product))
                    {
                        throw new InvalidOperationException(
                            $"Product '{itemPlan.Sheet} :: {itemPlan.Product}' was not resolved.");
                    }

                    var saleItem =
                        new SaleItem(
                            sale.Id,
                            product.Id,
                            itemPlan.Quantity.Value,
                            itemPlan.UnitPrice.Value);

                    _dbContext.SaleItems.Add(
                        saleItem);

                    legacyRows[
                            BuildRowKey(
                                itemPlan)]
                        .LinkSaleItem(
                            saleItem.Id);

                    saleItemsCreated++;
                }

                var metadata =
                    new LegacySaleMetadata(
                        sale.Id,
                        batch.Id,
                        transactionPlan.TransactionKey,
                        transactionPlan.DocumentNumber,
                        transactionPlan.TransactionDate);

                _dbContext.LegacySaleMetadataEntries.Add(
                    metadata);

                salesCreated++;
            }

            batch.Complete(
                plan.Rows.Count,
                plan.ReviewRowCount);

            EnsureNoForbiddenWrites();

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new CommercialImportResult(
                batch.Id,
                fileHash,
                plan.Rows.Count,
                plan.SafeTransactions.Count,
                plan.SafeItemCount,
                plan.ReviewTransactions.Count,
                plan.ReviewRowCount,
                customersCreated,
                createdProducts,
                salesCreated,
                saleItemsCreated);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private void EnsureNoForbiddenWrites()
    {
        if (
            _dbContext.ChangeTracker
                .Entries<InventoryMovement>()
                .Any(
                    entry =>
                        entry.State ==
                        EntityState.Added))
        {
            throw new InvalidOperationException(
                "Commercial legacy import attempted to create inventory movements.");
        }

        if (
            _dbContext.ChangeTracker
                .Entries<Receivable>()
                .Any(
                    entry =>
                        entry.State ==
                        EntityState.Added))
        {
            throw new InvalidOperationException(
                "Commercial legacy import attempted to create receivables.");
        }

        if (
            _dbContext.ChangeTracker
                .Entries<Payment>()
                .Any(
                    entry =>
                        entry.State ==
                        EntityState.Added))
        {
            throw new InvalidOperationException(
                "Commercial legacy import attempted to create payments.");
        }
    }

    private static Dictionary<string, string>
        BuildReviewReasonIndex(
            CommercialImportPlan plan)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (
            var transaction
            in plan.ReviewTransactions)
        {
            var reason =
                string.Join(
                    "|",
                    transaction.Issues);

            foreach (
                var row
                in transaction.Items)
            {
                result[
                    BuildRowKey(row)] =
                    reason;
            }
        }

        foreach (
            var row
            in plan.UnkeyedRows)
        {
            var issues =
                row.Issues.Count > 0
                    ? row.Issues
                    : ["UNKEYED_ROW"];

            result[
                BuildRowKey(row)] =
                string.Join(
                    "|",
                    issues);
        }

        return result;
    }

    private static DateTime ToHistoricalUtc(
        DateTime transactionDate)
    {
        var localMidnight =
            DateTime.SpecifyKind(
                transactionDate.Date,
                DateTimeKind.Unspecified);

        var timezone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "America/Sao_Paulo");

        return TimeZoneInfo.ConvertTimeToUtc(
            localMidnight,
            timezone);
    }

    private static string BuildRowKey(
        CommercialImportRow row)
    {
        return
            $"{row.Sheet}|" +
            $"{row.ExcelRow}";
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

public sealed record CommercialImportResult(
    Guid BatchId,
    string FileHash,
    int LegacyRows,
    int SafeTransactions,
    int SafeItems,
    int ReviewTransactions,
    int ReviewRows,
    int CustomersCreated,
    int LegacyProductsCreated,
    int SalesCreated,
    int SaleItemsCreated);
