using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.Middleware.Journal.FR.Archive.Writing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary><c>requests.jsonl</c> (archive-v2 §9): the stored request, verbatim.</summary>
    public class RequestsFileTests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // What a re-serialization would change: key order, spacing, an amount past double precision,
        // escapes, non-ASCII text and a line break inside a string.
        private const string Stored = "{ \"ftCashBoxID\":\"00000000-0000-0000-0000-000000000001\",  \"ftReceiptCase\":5067112530745229313," +
            "\"cbReceiptReference\":\"R-1\",\"cbReceiptMoment\":\"2026-03-01T10:00:00.123Z\",\"cbTerminalID\":\"T1\"," +
            "\"cbChargeItems\":[{\"Amount\":1.234567890123456789012,\"Description\":\"Caf\\u00e9 <crème>\\n\\\"x\\\"\",\"Position\":1}]}";

        private static ArchiveEntry Entry(string request, long row = 1)
            => ArchiveEntry.Read(Guid.NewGuid(), QueueId, row, request, ArchiveV2FilesTests.Response($"ft{row}#T{row}", $"t{row}"))!;

        private static async Task<string> Write(params ArchiveEntry[] entries)
        {
            using var writer = new StringWriter();
            await new RequestsFile().WriteAsync(entries, writer, new CsvOptions(), default);
            return writer.ToString();
        }

        [Fact]
        public async Task The_request_round_trips_byte_for_byte()
        {
            var entry = Entry(Stored);

            var line = (await Write(entry)).TrimEnd('\n');

            using var document = JsonDocument.Parse(line);
            var request = document.RootElement.GetProperty("request").GetString()!;
            request.Should().Be(Stored);
            Encoding.UTF8.GetBytes(request).Should().Equal(Encoding.UTF8.GetBytes(Stored));
        }

        [Fact]
        public async Task One_lf_terminated_line_per_entry_with_the_keys_in_contract_order()
        {
            var first = Entry(Stored, 1);
            var second = Entry(ArchiveV2FilesTests.Request(), 2);

            var content = await Write(first, second);

            content.Should().EndWith("\n").And.NotContain("\r");
            var lines = content.Split('\n').Where(l => l.Length > 0).ToList();
            lines.Should().HaveCount(2);
            using var document = JsonDocument.Parse(lines[1]);
            document.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("ftQueueItemID", "ftQueueRow", "ftReceiptIdentification", "ftReceiptMoment", "request");
            document.RootElement.GetProperty("ftQueueItemID").GetString().Should().Be(second.QueueItemId.ToString("D"));
            document.RootElement.GetProperty("ftQueueRow").GetInt64().Should().Be(2);
            document.RootElement.GetProperty("ftReceiptIdentification").GetString().Should().Be("ft2#T2");
            document.RootElement.GetProperty("ftReceiptMoment").GetString().Should().Be("2026-03-01T10:00:01.4560000Z");
        }

        [Theory]
        [InlineData("{\"ftQueueItemID\":\"x\",\"ftQueueRow\":1,\"ftReceiptIdentification\":\"a\",\"ftReceiptMoment\":\"m\",\"request\":{}}\n", "not a string")]
        [InlineData("{\"ftQueueItemID\":\"x\",\"ftQueueRow\":1,\"ftReceiptMoment\":\"m\",\"request\":\"{}\"}\n", "no ftReceiptIdentification")]
        [InlineData("ftQueueItemID;request\n", "not valid JSON")]
        [InlineData("[]\n", "not a JSON object")]
        public void Validate_reports_a_malformed_line(string content, string expected)
        {
            RequestsFile.Validate(new MemoryStream(Encoding.UTF8.GetBytes(content))).Should().Contain(expected);
        }

        [Fact]
        public async Task The_packaged_file_is_listed_hashed_and_verified()
        {
            var entries = new[] { Entry(Stored, 1), Entry(ArchiveV2FilesTests.Request(), 2) };
            var archive = ArchiveSplitter.Split(entries, e => e.ReceiptMoment, ArchiveV2Files.KeyOf, new ArchivePeriod(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1))).Single();
            using var certificate = TestFixtures.EcdsaCertificate();
            using var output = new MemoryStream();

            await ArchiveV2Files.CreatePackager(_ => new X509ArchiveSigner(certificate))
                .WriteAsync(archive, new ArchiveMetadata("12345678901234", DateTime.UtcNow), output);

            var result = ArchiveVerifier.Verify(new MemoryStream(output.ToArray()));
            result.AllErrors.Should().BeEmpty();
            result.Manifest!.Files.Should().ContainSingle(f => f.Name == RequestsFile.Name && f.Hash.Length > 0 && f.Signature.Length > 0);
            using var zip = new ZipArchive(new MemoryStream(output.ToArray()));
            using var reader = new StreamReader(zip.GetEntry(RequestsFile.Name)!.Open(), Encoding.UTF8);
            var firstLine = (await reader.ReadLineAsync())!;
            JsonDocument.Parse(firstLine).RootElement.GetProperty("request").GetString().Should().Be(Stored);
        }

        [Fact]
        public async Task The_verifier_reports_a_signed_but_malformed_requests_file()
        {
            // Signed and listed correctly, so only the shape check can catch it.
            var writer = new CsvArchiveWriter<TestRecord>(ArchiveFormatVersion.V2, new ICsvFileDefinition<TestRecord>[]
            {
                new FlatCsvFile<TestRecord>(RequestsFile.Name, new[] { new CsvColumn<TestRecord>("ftQueueItemID", r => r.Id) }),
            });
            var archive = ArchiveSplitter.Split(new[] { TestRecord.At(2024, 3, 1) }, r => r.Moment, r => r.Key,
                new ArchivePeriod(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))).Single();
            using var certificate = TestFixtures.EcdsaCertificate();
            using var output = new MemoryStream();

            await new ArchivePackager<TestRecord>(new[] { writer }, new X509ArchiveSigner(certificate), r => r.Describe()).WriteAsync(archive, new ArchiveMetadata("12345678901234", DateTime.UtcNow), output);

            ArchiveVerifier.Verify(new MemoryStream(output.ToArray())).AllErrors.Should().Contain(e => e.Contains(RequestsFile.Name) && e.Contains("not valid JSON"));
        }
    }
}
