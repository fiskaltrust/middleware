using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Writing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.UnitTest
{
    public class CsvTests
    {
        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("a;b", "\"a;b\"")]
        [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
        [InlineData("two\r\nlines", "\"two\r\nlines\"")]
        [InlineData("line\nfeed", "\"line\nfeed\"")]
        [InlineData(" padded", "\" padded\"")]
        [InlineData("a,b", "a,b")]
        [InlineData("=SUM(A1)", "=SUM(A1)")]
        public void Fields_are_escaped_per_rfc_4180(string? field, string expected)
            => CsvWriter.Escape(field).Should().Be(expected);

        [Fact]
        public void Values_are_culture_invariant_even_under_a_french_culture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

                CsvValueFormatter.Format(1234.50m).Should().Be("1234.50");
                CsvValueFormatter.Format(-0.1m).Should().Be("-0.1");
                CsvValueFormatter.Format(5.5d).Should().Be("5.5");
                CsvValueFormatter.Format(42).Should().Be("42");
                CsvValueFormatter.Format(true).Should().Be("true");
                CsvValueFormatter.Format(null).Should().BeEmpty();
                CsvValueFormatter.Format(Guid.Parse("6F9619FF-8B86-D011-B42D-00CF4FC964FF")).Should().Be("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
                CsvValueFormatter.Format(DateTimeKind.Local).Should().Be("2");
                CsvValueFormatter.Format(new byte[] { 1, 2, 3 }).Should().Be("AQID");
                CsvValueFormatter.Hex(0x4652_0000_0000_0001).Should().Be("0x4652000000000001");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Moments_keep_full_tick_precision()
        {
            // market-fr#127: the v1 CSV used "G" and lost the milliseconds.
            var moment = new DateTime(2026, 10, 12, 8, 15, 42, DateTimeKind.Utc).AddTicks(1234567);

            var text = CsvValueFormatter.Format(moment);

            text.Should().Be("2026-10-12T08:15:42.1234567Z");
            var parsed = DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            parsed.Should().Be(moment);
            parsed.Kind.Should().Be(DateTimeKind.Utc);
            var offset = new DateTimeOffset(moment.Ticks, TimeSpan.FromHours(2));
            DateTimeOffset.Parse(CsvValueFormatter.Format(offset), CultureInfo.InvariantCulture).Should().Be(offset);
        }

        [Fact]
        public async Task Sub_archive_files_are_utf8_without_bom_with_crlf_and_one_file_per_chain()
        {
            var ticket = new TestRecord
            {
                Moment = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc).AddTicks(1),
                Total = 12.30m,
                Text = "Crème brûlée; \"maison\"",
                Lines = { new TestLine { Description = "Café", Amount = 2.5m }, new TestLine { Description = "Thé", Amount = 9.8m } }
            };
            var closing = new TestRecord { Chain = "G", Moment = ticket.Moment.AddHours(1), Total = 12.30m };
            var subArchive = new SubArchive<TestRecord>(ticket.Key, new ArchivePeriod(new DateTime(2024, 1, 1), new DateTime(2025, 1, 1)), new[] { ticket, closing }, ticket.Moment, closing.Moment);
            var sink = new MemorySink();

            await TestFixtures.Writer(ArchiveFormatVersion.V2).WriteAsync(subArchive, sink, CancellationToken.None);

            sink.Files.Keys.Should().Equal("journal-T.csv", "journal-G.csv", "charge-items.csv");
            var journal = sink.Files["journal-T.csv"];
            journal.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
            Encoding.UTF8.GetString(journal).Should().Be(
                "ftQueueItemID;ftReceiptMoment;Totalizer;Text\r\n" +
                $"{ticket.Id:D};2024-05-01T10:00:00.0000001Z;12.30;\"Crème brûlée; \"\"maison\"\"\"\r\n");
            Encoding.UTF8.GetString(sink.Files["journal-G.csv"]).Should().Be($"ftQueueItemID;Totalizer\r\n{closing.Id:D};12.30\r\n");
            Encoding.UTF8.GetString(sink.Files["charge-items.csv"]).Should().Be(
                "ftQueueItemID;Position;Description;Amount\r\n" +
                $"{ticket.Id:D};1;Café;2.5\r\n" +
                $"{ticket.Id:D};2;Thé;9.8\r\n");
        }

        [Fact]
        public void Column_and_file_definitions_are_validated()
        {
            var column = new CsvColumn<TestRecord>("a", r => r.Id);
            FluentActions.Invoking(() => new FlatCsvFile<TestRecord>("x.csv", new[] { column, column })).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => new FlatCsvFile<TestRecord>("../x.csv", new[] { column })).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => new FlatCsvFile<TestRecord>("manifest.json", new[] { column })).Should().Throw<ArgumentException>();
        }

        private sealed class MemorySink : IArchiveFileSink
        {
            public System.Collections.Generic.Dictionary<string, byte[]> Files { get; } = new System.Collections.Generic.Dictionary<string, byte[]>();

            public async Task WriteFileAsync(string fileName, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken)
            {
                using var buffer = new MemoryStream();
                await write(buffer, cancellationToken);
                Files.Add(fileName, buffer.ToArray());
            }
        }
    }
}
