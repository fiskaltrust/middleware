using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv
{
    /// <summary>
    /// How the archive CSV files are written (ADR 0006). The defaults are the provisional archive format;
    /// the field-level content (column sets) is defined elsewhere, see <see cref="ICsvFileDefinition{TRecord}"/>.
    /// </summary>
    public sealed class CsvOptions
    {
        /// <summary>Field separator. Defaults to a semicolon (archive-v2 field contract, market-fr#227).</summary>
        public char Delimiter { get; set; } = ';';

        /// <summary>Record separator. Defaults to CRLF (RFC 4180).</summary>
        public string NewLine { get; set; } = "\r\n";

        /// <summary>Whether the UTF-8 files start with a byte-order mark. Defaults to false.</summary>
        public bool WriteByteOrderMark { get; set; }

        /// <summary>Whether the first line holds the column names. Defaults to true.</summary>
        public bool WriteHeader { get; set; } = true;

        internal Encoding Encoding => new UTF8Encoding(WriteByteOrderMark, throwOnInvalidBytes: true);
    }
}
