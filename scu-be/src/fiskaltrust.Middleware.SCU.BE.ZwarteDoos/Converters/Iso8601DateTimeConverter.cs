using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;

/// <summary>
/// Custom JSON converter to ensure DateTime values are serialized in ISO 8601 format (RFC3339)
/// Format: YYYY-MM-DDTHH:MM:SS+HH:SS (e.g., 2025-10-24T16:19:24+02:00)
/// </summary>
public class Iso8601DateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("DateTime value cannot be null or empty");
        }

        if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime))
        {
            return dateTime;
        }

        throw new JsonException($"Unable to parse '{value}' as DateTime in ISO 8601 format");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Ensure the DateTime has a DateTimeKind, default to Local if Unspecified
        var dateTimeToWrite = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value;
        
        // Format as ISO 8601 with timezone offset (RFC3339)
        var formattedDateTime = dateTimeToWrite.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        writer.WriteStringValue(formattedDateTime);
    }
}