using System;

namespace GBA.Common.Extensions;

public static class DateTimeExtensions {
    public static bool DateTimeEqualExtension(this DateTime value, DateTime otherValue) {
        return value.Date.Equals(otherValue.Date) && value.Hour.Equals(otherValue.Hour) && value.Minute.Equals(otherValue.Minute);
    }
}