using System.Globalization;
using System.Text.Json;
using EfratAgro.Adubos.Domain.Legacy;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfratAgro.Adubos.LegacyImporter.Services;

public sealed class KnownCommercialReviewResolutionService
{
    private const string SourceFileHash =
        "6E43F5654910B9535C65B7F3E8EACC4B5D75F0436DC9178523ABEA61B807B23F";

    private readonly AdubosDbContext _dbContext;

    public KnownCommercialReviewResolutionService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KnownCommercialReviewResolutionResult> ExecuteAsync(
        bool persist,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        if (persist)
        {
            transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);
        }

        try
        {
            var batch =
                await _dbContext.LegacyImportBatches
                    .SingleAsync(
                        x =>
                            x.SourceFileHash == SourceFileHash
                            &&
                            x.Scope ==
                                LegacyImportScope.CommercialHistory,
                        cancellationToken);

            var reviewRowsBefore =
                await _dbContext.LegacyImportRows
                    .CountAsync(
                        x =>
                            x.BatchId == batch.Id
                            &&
                            x.RequiresReview,
                        cancellationToken);

            var candidates =
                new List<ResolutionCandidate>();

            var alreadyResolved = 0;

            foreach (var definition in GetDefinitions())
            {
                var row =
                    await _dbContext.LegacyImportRows
                        .SingleAsync(
                            x =>
                                x.BatchId == batch.Id
                                &&
                                x.SheetName ==
                                    definition.SheetName
                                &&
                                x.RowNumber ==
                                    definition.RowNumber,
                            cancellationToken);

                ValidateRawTarget(
                    row,
                    definition);

                var metadata =
                    await _dbContext.LegacySaleMetadataEntries
                        .AsNoTracking()
                        .SingleAsync(
                            x =>
                                x.ImportBatchId == batch.Id
                                &&
                                x.TransactionKey ==
                                    definition.TransactionKey,
                            cancellationToken);

                if (!Same(
                        metadata.DocumentNumber,
                        definition.DocumentNumber))
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "metadata document mismatch.");
                }

