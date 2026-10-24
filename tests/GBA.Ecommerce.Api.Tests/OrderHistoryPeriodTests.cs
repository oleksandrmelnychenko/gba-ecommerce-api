using GBA.Common.Helpers;

namespace GBA.Ecommerce.Api.Tests;

public sealed class OrderHistoryPeriodTests {
    [Theory]
    [InlineData(2026, 10, 24, 21, 24)]
    [InlineData(2026, 10, 25, 21, 25)]
    [InlineData(2026, 3, 29, 22, 23)]
    public void Calendar_range_covers_the_full_Kyiv_day_including_DST(int year, int month, int day, int utcStartHour, int hours) {
        var date = new DateTime(year, month, day);
        var range = OrderHistoryPeriod.FromCalendarDates(date, date, DateTime.UtcNow);
        Assert.Equal(date.AddDays(-1).AddHours(utcStartHour), range.FromUtc);
        Assert.Equal(TimeSpan.FromHours(hours), range.ToUtcExclusive - range.FromUtc);
        Assert.Equal(DateTimeKind.Utc, range.FromUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, range.ToUtcExclusive.Kind);
    }

    [Fact]
    public void Default_dates_use_Kyiv_today_even_while_UTC_is_yesterday() {
        var range = OrderHistoryPeriod.FromCalendarDates(default, default, new DateTime(2026, 10, 23, 22, 0, 0));
        Assert.Equal(new DateTime(2026, 10, 23, 21, 0, 0), range.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 24, 21, 0, 0), range.ToUtcExclusive);
    }

    [Fact]
    public void Time_components_do_not_expand_the_calendar_end_date() {
        var range = OrderHistoryPeriod.FromCalendarDates(new DateTime(2026, 10, 24, 10, 0, 0), new DateTime(2026, 10, 24, 23, 59, 59), DateTime.UtcNow);
        Assert.Equal(new DateTime(2026, 10, 23, 21, 0, 0), range.FromUtc);
        Assert.Equal(new DateTime(2026, 10, 24, 21, 0, 0), range.ToUtcExclusive);
    }
}
