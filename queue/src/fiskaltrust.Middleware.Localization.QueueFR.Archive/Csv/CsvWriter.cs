using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv
{
    /// <summary>
    /// Writes RFC 4180 CSV. A field is quoted when it contains the delimiter, a double quote, CR or LF,
    /// or starts/ends with white space; quotes are doubled. Values are written verbatim otherwise: no
    /// spreadsheet formula escaping, because the fiscal data must stay byte-identical to the journal.
    /// </summary>
    public sealed class CsvWriter
    {
        private readonly TextWriter _writer;
        private readonly CsvOptions _options;

        public CsvWriter(TextWriter writer, CsvOptions options)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task WriteRowAsync(IEnumerable<string?> fields, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = new StringBuilder();
            var first = true;
            foreach (var field in fields)
            {
                if (!first)
                {
                    line.Append(_options.Delimiter);
                }
                first = false;
                line.Append(Escape(field, _options.Delimiter));
            }
            line.Append(_options.NewLine);
            await _writer.WriteAsync(line.ToString()).ConfigureAwait(false);
        }

        public static string Escape(string? field, char delimiter = ';')
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }
            var needsQuotes = char.IsWhiteSpace(field![0]) || char.IsWhiteSpace(field[field.Length - 1]);
            if (!needsQuotes)
            {
                foreach (var c in field)
                {
                    if (c == delimiter || c == '"' || c == '\r' || c == '\n')
                    {
                        needsQuotes = true;
                        break;
                    }
                }
            }
            return needsQuotes ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
        }
    }
}
