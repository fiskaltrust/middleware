using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// The chain links (<c>StartOfChain</c>, <c>PreviousSignature</c>), <c>Siret</c> and <c>RequestHash</c> are what
    /// the SCU signed, read from the entry itself (archive-v2 §1.1, ADR 0001, ADR 0013), never rebuilt by replaying
    /// the queue (ADR 0012).
    /// </summary>
    public class ArchiveEntrySignedLinksTests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private const string SignedRequestField = "1:n4bQgYhMfWWaL+qgxVrQFaO/TxsrC4Is0V1sFbDwCgg=";

        /// <summary>The LNE / self-certified data set (25 fields), as <c>LneDataSetBuilder</c> lays it out.</summary>
        internal static string LneDataSet(Guid queueItemId, string identification, long counter, string? previous, string siret = "12345678901234", string requestField = SignedRequestField, string serial = "SERIAL-1")
            => string.Join("|",
                siret, "RCS Paris", "", "", "", "",
                QueueId, queueItemId, requestField,
                "cashbox-fr", identification, "2026-03-01T10:00:01.4560000Z",
                "4652200000000001", "", "1", "EUR", "4.40", "13:4.40", "1:4.40", "", "",
                serial, counter, string.IsNullOrEmpty(previous) ? "N" : "O", previous ?? "");

        private static string Jws(string payloadJson, string signature)
            => "eyJhbGciOiJFUzI1NiJ9." + Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." + signature;

        private static ArchiveEntry? Read(string? request, string? response, long row = 1, Guid? id = null)
            => ArchiveEntry.Read(id ?? Guid.NewGuid(), QueueId, row, request, response);

        [Fact]
        public void Read_TakesTheLinksOfTheFirstV2EntryOfAHalfMigratedChain_FromTheSignedDataSet()
        {
            // ADR 0003: the first v2 entry of a chain continued from v1.2 carries the v1.2 signature it was chained on.
            var id = Guid.NewGuid();
            var dataSet = LneDataSet(id, "ft1#T42", 42, "v12-last-signature");

            var entry = Read(Request(), Response("ft1#T42", dataSet + ".t42"), id: id)!;

            entry.StartOfChain.Should().BeFalse();
            entry.PreviousSignature.Should().Be("v12-last-signature");
            entry.ChainCounter.Should().Be(42);
        }

        [Fact]
        public void Read_TakesSiretAndRequestHashFromTheirSignedFields()
        {
            var id = Guid.NewGuid();
            // A printed SIRET item that disagrees with the signed one: the column is what was signed (data-set field 1).
            var dataSet = LneDataSet(id, "ft1#T1", 1, null, siret: "98765432109876");

            var entry = Read(Request(), Response("ft1#T1", dataSet + ".t1"), id: id)!;

            entry.Siret.Should().Be("98765432109876");
            entry.RequestHash.Should().Be(SignedRequestField, "field 9, right after ftQueueItemID");
            entry.StartOfChain.Should().BeTrue();
            entry.PreviousSignature.Should().BeNull();
            entry.CertificateSerialNumber.Should().Be("SERIAL-1");
        }

        [Fact]
        public void Read_LeavesRequestHashEmpty_WhenTheSignedFieldIsEmpty()
        {
            var id = Guid.NewGuid();
            var entry = Read(Request(), Response("ft1#T1", LneDataSet(id, "ft1#T1", 1, null, requestField: "") + ".t1"), id: id)!;

            entry.RequestHash.Should().BeNull();
        }

        [Fact]
        public void Read_TakesTheLinksFromTheInfoCertPayload()
        {
            var qr = Jws("{\"siret\":\"55555555555555\",\"chainCounter\":7,\"previousSignatureCarriedOver\":\"O\",\"previousSignature\":\"t6-sig\",\"csn\":\"IC-1\"}", "t7-sig");

            var entry = Read(Request(), Response("ft1#T7", qr))!;

            entry.StartOfChain.Should().BeFalse();
            entry.PreviousSignature.Should().Be("t6-sig");
            entry.Siret.Should().Be("55555555555555");
            entry.CertificateSerialNumber.Should().Be("IC-1");
            entry.RequestHash.Should().BeNull("the InfoCert SCU signs no request hash");
            entry.ChainedSignature.Should().Be("t7-sig");
        }

        [Fact]
        public void Read_StartsTheChain_WhenTheInfoCertPayloadCarriesNoPreviousSignature()
        {
            var qr = Jws("{\"siret\":\"55555555555555\",\"chainCounter\":1,\"previousSignatureCarriedOver\":\"N\",\"previousSignature\":\"\",\"csn\":\"IC-1\"}", "t1-sig");

            var entry = Read(Request(), Response("ft1#T1", qr))!;

            entry.StartOfChain.Should().BeTrue();
            entry.PreviousSignature.Should().BeNull();
        }

        [Fact]
        public void Read_TakesTheSiretOfAnInfoCertJetFromThePrintedItem_SinceItsPayloadSignsNone()
        {
            var qr = Jws("{\"eventCode\":90,\"chainCounter\":3,\"previousSignatureCarriedOver\":\"O\",\"previousSignature\":\"l2\",\"csn\":\"IC-1\"}", "l3");

            var entry = Read(Request(0x4652_2000_0000_3000), Response("ft1#L3", qr))!;

            entry.PreviousSignature.Should().Be("l2");
            entry.Siret.Should().Be("12345678901234");
        }

        private static string B64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>The InfoCert JWS since market-fr#221: header with kid, the R13/R12/R19 string (or the data set) as payload.</summary>
        private static string StringJws(string signed, string signature, string? kid = "IC-2")
            => B64(kid is null ? "{\"alg\":\"ES256\",\"typ\":\"JWT\"}" : "{\"alg\":\"ES256\",\"typ\":\"JWT\",\"kid\":\"" + kid + "\"}") + "." + B64(signed) + "." + signature;

        [Fact]
        public void Read_TakesTheLinksFromAnR13TicketString_AndTheSerialFromTheHeaderKid()
        {
            var qr = StringJws("1000:440,440,20260301100001,ft1#T7,VENTE,O,t6-sig", "t7-sig");

            var entry = Read(Request(), Response("ft1#T7", qr))!;

            entry.ChainCounter.Should().Be(7, "the counter is the one of TAG-TIK-NUM, the receipt identification");
            entry.StartOfChain.Should().BeFalse();
            entry.PreviousSignature.Should().Be("t6-sig");
            entry.CertificateSerialNumber.Should().Be("IC-2");
            entry.Siret.Should().Be("12345678901234", "the string signs no SIRET: the printed item gives it");
            entry.Issuer.Rcs.Should().BeNull("R13 signs no issuer claim");
            entry.SignedByInfoCert.Should().BeTrue();
            entry.ChainedSignature.Should().Be("t7-sig");
            ArchiveEntry.CertificateSerialNumberOf(qr).Should().Be("IC-2");
        }

        [Theory]
        [InlineData(0x4652_2000_0000_0002, "ft1#P3", "ft1#P3,440,20260301100001,ticket-1,O,p2")]
        [InlineData(0x4652_2000_0000_1001, "ft1#I3", "1000:440,440,20260301100001,ft1#I3,FACTURE,Dupont,75008,,O,p2")]
        [InlineData(0x4652_2000_0000_3010, "ft1#D3", "ft1#D3,NOTE,1,Ana,20260301100001,ft1#B1,perte,O,p2")]
        [InlineData(0x4652_2000_0000_3010, "ft1#D3", "ft1#D3,TICKET,1,Ana,20260301100001,ft1#T1,O,p2")]
        [InlineData(0x4652_2000_0000_2011, "ft1#Z3", "1000:440,440,9000,20260301100001,ft1#Z3:20260228100000-20260301100001,O,p2")]
        [InlineData(0x4652_2000_0000_3001, "ft1#L3", "ft1#L3,20,info,20260301100001,Ana,cashbox-fr,O,p2")]
        [InlineData(0x4652_2000_0002_0001, "ft1#X3", "1000:440,440,20260301100001,ft1#X3,FORMATION-VENTE,O,p2")]
        public void Read_TakesTheCounterFromTheIdentificationFieldOfEveryString(long receiptCase, string identification, string signed)
        {
            var entry = Read(Request(receiptCase), Response(identification, StringJws(signed, "s3")))!;

            entry.ChainCounter.Should().Be(3);
            entry.PreviousSignature.Should().Be("p2");
        }

        [Fact]
        public void Read_TakesTheLinksOfAnArchiveReceiptString()
        {
            var values = new List<string> { "ft1#A2", "", Guid.NewGuid().ToString("D"), "20260101000000", Guid.NewGuid().ToString("D"), "20260301100001", "", "", "" };
            values.AddRange(Enumerable.Repeat("", 18));
            values.AddRange(new[] { "N", "" });

            var entry = Read(Request(0x4652_2000_0000_3011), Response("ft1#A2", StringJws(string.Join(",", values), "a2")))!;

            entry.ChainCounter.Should().Be(2);
            entry.StartOfChain.Should().BeTrue();
        }

        [Fact]
        public void Read_TakesTheDataSetOfAChainEEntry_FromTheJwsPayload()
        {
            var id = Guid.NewGuid();
            var qr = StringJws(LneDataSet(id, "ft1#E4", 4, "e3", serial: "IC-2"), "e4");

            var entry = Read(Request(0x4652_2000_0000_2000), Response("ft1#E4", qr), id: id)!;

            entry.ChainCounter.Should().Be(4);
            entry.PreviousSignature.Should().Be("e3");
            entry.RequestHash.Should().Be(SignedRequestField);
            entry.Issuer.Rcs.Should().Be("RCS Paris");
            entry.CertificateSerialNumber.Should().Be("IC-2");
            entry.SignedByInfoCert.Should().BeTrue();
        }

        [Fact]
        public void Read_KeepsReadingTheJsonPayloadOfEarlierInfoCertEntries_AndPrefersAKid()
        {
            var qr = B64("{\"alg\":\"ES256\",\"typ\":\"JWT\",\"kid\":\"IC-9\"}") + "." + B64("{\"chainCounter\":7,\"previousSignatureCarriedOver\":\"O\",\"previousSignature\":\"t6\",\"csn\":\"IC-1\"}") + ".t7";

            Read(Request(), Response("ft1#T7", qr))!.CertificateSerialNumber.Should().Be("IC-9");
        }

        public static IEnumerable<object[]> UnreadableSignedEntries()
        {
            yield return new object[] { "an R13 string signed for another identification", Request(), Response("ft1#T7", StringJws("1000:440,440,20260301100001,ft1#T8,VENTE,O,t6", "t7")) };
            yield return new object[] { "an R13 ticket string with a missing field", Request(), Response("ft1#T7", StringJws("440,20260301100001,ft1#T7,VENTE,O,t6", "t7")) };
            yield return new object[] { "an R13 string whose flag is neither O nor N", Request(), Response("ft1#T7", StringJws("1000:440,440,20260301100001,ft1#T7,VENTE,X,t6", "t7")) };
            var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
            yield return new object[] { "a response that is not JSON", Request(), "{\"ftSignatures\":[" };
            yield return new object[] { "a request that is not JSON", "not json", Response("ft1#T1", LneDataSet(id, "ft1#T1", 1, null) + ".t1") };
            yield return new object[] { "an identification without a chain", Request(), Response("ft1#", LneDataSet(id, "ft1#", 1, null) + ".t1") };
            yield return new object[] { "a receipt moment that is no date", Request(), Response("ft1#T1", LneDataSet(id, "ft1#T1", 1, null) + ".t1", moment: "yesterday") };
            yield return new object[] { "a data set without the chain fields", Request(), Response("ft1#T1", "12345678901234|x.t1") };
            yield return new object[] { "a start-of-chain flag that is neither O nor N", Request(), Response("ft1#T1", LneDataSet(id, "ft1#T1", 1, null).Replace("|1|N|", "|1|0|") + ".t1") };
            yield return new object[] { "a flag O without a previous signature", Request(), Response("ft1#T2", LneDataSet(id, "ft1#T2", 2, null).Replace("|2|N|", "|2|O|") + ".t2") };
            yield return new object[] { "a signed counter that is not the identification's", Request(), Response("ft1#T3", LneDataSet(id, "ft1#T3", 2, "t1") + ".t3") };
            yield return new object[] { "a JWS whose payload is not JSON", Request(), Response("ft1#T1", "eyJhbGciOiJFUzI1NiJ9.bm90IGpzb24.t1") };
            yield return new object[] { "a JWS payload without the chain claims", Request(), Response("ft1#T1", Jws("{\"csn\":\"IC-1\"}", "t1")) };
        }

        [Theory]
        [MemberData(nameof(UnreadableSignedEntries))]
        public void Read_FailsOnASignedEntryItCannotRead(string because, string request, string response)
        {
            var act = () => ArchiveEntry.Read(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), QueueId, 17, request, response);

            act.Should().Throw<ArchiveEntryException>(because).Which.QueueRow.Should().Be(17);
        }

        [Fact]
        public void Read_SkipsWhatIsNoSignedV2Entry()
        {
            Read(Request(), Response("ft1#T1", "s", state: 0x4652_2000_EEEE_EEEE)).Should().BeNull("an error response is no chain entry");
            Read(Request(), Response("ft1#T1", "s", signatureFormatNibble: 0)).Should().BeNull("v1 signature types have no v2 format nibble");
            Read(Request(), null).Should().BeNull("no response yet");
        }

        [Fact]
        public async Task Spool_GivesBackTheEntriesItStored_WithTheirContent()
        {
            using var spool = new ArchiveEntrySpool();
            var stored = new List<ArchiveEntry>();
            for (var row = 1; row <= 3; row++)
            {
                var id = Guid.NewGuid();
                stored.Add(spool.Read(id, QueueId, row, Request(), Response($"ft{row}#T{row}", LneDataSet(id, $"ft{row}#T{row}", row, row == 1 ? null : $"t{row - 1}") + $".t{row}"), "1.3.0")!);
            }

            stored.Select(e => e.PreviousSignature).Should().Equal(null, "t1", "t2");
            stored[2].RawRequest.Should().Be(Request());
            stored[0].Request.GetProperty("cbReceiptReference").GetString().Should().Be("R-1");
            stored[1].Response.GetProperty("ftReceiptIdentification").GetString().Should().Be("ft2#T2");
            stored[0].ProcessingVersion.Should().Be("1.3.0");

            var csv = await WriteJournal(stored);
            csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(4);
        }

        [Fact]
        public async Task Spool_HoldsALargePeriodOnDisk_NotInMemory()
        {
            // 2 000 entries of ~100 KB each: about 400 MB as .NET strings if they were kept in memory (#864 review).
            const int count = 2_000;
            var padding = ",\"cbCustomer\":{\"Note\":\"" + new string('x', 100_000) + "\"}";
            var request = Request(extra: padding);
            using var spool = new ArchiveEntrySpool();
            var entries = new List<ArchiveEntry>(count);

            var before = GC.GetTotalMemory(forceFullCollection: true);
            for (var row = 1; row <= count; row++)
            {
                var id = Guid.NewGuid();
                entries.Add(spool.Read(id, QueueId, row, request, Response($"ft{row}#T{row}", LneDataSet(id, $"ft{row}#T{row}", row, row == 1 ? null : $"t{row - 1}") + $".t{row}"))!);
            }
            var held = GC.GetTotalMemory(forceFullCollection: true) - before;

            held.Should().BeLessThan(32L * 1024 * 1024, "the entries' content stays in the spool file");
            var csv = await WriteJournal(entries);
            csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(count + 1);
            GC.KeepAlive(entries);
        }

        private static async Task<string> WriteJournal(IReadOnlyList<ArchiveEntry> entries)
        {
            var file = ArchiveV2Files.JournalFile(ArchiveV2Files.Journals.Single(j => j.Letter == "T"));
            using var writer = new StringWriter();
            await file.WriteAsync(entries, writer, new CsvOptions(), CancellationToken.None);
            return writer.ToString();
        }
    }
}
