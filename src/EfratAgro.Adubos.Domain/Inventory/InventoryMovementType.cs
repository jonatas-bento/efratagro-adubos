namespace EfratAgro.Adubos.Domain.Inventory;

public enum InventoryMovementType
{
    OpeningBalance = 1,
    Purchase = 2,
    Sale = 3,
    CustomerReturn = 4,
    SupplierReturn = 5,
    PositiveAdjustment = 6,
    NegativeAdjustment = 7
}
