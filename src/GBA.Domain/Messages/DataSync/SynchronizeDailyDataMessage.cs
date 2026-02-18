using System;
using System.Globalization;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Messages.DataSync;

public sealed class SynchronizeDailyDataMessage {
    public SynchronizeDailyDataMessage(
        Guid userNetId,
        bool forAmg,
        DateTime from,
        DateTime to,
        SyncProductConsignmentType[] types) {
        DateTime fromDate = TimeZoneInfo.ConvertTimeFromUtc(
            from,
            TimeZoneInfo.FindSystemTimeZoneById(
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                    ? "FLE Standard Time"
                    : "Central European Standard Time"
            )
        );

        DateTime toDate = TimeZoneInfo.ConvertTimeFromUtc(
            to,
            TimeZoneInfo.FindSystemTimeZoneById(
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                    ? "FLE Standard Time"
                    : "Central European Standard Time"
            )
        );

        UserNetId = userNetId;
        ForAmg = forAmg;
        From = fromDate;
        To = toDate.AddSeconds(59);
        //To = toDate;
        Types = types;
    }

    public Guid UserNetId { get; }

    public bool ForAmg { get; }
    public DateTime From { get; }
    public DateTime To { get; }

    public SyncProductConsignmentType[] Types { get; set; }
}