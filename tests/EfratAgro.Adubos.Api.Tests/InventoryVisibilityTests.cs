using EfratAgro.Adubos.Domain.Catalog;
using EfratAgro.Adubos.Domain.Inventory;
using EfratAgro.Adubos.Infrastructure.Inventory;
using EfratAgro.Adubos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class InventoryVisibilityTests
{
    [Fact]
    public async Task GetInventoryAsync_ShouldKeepInactiveProductWithPhysicalStockVisible()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AdubosDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AdubosDbContext(options);

        await dbContext.Database
            .EnsureCreatedAsync();

        var supplier =
            new Supplier(
                "FORNECEDOR TESTE");

        var activeProduct =
            new Product(
                "PRODUTO ATIVO",
                supplier.Id);

        var inactiveProduct =
            new Product(
                "PRODUTO HISTORICO",
                supplier.Id);

        inactiveProduct.Deactivate();

        var warehouse =
            new Warehouse(
                "Armazém Principal");

        dbContext.Suppliers.Add(
            supplier);

        dbContext.Products.AddRange(
            activeProduct,
            inactiveProduct);

        dbContext.Warehouses.Add(
            warehouse);

        dbContext.InventoryMovements.AddRange(
            new InventoryMovement(
                activeProduct.Id,
                warehouse.Id,
                InventoryMovementType.Purchase,
                StockBucket.Normal,
                10m,
                DateTime.UtcNow,
                referenceType: "TEST",
                referenceId: Guid.NewGuid(),
                notes: "Saldo ativo de teste."),
            new InventoryMovement(
                inactiveProduct.Id,
                warehouse.Id,
                InventoryMovementType.Purchase,
                StockBucket.Normal,
                5m,
                DateTime.UtcNow,
                referenceType: "TEST",
                referenceId: Guid.NewGuid(),
                notes: "Saldo histórico de teste."));

        await dbContext.SaveChangesAsync();

        var service =
            new InventoryQueryService(
                dbContext);

        var inventory =
            await service.GetInventoryAsync(
                null);

        Assert.Equal(
            2,
            inventory.Count);

        var historical =
            Assert.Single(
                inventory,
                item =>
                    item.ProductId ==
                    inactiveProduct.Id);

        Assert.Equal(
            "PRODUTO HISTORICO",
            historical.ProductName);

        Assert.False(
            historical.IsActive);

        Assert.Equal(
            5m,
            historical.Quantity);
    }
}
