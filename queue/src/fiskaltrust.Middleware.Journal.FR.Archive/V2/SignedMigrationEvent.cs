using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>Where one v1 chain stopped, as a finish-migration receipt signed it (market-fr ADR 0003).</summary>
    public sealed class SignedV1ChainHead
    {
        public SignedV1ChainHead(string chain, long counter, string? lastHash, string? lastSignature)
        {
            Chain = chain;
            Counter = counter;
            LastHash = string.IsNullOrEmpty(lastHash) ? null : lastHash;
            LastSignature = string.IsNullOrEmpty(lastSignature) ? null : lastSignature;
        }

        public string Chain { get; }

        public long Counter { get; }

        public string? LastHash { get; }

        public string? LastSignature { get; }
    }

    /// <summary>
    /// Reads the migration event a finish-migration receipt (0x4022) signed, from its receipt QR code (archive-v2 §5,
    /// market-fr#221): field 20 of the LNE and self-certified data set, one record per v1 chain,
    /// <c>letter,counter,lastHash,lastSignature</c>, records separated by <c>;</c>; or the <c>migration</c> object of an
    /// InfoCert JWS payload. Whoever rebuilds state from a stored entry reads the event from here, never from the unsigned
    /// copy in <c>ftStateData</c>.
    /// </summary>
    public static class SignedMigrationEvent
    {
        private const int DataSetFieldCount = 25;
        private const int MigrationField = 19;

        /// <exception cref="FormatException">The QR code signs no readable migration event.</exception>
        public static IReadOnlyList<SignedV1ChainHead> Read(string qrCode)
        {
            var dot = qrCode.LastIndexOf('.');
            var content = dot < 0 ? "" : qrCode.Substring(0, dot);
            return content.IndexOf('|') >= 0 ? FromDataSet(content) : FromJws(content);
        }

        private static IReadOnlyList<SignedV1ChainHead> FromDataSet(string dataSet)
        {
            var fields = dataSet.Split('|');
            if (fields.Length != DataSetFieldCount)
            {
                throw new FormatException($"the data set has {fields.Length} fields, not the {DataSetFieldCount} that sign a migration event in field 20");
            }
            var field = fields[MigrationField];
            if (field.Length == 0)
            {
                throw new FormatException("field 20 signs no migration event");
            }
            return field.Split(';').Select(record =>
            {
                var values = record.Split(',');
                if (values.Length != 4 || values[0].Length == 0 || !long.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var counter))
                {
                    throw new FormatException($"the migration record '{record}' is not letter,counter,lastHash,lastSignature");
                }
                return new SignedV1ChainHead(values[0], counter, values[2], values[3]);
            }).ToList();
        }

        private static IReadOnlyList<SignedV1ChainHead> FromJws(string headerAndPayload)
        {
            var segments = headerAndPayload.Split('.');
            if (segments.Length != 2)
            {
                throw new FormatException("the signed content is neither a data set nor a JWS");
            }
            JsonDocument document;
            try
            {
                var payload = segments[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                document = JsonDocument.Parse(Convert.FromBase64String(payload));
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException)
            {
                throw new FormatException("the JWS payload is no Base64Url JSON", ex);
            }
            using (document)
            {
                if (Get(document.RootElement, "migration") is not { ValueKind: JsonValueKind.Object } migration
                    || Get(migration, "chains") is not { ValueKind: JsonValueKind.Array } chains)
                {
                    throw new FormatException("the JWS payload signs no migration event");
                }
                return chains.EnumerateArray().Select(chain =>
                {
                    var letter = Text(chain, "chain");
                    if (string.IsNullOrEmpty(letter) || Get(chain, "counter") is not { ValueKind: JsonValueKind.Number } counter || !counter.TryGetInt64(out var number))
                    {
                        throw new FormatException($"the migration chain {chain.GetRawText()} has no letter or counter");
                    }
                    return new SignedV1ChainHead(letter!, number, Text(chain, "lastHash"), Text(chain, "lastSignature"));
                }).ToList();
            }
        }

        /// <summary>A property matched case-insensitively: the InfoCert payload writes the event's keys as the SCU's type names them.</summary>
        private static JsonElement? Get(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind != JsonValueKind.Null)
                {
                    return property.Value;
                }
            }
            return null;
        }

        private static string? Text(JsonElement element, string name)
            => Get(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    }
}
