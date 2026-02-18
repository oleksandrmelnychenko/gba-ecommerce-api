using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GBA.Common.Extensions;

public class TimeZoneDateTimeConverter : DateTimeConverterBase {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        if (!(value is DateTime)) writer.WriteValue(value);

        DateTime toParse = (DateTime)value;

        if (toParse.Kind == DateTimeKind.Unspecified) toParse = DateTime.SpecifyKind(toParse, DateTimeKind.Utc);

        if (toParse.Kind != DateTimeKind.Utc) toParse = toParse.ToUniversalTime();

        if (toParse.Hour == 0 && toParse.Minute == 0 && toParse.Second == 0)
            writer.WriteValue(toParse);
        else
            writer.WriteValue(
                TimeZoneInfo.ConvertTimeFromUtc(
                    toParse,
                    TimeZoneInfo.FindSystemTimeZoneById(
                        CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                            ? "FLE Standard Time"
                            : "Central European Standard Time"
                    )
                )
            );
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        if (!(reader.Value is DateTime dateTime)) return objectType == typeof(DateTime?) ? existingValue : DateTime.Now;

        if (dateTime.Year.Equals(1))
            dateTime = DateTime.Now;

        return TimeZoneInfo.ConvertTimeToUtc(dateTime);
    }
}