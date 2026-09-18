namespace EfratAgro.Adubos.Domain.Inventory;

public enum StockBucket
{
    Normal = 1,
    SafraTrava = 2,

    /// <summary>
    /// Legacy stock imported before the Normal/Safra-Trava
    /// classification was confirmed with the customer.
    /// </summary>
    UnclassifiedLegacy = 99
}
