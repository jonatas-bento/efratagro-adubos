using EfratAgro.Adubos.Application.Purchases;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Purchases;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Purchases;

public sealed class PurchaseService
    : IPurchaseService
{
    private readonly AdubosDbContext _dbContext;

    public PurchaseService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreatePurchaseResult> CreateAsync(
        CreatePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SupplierId == Guid.Empty)
        {
            throw new ArgumentException(
                "Informe o fornecedor.");
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "A compra precisa possuir ao menos um item.");
        }

        if (request.Items.Any(x => x.ProductId == Guid.Empty))
        {
            throw new ArgumentException(
                "Existe um produto inválido na compra.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            throw new ArgumentException(
                "A quantidade precisa ser maior que zero.");
        }

        if (request.Items.Any(
                x =>
                    Math.Round(
                        x.Quantity,
                        3,
                        MidpointRounding.AwayFromZero) !=
                    x.Quantity))
        {
            throw new ArgumentException(
                "A quantidade pode possuir no máximo 3 casas decimais.");
        }

        if (request.Items.Any(x => x.UnitCost < 0))
        {
            throw new ArgumentException(
                "O custo não pode ser negativo.");
        }

        if (request.Items.Any(
                x =>
                    Math.Round(
                        x.UnitCost,
                        2,
                        MidpointRounding.AwayFromZero) !=
                    x.UnitCost))
        {
            throw new ArgumentException(
                "O custo pode possuir no máximo 2 casas decimais.");
        }

        if (request.Items
            .GroupBy(x => x.ProductId)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "O mesmo produto não pode aparecer duas vezes na compra.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

        try
        {
            var supplier =
                await _dbContext.Suppliers
                    .SingleOrDefaultAsync(
                        x =>
                            x.Id == request.SupplierId &&
                            x.IsActive,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "Fornecedor não encontrado.");

            var products =
                await _dbContext.Products
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToListAsync(cancellationToken);

            var productsById =
                products.ToDictionary(
                    x => x.Id);

            foreach (var requestedItem in request.Items)
            {
                if (!productsById.TryGetValue(
                        requestedItem.ProductId,
                        out var product))
                {
                    throw new InvalidOperationException(
                        $"Produto {requestedItem.ProductId} não encontrado.");
                }

                if (product.SupplierId != supplier.Id)
                {
                    throw new InvalidOperationException(
                        $"O produto '{product.Name}' não pertence " +
                        $"ao fornecedor '{supplier.Name}'.");
                }
            }

            var warehouse =
                await _dbContext.Warehouses
                    .SingleAsync(
                        x => x.Name == "Armazém Principal",
                        cancellationToken);

            var occurredAtUtc =
                DateTime.UtcNow;

            var purchase =
                new Purchase(
                    supplier.Id,
                    occurredAtUtc);

            _dbContext.Purchases.Add(
                purchase);

            foreach (var requestedItem in request.Items)
            {
                var purchaseItem =
                    new PurchaseItem(
                        purchase.Id,
                        requestedItem.ProductId,
                        requestedItem.Quantity,
                        requestedItem.UnitCost);

                _dbContext.PurchaseItems.Add(
                    purchaseItem);

                var movement =
                    new InventoryMovement(
                        requestedItem.ProductId,
                        warehouse.Id,
                        InventoryMovementType.Purchase,
                        StockBucket.Normal,
                        requestedItem.Quantity,
                        occurredAtUtc,
                        referenceType: "PURCHASE",
                        referenceId: purchase.Id,
                        notes: "Entrada de estoque por compra.");

                _dbContext.InventoryMovements.Add(
                    movement);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new CreatePurchaseResult(
                purchase.Id,
                supplier.Id,
                supplier.Name,
                request.Items.Count,
                request.Items.Sum(x => x.Quantity),
                request.Items.Sum(
                    x => x.Quantity * x.UnitCost),
                occurredAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}
