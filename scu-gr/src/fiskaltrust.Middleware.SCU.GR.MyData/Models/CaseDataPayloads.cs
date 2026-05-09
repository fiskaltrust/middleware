using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.GR.MyData;

public class ftReceiptCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftReceiptCaseDataGreekPayload? GR { get; set; }
}

public class ftReceiptCaseDataGreekPayload
{
    public string? MerchantVATID { get; set; }
    public string? Series { get; set; }
    public long? AA { get; set; }
    public string? HashAlg { get; set; }
    public string? HashPayload { get; set; }

    [JsonPropertyName("mydataoverride")]
    public ReceiptRequestMyDataOverride? MyDataOverride { get; set; }

    [JsonPropertyName("PreviousReceiptReference")]
    public ftReceiptCaseDataPreviousReceiptReferenceGreekPayload? PreviousReceiptReference { get; set; }
}

public class ftReceiptCaseDataPreviousReceiptReferenceGreekPayload
{
    [JsonPropertyName("invoiceMark")]
    [JsonConverter(typeof(SingleOrListLongJsonConverter))]
    public List<long>? InvoiceMark { get; set; }
}

internal class SingleOrListLongJsonConverter : JsonConverter<List<long>?>
{
    public override List<long>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var values = new List<long>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                values.Add(ReadSingleLong(ref reader));
            }
            return values;
        }

        return [ReadSingleLong(ref reader)];
    }

    private static long ReadSingleLong(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            throw new JsonException($"Cannot parse '{text}' as a long invoiceMark.");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when reading invoiceMark.");
    }

    public override void Write(Utf8JsonWriter writer, List<long>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        foreach (var v in value)
        {
            writer.WriteNumberValue(v);
        }
        writer.WriteEndArray();
    }
}

public class ftChargeItemCaseDataPayload
{
    [JsonPropertyName("GR")]
    public ftChargeItemCaseDataGreekPayload? GR { get; set; }
}

public class ftChargeItemCaseDataGreekPayload
{
    [JsonPropertyName("mydataoverride")]
    public ChargeItemMyDataOverride? MyDataOverride { get; set; }
}
