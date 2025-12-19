using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("DateTime value cannot be null or empty");
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime))
        {
            return dateTime;
        }

        throw new JsonException($"Unable to parse '{value}' as DateTime in ISO 8601 format");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        // Format as ISO 8601 (RFC3339)
        var formattedDateTime = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        writer.WriteStringValue(formattedDateTime);
    }
}