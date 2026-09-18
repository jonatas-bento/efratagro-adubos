using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Domain.Customers;
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
        if (string.IsNullOrWhiteSpace(request.CustomerName))
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

        if (request.Items.Any(x => x.UnitPrice < 0))
        {
            throw new ArgumentException(
                "O preço não pode ser negativo.");
        }

        if (request.Items
            .GroupBy(x => x.ProductId)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "O mesmo produto não pode aparecer duas vezes na venda.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

        try
        {
            var normalizedCustomer =
                request.CustomerName
                    .Trim()
                    .ToUpperInvariant();

            var customer =
                await _dbContext.Customers
                    .FirstOrDefaultAsync(
                        x => x.NormalizedName == normalizedCustomer,
                        cancellationToken);

            if (customer is null)
            {
                customer =
                    new Customer(
                        request.CustomerName,
                        request.CustomerPhone);

                _dbContext.Customers.Add(customer);
            }

            var products =
                await _dbContext.Products
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToListAsync(cancellationToken);

            var productsById =
                products.ToDictionary(
                    x => x.Id);

            var stockRows =
                await _dbContext.InventoryMovements
                    .AsNoTracking()
                    .GroupBy(x => x.ProductId)
                    .Select(group => new
                    {
                        ProductId = group.Key,
                        Quantity = group.Sum(x => x.Quantity)
                    })
                    .ToListAsync(cancellationToken);

            var stockByProduct =
                stockRows.ToDictionary(
                    x => x.ProductId,
                    x => x.Quantity);

            foreach (var requestedItem in request.Items)
            {
                if (!productsById.TryGetValue(
                        requestedItem.ProductId,
                        out var product))
                {
                    throw new InvalidOperationException(
                        $"Produto {requestedItem.ProductId} não encontrado.");
                }

                var available =
                    stockByProduct.GetValueOrDefault(
                        product.Id,
                        0m);

                if (requestedItem.Quantity > available)
                {
                    throw new InvalidOperationException(
                        $"Estoque insuficiente para '{product.Name}'. " +
                        $"Disponível: {available:N3}. " +
                        $"Solicitado: {requestedItem.Quantity:N3}.");
                }
            }

            var warehouse =
                await _dbContext.Warehouses
                    .SingleAsync(
                        x => x.Name == "Armazém Principal",
                        cancellationToken);

            var occurredAtUtc =
                DateTime.UtcNow;

            var sale =
                new Sale(
                    customer.Id,
                    occurredAtUtc,
                    request.DeliveryMethod);

            _dbContext.Sales.Add(sale);

            foreach (var requestedItem in request.Items)
            {
                var saleItem =
                    new SaleItem(
                        sale.Id,
                        requestedItem.ProductId,
                        requestedItem.Quantity,
                        requestedItem.UnitPrice);

                _dbContext.SaleItems.Add(saleItem);

                var movement =
                    new InventoryMovement(
                        requestedItem.ProductId,
                        warehouse.Id,
                        InventoryMovementType.Sale,
                        StockBucket.UnclassifiedLegacy,
                        -requestedItem.Quantity,
                        occurredAtUtc,
                        referenceType: "SALE",
                        referenceId: sale.Id,
                        notes: "Saída de estoque por venda.");

                _dbContext.InventoryMovements.Add(
                    movement);
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
                request.Items.Sum(x => x.Quantity),
                request.Items.Sum(
                    x => x.Quantity * x.UnitPrice),
                occurredAtUtc,
                sale.DeliveryMethod,
                sale.DeliveryStatus);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }
}
