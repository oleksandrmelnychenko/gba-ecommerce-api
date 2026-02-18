using System;

namespace GBA.Common.Helpers;

public static class DateTimeHelper {
    private const string DATE_FORMAT_STRING = "dd/MM/yyyy HH:mm:ss - ";

    // Cache TimeZoneInfo to avoid repeated lookup
    private static readonly TimeZoneInfo UkraineTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

    public static string GetCurrentDateInString() {
        return DateTime.Now.ToString(DATE_FORMAT_STRING);
    }

    public static DateTime ConvertDateTimeToUtcInUkraineTimeZone(DateTime date) {
        return TimeZoneInfo.ConvertTimeToUtc(date, UkraineTimeZone);
    }
}