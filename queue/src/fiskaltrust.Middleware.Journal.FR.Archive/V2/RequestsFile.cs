using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// <c>requests.jsonl</c> (archive-v2 field contract §9): the request each archived entry's queue item
    /// stored, verbatim. A verifier recomputes the LNE request hash (ADR 0013) from it, which the re-read
    /// CSV values cannot give. JSON Lines, not CSV: a CSV column holding JSON is what ADR 0006 removes, and
    /// a JSON string decodes to exactly the stored text.
    /// </summary>
    /// <remarks>
    /// One line per entry, in the archive file's order, LF-terminated whatever <see cref="CsvOptions.NewLine"/>
    /// says. It implements the CSV file interface only to be written into an archive file like the others.
    /// </remarks>
    public sealed class RequestsFile : ICsvFileDefinition<ArchiveEntry>
    {
        public const string Name = "requests.jsonl";

        private static readonly string[] Keys = { "ftQueueItemID", "ftQueueRow", "ftReceiptIdentification", "ftReceiptMoment", "request" };

        private static readonly JsonWriterOptions WriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public string FileName => Name;

        public async Task WriteAsync(IReadOnlyList<ArchiveEntry> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            foreach (var entry in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteAsync(Line(entry)).ConfigureAwait(false);
                await writer.WriteAsync('\n').ConfigureAwait(false);
            }
        }

        internal static string Line(ArchiveEntry entry)
        {
            using var buffer = new MemoryStream();
            using (var json = new Utf8JsonWriter(buffer, WriterOptions))
            {
                json.WriteStartObject();
                json.WriteString(Keys[0], entry.QueueItemId.ToString("D"));
                json.WriteNumber(Keys[1], entry.QueueRow);
                json.WriteString(Keys[2], entry.ReceiptIdentification);
                json.WriteString(Keys[3], entry.ReceiptMoment.ToString("o", CultureInfo.InvariantCulture));
                json.WriteString(Keys[4], entry.RawRequest);
                json.WriteEndObject();
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        /// <summary>
        /// Checks the file's shape: every line an object with the five keys and a string <c>request</c>.
        /// Returns the first problem, or null. The hash and signature are checked like any file's.
        /// </summary>
        public static string? Validate(Stream content)
        {
            using var reader = new StreamReader(content, new UTF8Encoding(false, throwOnInvalidBytes: true));
            var number = 0;
            try
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    number++;
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        return $"line {number} is not a JSON object";
                    }
                    foreach (var key in Keys)
                    {
                        if (!root.TryGetProperty(key, out _))
                        {
                            return $"line {number} has no {key}";
                        }
                    }
                    if (root.GetProperty("request").ValueKind != JsonValueKind.String)
                    {
                        return $"line {number}: request is not a string";
                    }
                }
            }
            catch (Exception e) when (e is JsonException || e is DecoderFallbackException)
            {
                return $"line {number} is not valid JSON ({e.Message})";
            }
            return null;
        }
    }
}
