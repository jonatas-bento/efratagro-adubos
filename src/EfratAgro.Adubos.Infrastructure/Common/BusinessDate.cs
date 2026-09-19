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
}
