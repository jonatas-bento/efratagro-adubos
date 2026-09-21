namespace EfratAgro.Adubos.Infrastructure.DataQuality;

internal static class DataQualityRules
{
    public static void ValidateOperationalTrustedFrom(
        DateOnly trustedFrom,
        DateOnly? inventoryHistoryAvailableFrom,
        DateOnly today)
    {
        if (!inventoryHistoryAvailableFrom.HasValue)
        {
            throw new InvalidOperationException(
                "Não é possível definir a data de confiança " +
                "operacional antes de existir um baseline " +
                "técnico de estoque.");
        }

        if (
            trustedFrom <
            inventoryHistoryAvailableFrom.Value)
        {
            throw new ArgumentException(
                "A data de confiança operacional não pode " +
                "ser anterior ao início do histórico físico " +
                $"disponível ({inventoryHistoryAvailableFrom:dd/MM/yyyy}).",
                nameof(trustedFrom));
        }

        if (trustedFrom > today)
        {
            throw new ArgumentException(
                "A data de confiança operacional não pode " +
                "estar no futuro.",
                nameof(trustedFrom));
        }
    }
}
