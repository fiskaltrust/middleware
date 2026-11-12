using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;
using static fiskaltrust.Middleware.Journal.FR.Archive.UnitTest.ArchiveV2FilesTests;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// <c>journal-A-archive.csv</c> carries the boundaries the archive receipt signed (archive-v2 §1.4), read from
    /// <c>ftStateData.FR.Archive</c>, and not the closing totals of §1.3.
    /// </summary>
    public class ArchiveJournalATests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid Previous = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        private static readonly Guid First = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
        private static readonly Guid Last = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
        private static readonly Guid ReceiptJournal = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
        private static readonly Guid ActionJournal = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005");

        private static string State(string previous) =>
            "{\"FR\":{\"Totals\":{\"Totalizer\":24.00,\"CINormal\":24.00,\"CIDiscounted1\":0,\"CIDiscounted2\":0,\"CISuperReduced1\":0,\"CIZero\":0,\"CIUnknown\":0,\"PICash\":24.00,\"PINonCash\":0,\"PIInternal\":0,\"PIUnknown\":0}," +
            "\"Archive\":{\"PreviousArchiveQueueItemId\":" + previous + ",\"FirstContainedReceiptQueueItemId\":\"" + First + "\",\"FirstContainedReceiptMoment\":\"2026-03-01T08:00:00Z\"," +
            "\"LastContainedReceiptQueueItemId\":\"" + Last + "\",\"LastContainedReceiptMoment\":\"2026-03-01T10:00:01.456Z\"," +
            "\"LastReceiptJournalId\":\"" + ReceiptJournal + "\",\"LastActionJournalId\":\"" + ActionJournal + "\",\"LastJournalFRId\":null," +
            "\"Day\":{\"Totalizer\":12.00},\"Month\":{\"Totalizer\":24.00},\"Year\":{\"Totalizer\":24.00}}}}";

        private static readonly string[] AColumns =
        {
            "PreviousArchiveQueueItemID", "FirstContainedReceiptQueueItemID", "FirstContainedReceiptMoment", "LastContainedReceiptQueueItemID",
            "LastContainedReceiptMoment", "LastReceiptJournalID", "LastActionJournalID", "LastJournalFRID", "ATotalizer",
        };

        private static async Task<(string[] Header, string[] Row)> WriteA(string previous)
        {
            var entry = ArchiveEntry.Read(Last, QueueId, 7, Request(0x4652_2000_0000_3011), Response("ft7#A2", "h.p.a2", stateData: State(previous), previous: "a1"))!;
            var file = ArchiveV2Files.JournalFile(ArchiveV2Files.Journals.Single(j => j.Letter == "A"));
            using var writer = new StringWriter();
            await file.WriteAsync(new[] { entry }, writer, new CsvOptions(), CancellationToken.None);
            var lines = writer.ToString().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            return (lines[0].Split(';'), lines[1].Split(';'));
        }

        [Fact]
        public async Task JournalA_HasTheCommonColumnsThenTheArchiveReceiptColumns_AndNoClosingTotals()
        {
            var (header, _) = await WriteA("\"" + Previous + "\"");

            header.Skip(header.Length - AColumns.Length).Should().Equal(AColumns);
            header.Should().NotContain(new[] { "Totalizer", "CINormal", "PICash", "PerpetualTotalizer" }, "§1.4: the archive-period totals per class are not archived");
            header.Should().Contain(new[] { "ftQueueItemID", "PreviousSignature", "Signature" });
        }

        [Fact]
        public async Task JournalA_CarriesTheBoundariesTheReceiptSigned()
        {
            var (header, row) = await WriteA("\"" + Previous + "\"");
            string Cell(string column) => row[Array.IndexOf(header, column)];

            Cell("PreviousArchiveQueueItemID").Should().Be(Previous.ToString("D"));
            Cell("FirstContainedReceiptQueueItemID").Should().Be(First.ToString("D"));
            Cell("FirstContainedReceiptMoment").Should().Be("2026-03-01T08:00:00.0000000Z");
            Cell("LastContainedReceiptQueueItemID").Should().Be(Last.ToString("D"));
            Cell("LastContainedReceiptMoment").Should().Be("2026-03-01T10:00:01.4560000Z");
            Cell("LastReceiptJournalID").Should().Be(ReceiptJournal.ToString("D"));
            Cell("LastActionJournalID").Should().Be(ActionJournal.ToString("D"));
            Cell("LastJournalFRID").Should().BeEmpty("v2 writes no ftJournalFR");
            Cell("ATotalizer").Should().Be("24.00");
        }

        [Fact]
        public async Task JournalA_LeavesPreviousArchiveEmpty_OnTheQueuesFirstArchiveReceipt()
        {
            var (header, row) = await WriteA("null");

            row[Array.IndexOf(header, "PreviousArchiveQueueItemID")].Should().BeEmpty();
        }
    }
}
