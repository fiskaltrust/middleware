using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    public class ArchiveV2FilesTests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        internal static string Request(long receiptCase = 0x4652_2000_0000_0001, string extra = "") =>
            "{\"ftCashBoxID\":\"00000000-0000-0000-0000-000000000001\",\"ftReceiptCase\":" + receiptCase +
            ",\"cbReceiptReference\":\"R-1\",\"cbReceiptMoment\":\"2026-03-01T10:00:00.123Z\",\"cbTerminalID\":\"T1\"," +
            "\"cbUser\":{\"Name\":\"Ana\"}," +
            "\"cbChargeItems\":[{\"Position\":1,\"Description\":\"Café; crème\",\"Quantity\":2,\"Amount\":4.40,\"VATRate\":10,\"ftChargeItemCase\":" + 0x4652_2000_0000_0013L + "," +
            "\"ftChargeItemCaseData\":{\"FR\":{\"DiscountCode\":\"D1\",\"a/b\":[1,null]}}}]," +
            "\"cbPayItems\":[{\"Position\":1,\"Description\":\"Cash\",\"Amount\":4.40,\"ftPayItemCase\":" + (0x4652_2000_0008_0001L) + "}]" + extra + "}";

        internal static string Response(string identification, string signature, string serial = "SERIAL-1", long state = 0x4652_2000_0000_0000, long signatureFormatNibble = 2) =>
            "{\"ftQueueID\":\"" + QueueId + "\",\"ftCashBoxIdentification\":\"cashbox-fr\",\"ftReceiptIdentification\":\"" + identification + "\"," +
            "\"ftReceiptMoment\":\"2026-03-01T10:00:01.4560000Z\",\"ftState\":" + state + ",\"ftSignatures\":[" +
            Item(0x0001, signature, signatureFormatNibble) + "," + Item(0x0002, serial, signatureFormatNibble) + "," + Item(0x0009, "12345678901234", signatureFormatNibble) + "]}";

        private static string Item(long type, string data, long nibble) =>
            "{\"ftSignatureFormat\":1,\"ftSignatureType\":" + (0x4652_0000_0000_0000L | (nibble << 44) | type) + ",\"Caption\":\"x\",\"Data\":\"" + data + "\"}";

        internal static ArchiveEntry Entry(string identification, string signature, long row, string serial = "SERIAL-1", long receiptCase = 0x4652_2000_0000_0001)
            => ArchiveEntry.TryCreate(Guid.NewGuid(), QueueId, row, Request(receiptCase), Response(identification, signature, serial), "aGFzaA==")!;

        private static async Task<string> Write(ICsvFileDefinition<ArchiveEntry> file, params ArchiveEntry[] entries)
        {
            using var writer = new StringWriter();
            await file.WriteAsync(entries, writer, new CsvOptions(), default);
            return writer.ToString();
        }

        [Theory]
        [InlineData("ft1A#T42", "T", 42)]
        [InlineData("ft3#E1", "E", 1)]
        [InlineData("Z7", "Z", 7)]
        public void TryReadChain_ReadsTheTrailingLetterAndCounter(string identification, string chain, long counter)
        {
            ArchiveEntry.TryReadChain(identification, out var readChain, out var readCounter).Should().BeTrue();
            readChain.Should().Be(chain);
            readCounter.Should().Be(counter);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ft1A#")]
        [InlineData("ft1A#42")]
        public void TryReadChain_RejectsAnIdentificationWithoutChain(string? identification)
            => ArchiveEntry.TryReadChain(identification, out _, out _).Should().BeFalse();

        [Fact]
        public void TryCreate_ReadsASignedV2Entry()
        {
            var entry = Entry("ft1#T3", "h.p.sig3", 5);

            entry.Chain.Should().Be("T");
            entry.ChainCounter.Should().Be(3);
            entry.ChainedSignature.Should().Be("sig3");
            entry.CertificateSerialNumber.Should().Be("SERIAL-1");
            entry.Siret.Should().Be("12345678901234");
            entry.ReceiptMoment.Should().Be(new DateTime(2026, 3, 1, 10, 0, 1, 456, DateTimeKind.Utc));
            entry.Training.Should().BeFalse();
        }

        [Fact]
        public void TryCreate_SkipsErrorResponsesV1EntriesAndUnsignedOnes()
        {
            ArchiveEntry.TryCreate(Guid.NewGuid(), QueueId, 1, Request(), Response("ft1#T1", "s", state: 0x4652_2000_EEEE_EEEE), null).Should().BeNull("an error response is no chain entry");
            ArchiveEntry.TryCreate(Guid.NewGuid(), QueueId, 1, Request(), Response("ft1#T1", "s", signatureFormatNibble: 0), null).Should().BeNull("v1 signature types have no v2 format nibble");
            ArchiveEntry.TryCreate(Guid.NewGuid(), QueueId, 1, Request(), null, null).Should().BeNull("no response yet");
            ArchiveEntry.TryCreate(Guid.NewGuid(), QueueId, 1, Request(), "not json", null).Should().BeNull();
        }

        [Fact]
        public void Linker_ChainsEachChainOnItsOwnPreviousSignature()
        {
            var t1 = Entry("ft1#T1", "h.p.t1", 1);
            var e1 = Entry("ft2#E1", "e1", 2);
            var t2 = Entry("ft3#T2", "h.p.t2", 3);
            var linker = new ArchiveChainLinker();

            foreach (var entry in new[] { t1, e1, t2 })
            {
                linker.Link(entry);
            }

            t1.StartOfChain.Should().BeTrue();
            t1.PreviousSignature.Should().BeNull();
            e1.StartOfChain.Should().BeTrue();
            t2.StartOfChain.Should().BeFalse();
            t2.PreviousSignature.Should().Be("t1", "the JWS chains on its signature segment (ADR 0001)");
        }

        [Fact]
        public void Linker_LeavesThePreviousSignatureEmptyWhenThePreviousEntryWasNotSeen()
        {
            var t5 = Entry("ft9#T5", "t5", 9);

            new ArchiveChainLinker().Link(t5);

            t5.StartOfChain.Should().BeFalse();
            t5.PreviousSignature.Should().BeNull();
        }

        [Fact]
        public async Task JournalFile_HoldsTheJournalsChainsWithTheCommonColumns()
        {
            var t1 = Entry("ft1#T1", "h.p.t1", 1);
            var z1 = Entry("ft2#Z1", "z1", 2);
            var e1 = Entry("ft3#E1", "e1", 3);
            foreach (var entry in new[] { t1, z1, e1 })
            {
                new ArchiveChainLinker().Link(entry);
            }

            var tickets = await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x001)!), t1, z1, e1);
            var grandTotals = await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x004)!), t1, z1, e1);

            var lines = tickets.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(2);
            lines[0].Should().Be("ftQueueItemID;ftQueueID;ftCashBoxIdentification;ftQueueRow;Chain;ftReceiptIdentification;ChainCounter;StartOfChain;PreviousSignature;ftReceiptMoment;cbReceiptMoment;cbReceiptReference;cbPreviousReceiptReference;ftReceiptCase;cbTerminalID;cbUser;cbArea;ftState;RequestHash;Siret;CertificateSerialNumber;Training;Signature");
            lines[1].Should().Be($"{t1.QueueItemId};{QueueId};cashbox-fr;1;T;ft1#T1;1;true;;2026-03-01T10:00:01.4560000Z;2026-03-01T10:00:00.1230000Z;R-1;;0x4652200000000001;T1;\"{{\"\"Name\"\":\"\"Ana\"\"}}\";;0x4652200000000000;aGFzaA==;12345678901234;SERIAL-1;false;h.p.t1");
            grandTotals.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Split(';')[4])
                .Should().Equal(new[] { "Z", "E" }, "the closing and technical chains replace G (ADR 0002)");
        }

        [Fact]
        public async Task ChargeAndPayItems_KeepTheNumbersAsSent()
        {
            var t1 = Entry("ft1#T1", "t1", 1);
            var files = ArchiveV2Files.Files();

            var chargeItems = await Write(files.Single(f => f.FileName == ArchiveV2Files.ChargeItemsFileName), t1);
            var payItems = await Write(files.Single(f => f.FileName == ArchiveV2Files.PayItemsFileName), t1);

            chargeItems.Should().Contain($"{t1.QueueItemId};ft1#T1;1;;\"Café; crème\";2;;;4.40;10;0x4652200000000013;;D1;");
            payItems.Should().Contain($"{t1.QueueItemId};ft1#T1;1;0x4652200000080001;Cash;4.40;;2026-03-01T10:00:01.4560000Z;;;true");
        }

        [Fact]
        public async Task CaseData_WritesOneRowPerContainerAndScalarWithEscapedPointers()
        {
            var t1 = Entry("ft1#T1", "t1", 1);

            var caseData = await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.CaseDataFileName), t1);

            var rows = caseData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => string.Join(";", l.Split(';').Skip(2))).ToList();
            rows.Should().Equal(
                "Receipt;;cbUser;;object;",
                "Receipt;;cbUser;/Name;string;Ana",
                "ChargeItem;1;ftChargeItemCaseData;;object;",
                "ChargeItem;1;ftChargeItemCaseData;/FR;object;",
                "ChargeItem;1;ftChargeItemCaseData;/FR/DiscountCode;string;D1",
                "ChargeItem;1;ftChargeItemCaseData;/FR/a~1b;array;",
                "ChargeItem;1;ftChargeItemCaseData;/FR/a~1b/0;number;1",
                "ChargeItem;1;ftChargeItemCaseData;/FR/a~1b/1;null;");
        }

        [Fact]
        public async Task Package_WritesEveryV2FileAndVerifies()
        {
            var entries = new[] { Entry("ft1#T1", "t1", 1), Entry("ft2#T2", "t2", 2, serial: "SERIAL-2") };
            var linker = new ArchiveChainLinker();
            foreach (var entry in entries)
            {
                linker.Link(entry);
            }
            var archive = ArchiveSplitter.Split(entries, e => e.ReceiptMoment, ArchiveV2Files.KeyOf, new ArchivePeriod(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1))).Single();
            using var certificate = TestFixtures.EcdsaCertificate();
            using var output = new MemoryStream();

            await new ArchivePackager<ArchiveEntry>(new[] { ArchiveV2Files.CreateWriter() }, new X509ArchiveSigner(certificate))
                .WriteAsync(archive, new ArchiveMetadata("12345678901234", DateTime.UtcNow), output);

            output.Position = 0;
            ArchiveVerifier.Verify(new MemoryStream(output.ToArray())).Errors.Should().BeEmpty();
            using var zip = new ZipArchive(new MemoryStream(output.ToArray()));
            zip.Entries.Select(e => e.Name).Should().Contain(new[] { "subarchive_SERIAL_1_v2.zip", "subarchive_SERIAL_2_v2.zip" }, "an SCU switch splits the archive (ADR 0005)");
            using var sub = new ZipArchive(zip.GetEntry("subarchive_SERIAL_1_v2.zip")!.Open());
            sub.Entries.Select(e => e.Name).Should().Contain(ArchiveV2Files.Journals.Select(j => j.FileName)
                .Concat(new[] { ArchiveV2Files.ChargeItemsFileName, ArchiveV2Files.PayItemsFileName, ArchiveV2Files.JournalTypesFileName, ArchiveV2Files.CaseDataFileName }));
            using var reader = new StreamReader(sub.GetEntry(ArchiveV2Files.JournalTypesFileName)!.Open(), Encoding.UTF8);
            (await reader.ReadToEndAsync()).Should().Contain("00A;C;Copy\r\n").And.StartWith("Code;Chain;Description\r\n000;;Status information QueueFR\r\n001;T;Ticket");
        }
    }
}
