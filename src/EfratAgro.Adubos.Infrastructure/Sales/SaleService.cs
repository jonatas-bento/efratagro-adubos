using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Domain.Finance;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Infrastructure.Sales;

public sealed class SaleService
    : ISaleService
{
    private readonly AdubosDbContext _dbContext;

    public SaleService(
        AdubosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateSaleResult> CreateAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Informe o cliente.");
        }

        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "A venda precisa possuir ao menos um item.");
        }

        if (request.Receivables is null ||
            request.Receivables.Count == 0)
        {
            throw new ArgumentException(
                "Informe a programação financeira da venda.");
        }

        if (request.Items.Any(x => x.ProductId == Guid.Empty))
        {
            throw new ArgumentException(
                "Existe um produto inválido na venda.");
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
                        3) !=
                    x.Quantity))
        {
            throw new ArgumentException(
                "A quantidade deve ter no máximo três casas decimais.");
        }

        if (request.Items.Any(x => x.UnitPrice < 0))
        {
            throw new ArgumentException(
                "O preço não pode ser negativo.");
        }

        if (request.Items.Any(
                x =>
                    Math.Round(
                        x.UnitPrice,
                        2) !=
                    x.UnitPrice))
        {
            throw new ArgumentException(
                "O preço unitário deve ter no máximo duas casas decimais.");
        }

        if (request.Items
            .GroupBy(x => x.ProductId)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "O mesmo produto não pode aparecer duas vezes na venda.");
        }

        if (request.Receivables
            .GroupBy(x => x.InstallmentNumber)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "Existem parcelas duplicadas.");
        }

        if (request.Receivables.Any(
                x =>
                    x.InstallmentNumber <= 0 ||
                    x.Amount <= 0))
        {
            throw new ArgumentException(
                "A programação financeira contém uma parcela inválida.");
        }

        if (request.Receivables.Any(
                x =>
                    Math.Round(
                        x.Amount,
                        2) !=
                    x.Amount))
        {
            throw new ArgumentException(
                "As parcelas devem ter no máximo duas casas decimais.");
        }

        var totalValue =
            Math.Round(
                request.Items.Sum(
                    x =>
                        x.Quantity *
                        x.UnitPrice),
                2,
                MidpointRounding.AwayFromZero);

        if (totalValue <= 0)
        {
            throw new ArgumentException(
                "O valor total da venda precisa ser maior que zero.");
        }

        var scheduledAmount =
            request.Receivables.Sum(
                x => x.Amount);

        if (totalValue != scheduledAmount)
        {
            throw new ArgumentException(
                $"A programação financeira ({scheduledAmount:C}) " +
                $"não corresponde ao total da venda ({totalValue:C}).");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            var customer =
                await _dbContext.Customers
                    .SingleOrDefaultAsync(
                        x =>
                            x.Id == request.CustomerId &&
                            x.IsActive,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    "Cliente não encontrado.");

            var products =
                await _dbContext.Products
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToListAsync(
                        cancellationToken);

            var productsById =
                products.ToDictionary(
                    x => x.Id);

            foreach (var item in request.Items)
            {
                if (!productsById.ContainsKey(
                        item.ProductId))
                {
                    throw new InvalidOperationException(
                        "Produto não encontrado.");
                }
            }

            var balances =
                await _dbContext.InventoryMovements
                    .AsNoTracking()
                    .GroupBy(
                        x => x.ProductId)
                    .Select(group =>
                        new
                        {
                            ProductId = group.Key,
                            Quantity =
                                group.Sum(
                                    x => x.Quantity)
                        })
                    .ToListAsync(
                        cancellationToken);

            var stockByProduct =
                balances.ToDictionary(
                    x => x.ProductId,
                    x => x.Quantity);

            foreach (var item in request.Items)
            {
                var available =
                    stockByProduct
                        .GetValueOrDefault(
                            item.ProductId);

                if (available < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Estoque insuficiente para " +
                        $"'{productsById[item.ProductId].Name}'. " +
                        $"Disponível: {available}.");
                }
            }

            var warehouse =
                await _dbContext.Warehouses
                    .SingleAsync(
                        x =>
                            x.Name ==
                            "Armazém Principal",
                        cancellationToken);

            var occurredAtUtc =
                DateTime.UtcNow;

            var sale =
                new Sale(
                    customer.Id,
                    occurredAtUtc,
                    request.DeliveryMethod,
                    SaleOrigin.Operational);

            _dbContext.Sales.Add(
                sale);

            foreach (var requestedItem in request.Items)
            {
                var saleItem =
                    new SaleItem(
                        sale.Id,
                        requestedItem.ProductId,
                        requestedItem.Quantity,
                        requestedItem.UnitPrice);

                _dbContext.SaleItems.Add(
                    saleItem);

                var movement =
                    new InventoryMovement(
                        requestedItem.ProductId,
                        warehouse.Id,
                        InventoryMovementType.Sale,
                        StockBucket.Normal,
                        -requestedItem.Quantity,
                        occurredAtUtc,
                        referenceType: "SALE",
                        referenceId: sale.Id,
                        notes:
                            "Saída de estoque por venda.");

                _dbContext.InventoryMovements.Add(
                    movement);
            }

            foreach (
                var requestedReceivable
                in request.Receivables
                    .OrderBy(
                        x =>
                            x.InstallmentNumber))
            {
                var receivable =
                    new Receivable(
                        sale.Id,
                        requestedReceivable.InstallmentNumber,
                        requestedReceivable.DueDate,
                        requestedReceivable.Amount);

                _dbContext.Receivables.Add(
                    receivable);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new CreateSaleResult(
                sale.Id,
                customer.Id,
                customer.Name,
                request.Items.Count,
                request.Items.Sum(
                    x => x.Quantity),
                totalValue,
                occurredAtUtc,
                sale.DeliveryMethod,
                sale.DeliveryStatus,
                request.Receivables.Count,
                scheduledAmount);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}
