using System;

namespace GBA.Common.Helpers;

public static class OrderHistoryPeriod {
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "FLE Standard Time" : "Europe/Kyiv");
    // The Ukrainian storefront uses calendar dates in Kyiv, including its ru locale.
    // Convert each midnight separately so DST days may span 23 or 25 hours.
    public static (DateTime FromUtc, DateTime ToUtcExclusive) FromCalendarDates(
        DateTime from, DateTime to, DateTime utcNow) {
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Kyiv).Date;
        DateTime first = from.Year == 1 ? today : from.Date;
        DateTime last = to.Year == 1 ? today : to.Date;
        return (
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(first, DateTimeKind.Unspecified), Kyiv),
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(last.AddDays(1), DateTimeKind.Unspecified), Kyiv));
    }
}
