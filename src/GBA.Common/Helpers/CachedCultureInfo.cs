using System;
using System.Globalization;

namespace GBA.Common.Helpers;

/// <summary>
/// Cached CultureInfo and TimeZoneInfo instances to avoid repeated allocations.
/// </summary>
public static class CachedCultureInfo {
    /// <summary>
    /// Ukrainian culture info (uk-UA).
    /// </summary>
    public static readonly CultureInfo Ukrainian = new("uk-UA");

    /// <summary>
    /// Invariant culture info.
    /// </summary>
    public static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// Central European Standard Time zone.
    /// </summary>
    public static readonly TimeZoneInfo CentralEuropeanTime =
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    /// <summary>
    /// FLE Standard Time zone (Ukraine).
    /// </summary>
    public static readonly TimeZoneInfo UkraineTime =
        TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

    /// <summary>
    /// Gets the appropriate time zone based on the current culture.
    /// </summary>
    /// <returns>FLE Standard Time for Ukrainian culture, Central European Standard Time otherwise.</returns>
    public static TimeZoneInfo GetCurrentTimeZone() {
        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk", StringComparison.OrdinalIgnoreCase)
            ? UkraineTime
            : CentralEuropeanTime;
    }

    /// <summary>
    /// Converts a DateTime to the appropriate timezone based on culture.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>The converted UTC DateTime.</returns>
    public static DateTime ConvertToUtc(DateTime date) {
        return TimeZoneInfo.ConvertTimeToUtc(date, GetCurrentTimeZone());
    }

    /// <summary>
    /// Converts a DateTime from UTC to the appropriate local timezone.
    /// </summary>
    /// <param name="utcDate">The UTC date to convert.</param>
    /// <returns>The converted local DateTime.</returns>
    public static DateTime ConvertFromUtc(DateTime utcDate) {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, GetCurrentTimeZone());
    }
}
