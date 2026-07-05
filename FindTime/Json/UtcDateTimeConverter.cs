using System.Text.Json;
using System.Text.Json.Serialization;

namespace FindTime.Json;

// Every DateTime in this app is UTC (set via DateTime.UtcNow server-side, or
// treated as a UTC instant everywhere it's compared - see FindFreeSlotAsync,
// GetAllEventsNextWeekAsync, etc). Npgsql reads "timestamp without time zone"
// columns back with Kind=Unspecified, and System.Text.Json only appends the
// "Z" suffix for Kind=Utc - so without this converter, every timestamp goes
// out over the wire looking like local time, and a browser in a non-UTC
// timezone silently misinterprets it, shifting every displayed time by the
// browser's UTC offset.
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcValue = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utcValue);
    }
}

public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var utcValue = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        writer.WriteStringValue(utcValue);
    }
}
