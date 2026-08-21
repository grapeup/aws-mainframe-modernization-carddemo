using System;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

/// <summary>
/// Centralised legacy-timestamp conversion.
/// Legacy fields are wall-clock readings in Europe/Berlin; the schema stores timestamptz (UTC instants).
/// </summary>
public static class TimezoneConverter
{
    private static readonly TimeZoneInfo BerlinZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>
    /// Converts a wall-clock DateTime (Kind=Unspecified or Local) assumed to be in Europe/Berlin
    /// to a UTC DateTime suitable for storing in a timestamptz column.
    /// </summary>
    public static DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, BerlinZone);
    }

    /// <summary>
    /// Converts a UTC DateTime back to Europe/Berlin wall-clock time for presentation.
    /// </summary>
    public static DateTime FromUtc(DateTime utc)
    {
        var specified = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(specified, BerlinZone);
    }
}
