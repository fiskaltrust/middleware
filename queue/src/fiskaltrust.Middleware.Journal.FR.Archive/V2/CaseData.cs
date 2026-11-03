using System.Collections.Generic;
using System.Text.Json;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>One row of <c>case-data.csv</c> (archive-v2 field contract §8).</summary>
    public sealed class CaseDataRow
    {
        public CaseDataRow(string owner, string? position, string field, string path, string valueType, string? value)
        {
            Owner = owner;
            Position = position;
            Field = field;
            Path = path;
            ValueType = valueType;
            Value = value;
        }

        /// <summary><c>Receipt</c>, <c>ChargeItem</c> or <c>PayItem</c>.</summary>
        public string Owner { get; }

        /// <summary>The item's <c>Position</c> as sent; null for the receipt.</summary>
        public string? Position { get; }

        public string Field { get; }

        /// <summary>RFC 6901 JSON pointer inside the field; empty for its root.</summary>
        public string Path { get; }

        public string ValueType { get; }

        public string? Value { get; }
    }

    /// <summary>
    /// Flattens what the POS sent in object form into one row per container and per scalar, in document
    /// order, so the JSON can be rebuilt without ambiguity and still no column holds JSON (ADR 0006).
    /// </summary>
    public static class CaseData
    {
        // cbPreviousReceiptReference too: the journal column joins an array with commas, which loses the
        // boundaries of references that contain one.
        private static readonly string[] ReceiptFields = { "ftReceiptCaseData", "cbCustomer", "cbUser", "cbArea", "cbSettlement", "cbPreviousReceiptReference" };

        public static IEnumerable<CaseDataRow> Rows(ArchiveEntry entry)
        {
            foreach (var field in ReceiptFields)
            {
                foreach (var row in Field(entry.Request, "Receipt", null, field))
                {
                    yield return row;
                }
            }
            foreach (var (owner, items, field) in new[] { ("ChargeItem", "cbChargeItems", "ftChargeItemCaseData"), ("PayItem", "cbPayItems", "ftPayItemCaseData") })
            {
                foreach (var item in ArchiveEntry.Array(entry.Request, items))
                {
                    var position = ArchiveEntry.Property(item, "Position") is { } p ? (p.ValueKind == JsonValueKind.String ? p.GetString() : p.GetRawText()) : null;
                    foreach (var row in Field(item, owner, position, field))
                    {
                        yield return row;
                    }
                }
            }
        }

        private static IEnumerable<CaseDataRow> Field(JsonElement parent, string owner, string? position, string field)
        {
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(field, out var value))
            {
                return new CaseDataRow[0];
            }
            var rows = new List<CaseDataRow>();
            Walk(value, string.Empty, owner, position, field, rows);
            return rows;
        }

        private static void Walk(JsonElement value, string path, string owner, string? position, string field, List<CaseDataRow> rows)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    rows.Add(new CaseDataRow(owner, position, field, path, "object", null));
                    foreach (var property in value.EnumerateObject())
                    {
                        Walk(property.Value, path + "/" + Escape(property.Name), owner, position, field, rows);
                    }
                    break;
                case JsonValueKind.Array:
                    rows.Add(new CaseDataRow(owner, position, field, path, "array", null));
                    var index = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        Walk(item, path + "/" + index++, owner, position, field, rows);
                    }
                    break;
                case JsonValueKind.String:
                    rows.Add(new CaseDataRow(owner, position, field, path, "string", value.GetString()));
                    break;
                case JsonValueKind.Number:
                    rows.Add(new CaseDataRow(owner, position, field, path, "number", value.GetRawText()));
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    rows.Add(new CaseDataRow(owner, position, field, path, "boolean", value.ValueKind == JsonValueKind.True ? "true" : "false"));
                    break;
                default:
                    rows.Add(new CaseDataRow(owner, position, field, path, "null", null));
                    break;
            }
        }

        /// <summary>RFC 6901: <c>~</c> becomes <c>~0</c>, then <c>/</c> becomes <c>~1</c>.</summary>
        private static string Escape(string key) => key.Replace("~", "~0").Replace("/", "~1");
    }
}
