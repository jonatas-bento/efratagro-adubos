using EfratAgro.Adubos.Application.Sales;
using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Customers;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Domain.Sales;
using EfratAgro.Adubos.Infrastructure.Deliveries;
using EfratAgro.Adubos.Infrastructure.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using EfratAgro.Adubos.Infrastructure.Sales;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class ReservedSaleInventoryFlowTests
{
    [Fact]
    public async Task ReservedSale_ShouldReserveThenReducePhysicalOnDelivery()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<
                    AdubosDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        await using var dbContext =
            new AdubosDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        var supplier =
            new Supplier(
                "Fornecedor Teste");

        var product =
            new Product(
                "Produto Safra",
                supplier.Id);

        var warehouse =
            new Warehouse(
                "Armazém Principal");

        var customer =
            new Customer(
                "Cliente Safra",
                phone: null);

        dbContext.Suppliers.Add(
            supplier);

        dbContext.Products.Add(
            product);

        dbContext.Warehouses.Add(
            warehouse);

        dbContext.Customers.Add(
            customer);

        dbContext.InventoryMovements.Add(
            new InventoryMovement(
                product.Id,
                warehouse.Id,
                InventoryMovementType.OpeningBalance,
                StockBucket.Normal,
                100m,
                DateTime.UtcNow,
                referenceType:
                    "TEST_OPENING",
                notes:
                    "Saldo inicial do teste."));

        await dbContext.SaveChangesAsync();

        var sales =
            new SaleService(
                dbContext);

        var saleResult =
            await sales.CreateAsync(
                new CreateSaleRequest(
                    customer.Id,
                    [
                        new CreateSaleItemRequest(
                            product.Id,
                            10m,
                            100m)
                    ],
                    DeliveryMethod.Delivery,
                    [
                        new CreateSaleReceivableRequest(
                            1,
                            DateTime.UtcNow.Date,
                            1000m)
                    ])
                {
                    StockMode =
                        SaleStockMode.Reserved
                });

        Assert.Equal(
            SaleStockMode.Reserved,
            saleResult.StockMode);

        Assert.Equal(
            1,
            await dbContext
                .InventoryMovements
                .CountAsync());

        Assert.Equal(
            100m,
            await dbContext
                .InventoryMovements
                .SumAsync(
                    x => x.Quantity));

        var reservation =
            await dbContext
                .InventoryReservations
                .SingleAsync();

        Assert.Equal(
            10m,
            reservation.Quantity);

        Assert.Equal(
            InventoryReservationStatus.Active,
            reservation.Status);

        var inventory =
            new InventoryQueryService(
                dbContext);

        var beforeDelivery =
            Assert.Single(
                await inventory
                    .GetInventoryAsync(
                        null));

        Assert.Equal(
            100m,
            beforeDelivery.Quantity);

        Assert.Equal(
            10m,
            beforeDelivery.ReservedQuantity);

        Assert.Equal(
            90m,
            beforeDelivery.AvailableQuantity);

        var deliveries =
            new DeliveryService(
                dbContext);

        await deliveries.MarkDeliveredAsync(
            saleResult.SaleId);

        Assert.Equal(
            2,
            await dbContext
                .InventoryMovements
                .CountAsync());

        Assert.Equal(
            90m,
            await dbContext
                .InventoryMovements
                .SumAsync(
                    x => x.Quantity));

        var fulfilled =
            await dbContext
                .InventoryReservations
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            InventoryReservationStatus.Fulfilled,
            fulfilled.Status);

        Assert.NotNull(
            fulfilled.FulfilledAtUtc);

        var afterDelivery =
            Assert.Single(
                await inventory
                    .GetInventoryAsync(
                        null));

        Assert.Equal(
            90m,
            afterDelivery.Quantity);

        Assert.Equal(
            0m,
            afterDelivery.ReservedQuantity);

        Assert.Equal(
            90m,
            afterDelivery.AvailableQuantity);
    }
}