                if (metadata.TransactionDate.Date !=
                    definition.TransactionDate.Date)
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "metadata transaction date mismatch.");
                }

                var sale =
                    await _dbContext.Sales
                        .AsNoTracking()
                        .SingleAsync(
                            x =>
                                x.Id == metadata.SaleId,
                            cancellationToken);

                if (sale.Origin != SaleOrigin.Legacy)
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "target sale is not legacy.");
                }

                if (sale.DeliveryStatus !=
                    DeliveryStatus.NotTracked)
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "legacy delivery must be NotTracked.");
                }

                var customer =
                    await _dbContext.Customers
                        .AsNoTracking()
                        .SingleAsync(
                            x =>
                                x.Id == sale.CustomerId,
                            cancellationToken);

                if (!Same(
                        customer.Name,
                        definition.CustomerName))
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        $"customer mismatch. Expected " +
                        $"'{definition.CustomerName}', found " +
                        $"'{customer.Name}'.");
                }

                var supplier =
                    await _dbContext.Suppliers
                        .AsNoTracking()
                        .SingleAsync(
                            x =>
                                x.Name ==
                                    definition.SupplierName,
                            cancellationToken);

                var product =
                    await _dbContext.Products
                        .AsNoTracking()
                        .SingleAsync(
                            x =>
                                x.SupplierId == supplier.Id
                                &&
                                x.Name ==
                                    definition.ProductName,
                            cancellationToken);

                await ValidateEvidenceAsync(
                    batch.Id,
                    definition,
                    cancellationToken);

                if (!row.RequiresReview)
                {
                    ValidateAlreadyResolvedRow(
                        row,
                        definition);

                    var linkedItem =
                        await _dbContext.SaleItems
                            .AsNoTracking()
                            .SingleAsync(
                                x =>
                                    x.Id ==
                                        row.SaleItemId!.Value,
                                cancellationToken);

                    ValidateLinkedItem(
                        linkedItem,
                        sale.Id,
                        product.Id,
                        definition);

                    alreadyResolved++;
                    continue;
                }

                if (row.SaleItemId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "pending review row already has SaleItemId.");
                }

                if (!Same(
                        row.ReviewReason,
                        "SALE_DATE_INVALID"))
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        $"unexpected review reason " +
                        $"'{row.ReviewReason}'.");
                }

                var existingItems =
                    await _dbContext.SaleItems
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.SaleId == sale.Id
                                &&
                                x.ProductId == product.Id)
                        .ToListAsync(
                            cancellationToken);

                var duplicate =
                    existingItems.Any(
                        x =>
                            x.Quantity ==
                                definition.Quantity
                            &&
                            x.UnitPrice ==
                                definition.UnitPrice);

                if (duplicate)
                {
                    throw new InvalidOperationException(
                        $"{definition.TransactionKey}: " +
                        "matching SaleItem already exists " +
                        "without provenance linkage.");
                }

                candidates.Add(
                    new ResolutionCandidate(
                        row,
                        sale.Id,
                        product.Id,
                        definition));
            }

            var projectedReviewRowsAfter =
                reviewRowsBefore -
                candidates.Count;

            if (!persist)
            {
                return new(
                    Candidates:
                        candidates.Count,
                    AlreadyResolved:
                        alreadyResolved,
                    SaleItemsCreated:
                        0,
                    ReviewRowsBefore:
                        reviewRowsBefore,
                    ReviewRowsAfter:
                        projectedReviewRowsAfter);
            }

            foreach (var candidate in candidates)
            {
                var item =
                    new SaleItem(
                        candidate.SaleId,
                        candidate.ProductId,
                        candidate.Definition.Quantity,
                        candidate.Definition.UnitPrice);

                _dbContext.SaleItems.Add(
                    item);

                candidate.Row.ResolveWithSaleItem(
                    item.Id,
                    candidate.Definition.ResolutionNote);
            }

            EnsureOnlyAllowedWrites();

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            var actualReviewRowsAfter =
                await _dbContext.LegacyImportRows
                    .CountAsync(
                        x =>
                            x.BatchId == batch.Id
                            &&
                            x.RequiresReview,
                        cancellationToken);

            if (actualReviewRowsAfter !=
                projectedReviewRowsAfter)
            {
                throw new InvalidOperationException(
                    "Unexpected review-row count after resolution. " +
                    $"Expected {projectedReviewRowsAfter}, " +
                    $"found {actualReviewRowsAfter}.");
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            return new(
                Candidates:
                    candidates.Count,
                AlreadyResolved:
                    alreadyResolved,
                SaleItemsCreated:
                    candidates.Count,
                ReviewRowsBefore:
                    reviewRowsBefore,
                ReviewRowsAfter:
                    actualReviewRowsAfter);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task ValidateEvidenceAsync(
        Guid batchId,
        KnownResolutionDefinition definition,
        CancellationToken cancellationToken)
    {
        foreach (var evidence in definition.Evidence)
        {
            var row =
                await _dbContext.LegacyImportRows
                    .AsNoTracking()
                    .SingleAsync(
                        x =>
                            x.BatchId == batchId
                            &&
                            x.SheetName ==
                                evidence.SheetName
                            &&
                            x.RowNumber ==
                                evidence.RowNumber,
                        cancellationToken);

            if (row.RequiresReview)
            {
                throw new InvalidOperationException(
                    $"{definition.TransactionKey}: " +
                    $"evidence row {evidence.SheetName}#" +
                    $"{evidence.RowNumber} is still under review.");
            }

            if (!row.SaleItemId.HasValue)
            {
                throw new InvalidOperationException(
                    $"{definition.TransactionKey}: " +
                    $"evidence row {evidence.SheetName}#" +
                    $"{evidence.RowNumber} has no SaleItem linkage.");
            }

            using var json =
                JsonDocument.Parse(
                    row.RawData);

            var root =
                json.RootElement;

            ExpectText(
                root,
                "document",
                definition.DocumentNumber,
                definition.TransactionKey);

            ExpectText(
                root,
                "customer",
                definition.CustomerName,
                definition.TransactionKey);

            ExpectText(
                root,
                "seller",
                evidence.Seller,
                definition.TransactionKey);

            ExpectText(
                root,
                "address",
                evidence.Address,
                definition.TransactionKey);

            var date =
                ReadDate(
                    root,
                    "saleDate");

            if (!date.HasValue ||
                date.Value.Date !=
                    definition.TransactionDate.Date)
            {
                throw new InvalidOperationException(
                    $"{definition.TransactionKey}: " +
                    $"evidence row {evidence.SheetName}#" +
                    $"{evidence.RowNumber} does not confirm date.");
            }
        }
    }

    private static void ValidateRawTarget(
        LegacyImportRow row,
        KnownResolutionDefinition definition)
    {
        using var json =
            JsonDocument.Parse(
                row.RawData);

        var root =
            json.RootElement;

        ExpectText(
            root,
            "document",
            definition.DocumentNumber,
            definition.TransactionKey);

        ExpectText(
            root,
            "customer",
            definition.CustomerName,
            definition.TransactionKey);

        ExpectText(
            root,
            "product",
            definition.ProductName,
            definition.TransactionKey);

        ExpectDecimal(
            root,
            "quantity",
            definition.Quantity,
            definition.TransactionKey);

        ExpectDecimal(
            root,
            "unitPrice",
            definition.UnitPrice,
            definition.TransactionKey);
    }

    private static void ValidateAlreadyResolvedRow(
        LegacyImportRow row,
        KnownResolutionDefinition definition)
    {
        if (!row.SaleItemId.HasValue)
        {
            throw new InvalidOperationException(
                $"{definition.TransactionKey}: " +
                "resolved row has no SaleItemId.");
        }

        if (!row.ResolvedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                $"{definition.TransactionKey}: " +
                "resolved row has no ResolvedAtUtc.");
        }

        if (string.IsNullOrWhiteSpace(
                row.ResolutionNote))
        {
            throw new InvalidOperationException(
                $"{definition.TransactionKey}: " +
                "resolved row has no ResolutionNote.");
        }
    }

    private static void ValidateLinkedItem(
        SaleItem item,
        Guid saleId,
        Guid productId,
        KnownResolutionDefinition definition)
    {
        if (
            item.SaleId != saleId
            ||
            item.ProductId != productId
            ||
            item.Quantity != definition.Quantity
            ||
            item.UnitPrice != definition.UnitPrice)
        {
            throw new InvalidOperationException(
                $"{definition.TransactionKey}: " +
                "linked SaleItem does not match resolution.");
        }
    }

    private void EnsureOnlyAllowedWrites()
    {
        var forbidden =
            _dbContext.ChangeTracker
                .Entries()
                .Where(
                    x =>
                        x.State == EntityState.Added
                        ||
                        x.State == EntityState.Modified
                        ||
                        x.State == EntityState.Deleted)
                .Where(
                    x =>
                        x.Entity is not SaleItem
                        &&
                        x.Entity is not LegacyImportRow)
                .Select(
                    x =>
                        $"{x.Entity.GetType().Name}:{x.State}")
                .ToArray();

        if (forbidden.Length > 0)
        {
            throw new InvalidOperationException(
                "Forbidden writes detected: " +
                string.Join(
                    ", ",
                    forbidden));
        }
    }

    private static void ExpectText(
        JsonElement root,
        string property,
        string expected,
        string transactionKey)
    {
        var actual =
            ReadText(
                root,
                property);

        if (!Same(
                actual,
                expected))
        {
            throw new InvalidOperationException(
                $"{transactionKey}: raw field '{property}' " +
                $"mismatch. Expected '{expected}', " +
                $"found '{actual}'.");
        }
    }

    private static void ExpectDecimal(
        JsonElement root,
        string property,
        decimal expected,
        string transactionKey)
    {
        var actual =
            ReadDecimal(
                root,
                property);

        if (!actual.HasValue ||
            actual.Value != expected)
        {
            throw new InvalidOperationException(
                $"{transactionKey}: raw field '{property}' " +
                "mismatch.");
        }
    }

    private static string? ReadText(
        JsonElement root,
        string property)
    {
        if (!root.TryGetProperty(
                property,
                out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String =>
                value.GetString(),

            JsonValueKind.Number =>
                value.GetRawText(),

            JsonValueKind.Null =>
                null,

            _ =>
                value.ToString()
        };
    }

    private static decimal? ReadDecimal(
        JsonElement root,
        string property)
    {
        if (!root.TryGetProperty(
                property,
                out var value))
        {
            return null;
        }

        if (
            value.ValueKind == JsonValueKind.Number
            &&
            value.TryGetDecimal(
                out var numeric))
        {
            return numeric;
        }

        if (
            value.ValueKind == JsonValueKind.String
            &&
            decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadDate(
        JsonElement root,
        string property)
    {
        var value =
            ReadText(
                root,
                property);

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool Same(
        string? left,
        string? right)
    {
        return string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<KnownResolutionDefinition>
        GetDefinitions()
    {
        return
        [
            new KnownResolutionDefinition(
                SheetName:
                    "EQUILÍBRIO",
                RowNumber:
                    62,
                TransactionKey:
                    "2026-07-20|59878",
                DocumentNumber:
                    "59878",
                TransactionDate:
                    new DateTime(
                        2026,
                        7,
                        20),
                CustomerName:
                    "AGMAR ANDRADE",
                SupplierName:
                    "EQUILÍBRIO",
                ProductName:
                    "BORO 10% ULEXITA GR",
                Quantity:
                    1m,
                UnitPrice:
                    150m,
                ResolutionNote:
                    "Data recuperada como 2026-07-20 a partir " +
                    "da linha DIVERSOS#148: mesmo documento 59878, " +
                    "cliente AGMAR ANDRADE, vendedor GABRIEL e " +
                    "endereço ALTO CACHOEIRA.",
                Evidence:
                [
                    new KnownEvidenceDefinition(
                        "DIVERSOS",
                        148,
                        "GABRIEL",
                        "ALTO CACHOEIRA")
                ]),

            new KnownResolutionDefinition(
                SheetName:
                    "HERINGER",
                RowNumber:
                    341,
                TransactionKey:
                    "2026-09-02|7813",
                DocumentNumber:
                    "7813",
                TransactionDate:
                    new DateTime(
                        2026,
                        9,
                        2),
                CustomerName:
                    "ANDERSON DE FREITAS FARIA",
                SupplierName:
                    "HERINGER",
                ProductName:
                    "46-00-00",
                Quantity:
                    10m,
                UnitPrice:
                    176m,
                ResolutionNote:
                    "Data recuperada como 2026-09-02 a partir " +
                    "das linhas HERINGER#84 e DIVERSOS#111: " +
                    "mesmo documento 7813, cliente ANDERSON DE " +
                    "FREITAS FARIA, vendedora DAIANE e endereço " +
                    "BARRA GRANDE.",
                Evidence:
                [
                    new KnownEvidenceDefinition(
                        "HERINGER",
                        84,
                        "DAIANE",
                        "BARRA GRANDE"),

                    new KnownEvidenceDefinition(
                        "DIVERSOS",
                        111,
                        "DAIANE",
                        "BARRA GRANDE")
                ])
        ];
    }

    private sealed record ResolutionCandidate(
        LegacyImportRow Row,
        Guid SaleId,
        Guid ProductId,
        KnownResolutionDefinition Definition);
}

public sealed record KnownCommercialReviewResolutionResult(
    int Candidates,
    int AlreadyResolved,
    int SaleItemsCreated,
    int ReviewRowsBefore,
    int ReviewRowsAfter);

public sealed record KnownResolutionDefinition(
    string SheetName,
    int RowNumber,
    string TransactionKey,
    string DocumentNumber,
    DateTime TransactionDate,
    string CustomerName,
    string SupplierName,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    string ResolutionNote,
    IReadOnlyList<KnownEvidenceDefinition> Evidence);

public sealed record KnownEvidenceDefinition(
    string SheetName,
    int RowNumber,
    string Seller,
    string Address);
