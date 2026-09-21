namespace EfratAgro.Adubos.Infrastructure.Common;

internal static class BusinessDate
{
    private static readonly TimeZoneInfo BusinessTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            "America/Sao_Paulo");

    public static DateTime Today
    {
        get
        {
            var localNow =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    BusinessTimeZone);

            return localNow.Date;
        }
    }

    public static DateTime StartOfDayUtc(
        DateOnly date)
    {
        var localDateTime =
            DateTime.SpecifyKind(
                date.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(
            localDateTime,
            BusinessTimeZone);
    }

    public static DateTime ExclusiveEndUtc(
        DateOnly date)
    {
        return StartOfDayUtc(
            date.AddDays(1));
    }

    public static DateOnly ToLocalDate(
        DateTime utcDateTime)
    {
        var normalizedUtc =
            utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(
                    utcDateTime,
                    DateTimeKind.Utc);

        var localDateTime =
            TimeZoneInfo.ConvertTimeFromUtc(
                normalizedUtc,
                BusinessTimeZone);

        return DateOnly.FromDateTime(
            localDateTime);
    }

    public static void ValidateRange(
        DateOnly? from,
        DateOnly? to)
    {
        if (
            from.HasValue &&
            to.HasValue &&
            from.Value > to.Value)
        {
            throw new ArgumentException(
                "A data inicial não pode ser posterior à data final.");
        }
    }
}
