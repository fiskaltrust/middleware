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

        internal static string Response(string identification, string signature, string serial = "SERIAL-1", long state = 0x4652_2000_0000_0000, long signatureFormatNibble = 2, string extraItems = "", string moment = "2026-03-01T10:00:01.4560000Z", string stateData = "", string? previous = null) =>
            "{" + (stateData.Length > 0 ? "\"ftStateData\":" + stateData + "," : "") + "\"ftQueueID\":\"" + QueueId + "\",\"ftCashBoxIdentification\":\"cashbox-fr\",\"ftReceiptIdentification\":\"" + identification + "\"," +
            "\"ftReceiptMoment\":\"" + moment + "\",\"ftState\":" + state + ",\"ftSignatures\":[" +
            Item(0x0001, Qr(signature, serial, identification, previous), signatureFormatNibble) + "," + Item(0x0009, "12345678901234", signatureFormatNibble) + extraItems + "]}";

        /// <summary>
        /// The receipt QR code for a test signature, signing what an SCU signs (ADR 0001): the serial where v1.2
        /// carried it, the chain counter of <paramref name="identification"/>, the start-of-chain flag and
        /// <paramref name="previous"/>. A fake JWS <c>h.p.sig</c> gets a payload with those claims, a bare signature
        /// a minimal data set ending with them. A data set given in full (it holds '|') or a JWS with a real header is kept as is.
        /// </summary>
        internal static string Qr(string signature, string serial, string identification = "ft1#T1", string? previous = null)
        {
            if (signature.Contains('|') || (signature.Split('.').Length == 3 && !signature.StartsWith("h.")))
            {
                return signature;
            }
            ArchiveEntry.TryReadChain(identification, out _, out var counter);
            var flag = string.IsNullOrEmpty(previous) ? "N" : "O";
            var dot = signature.LastIndexOf('.');
            if (dot < 0)
            {
                return $"12345678901234|{serial}|{counter}|{flag}|{previous}.{signature}";
            }
            var claims = "{\"siret\":\"12345678901234\",\"csn\":\"" + serial + "\",\"chainCounter\":" + counter + ",\"previousSignatureCarriedOver\":\"" + flag + "\",\"previousSignature\":\"" + previous + "\"}";
            var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(claims)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"h.{payload}.{signature.Substring(dot + 1)}";
        }

        private static string Item(long type, string data, long nibble) =>
            "{\"ftSignatureFormat\":1,\"ftSignatureType\":" + (0x4652_0000_0000_0000L | (nibble << 44) | type) + ",\"Caption\":\"x\",\"Data\":\"" + data + "\"}";

        internal static ArchiveEntry Entry(string identification, string signature, long row, string serial = "SERIAL-1", long receiptCase = 0x4652_2000_0000_0001, string moment = "2026-03-01T10:00:01.4560000Z", string? previous = null)
            => ArchiveEntry.Read(Guid.NewGuid(), QueueId, row, Request(receiptCase), Response(identification, signature, serial, moment: moment, previous: previous))!;

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
        public void Read_ReadsASignedV2Entry()
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
        public async Task JournalFile_HoldsTheJournalsChainsWithTheCommonColumns()
        {
            var t1 = Entry("ft1#T1", "h.p.t1", 1);
            var z1 = Entry("ft2#Z1", "z1", 2);
            var e1 = Entry("ft3#E1", "e1", 3);

            var tickets = await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x001)!), t1, z1, e1);
            var technical = await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x004)!), t1, z1, e1);
            var daily = await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x00D)!), t1, z1, e1);

            var lines = tickets.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(2);
            lines[0].Should().Be("ftQueueItemID;ftQueueID;ftCashBoxIdentification;ftQueueRow;Chain;ftReceiptIdentification;ChainCounter;StartOfChain;PreviousSignature;ftReceiptMoment;cbReceiptMoment;cbReceiptReference;cbPreviousReceiptReference;ftReceiptCase;cbTerminalID;cbUser;cbArea;ftState;RequestHash;Siret;CertificateSerialNumber;SoftwareVersion;Training;Signature;Totalizer;CINormal;CIDiscounted1;CIDiscounted2;CISuperReduced1;CIZero;CIUnknown;PICash;PINonCash;PIInternal;PIUnknown");
            lines[1].Should().Be($"{t1.QueueItemId};{QueueId};cashbox-fr;1;T;ft1#T1;1;true;;2026-03-01T10:00:01.4560000Z;2026-03-01T10:00:00.1230000Z;R-1;;0x4652200000000001;T1;\"{{\"\"Name\"\":\"\"Ana\"\"}}\";;0x4652200000000000;;12345678901234;SERIAL-1;;false;{Qr("h.p.t1", "SERIAL-1", "ft1#T1")};;;;;;;;;;;");
            technical.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Split(';')[4])
                .Should().Equal(new[] { "E" }, "each ADR 0002 chain has its own journal");
            daily.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Split(';')[4])
                .Should().Equal(new[] { "Z" });
        }

        [Fact]
        public async Task ChargeAndPayItems_KeepTheNumbersAsSent()
        {
            var t1 = Entry("ft1#T1", "t1", 1);
            var files = ArchiveV2Files.Files();

            var chargeItems = await Write(files.Single(f => f.FileName == ArchiveV2Files.ChargeItemsFileName), t1);
            var payItems = await Write(files.Single(f => f.FileName == ArchiveV2Files.PayItemsFileName), t1);

            chargeItems.Should().Contain($"{t1.QueueItemId};ft1#T1;1;;\"Café; crème\";2;;;;4.40;;;true;10;;0x4652200000000013;false;false;false;;;D1;;\r\n");
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
        public async Task CaseData_KeepsTheBoundariesOfPreviousReceiptReferences()
        {
            // The journal column joins with commas, so ["a,b","c"] and ["a","b,c"] read the same there.
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, Request(extra: ",\"cbPreviousReceiptReference\":[\"a,b\",\"c\"]"), Response("ft1#T1", "t1"))!;

            var caseData = await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.CaseDataFileName), entry);

            var rows = caseData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => string.Join(";", l.Split(';').Skip(2))).ToList();
            rows.Should().Contain(new[]
            {
                "Receipt;;cbPreviousReceiptReference;;array;",
                "Receipt;;cbPreviousReceiptReference;/0;string;a,b",
                "Receipt;;cbPreviousReceiptReference;/1;string;c",
            });
        }

        [Fact]
        public void ChainAnchors_HaveOneEntryPerJournalWithRows_WithItsFirstRowAndPreviousSignatureColumn()
        {
            var entries = new[] { Entry("ft1#T1", "t1", 1), Entry("ft1#T2", "t2", 2, previous: "t1"), Entry("ft1#I1", "i1", 3, receiptCase: 0x4652_2000_0000_1001) };
            var file = ArchiveSplitter.Split(entries.Skip(1).ToList(), e => e.ReceiptMoment, ArchiveV2Files.KeyOf, new ArchivePeriod(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1))).Single();

            var anchors = ArchiveV2Files.ChainAnchorsOf(file);

            anchors.Select(a => a.Chain).Should().Equal("T", "I");
            anchors[0].FirstReceiptIdentification.Should().Be("ft1#T2");
            anchors[0].PreviousSignature.Should().Be(entries[1].PreviousSignature).And.NotBeEmpty("T2 covers T1, archived in an earlier file");
            anchors[1].PreviousSignature.Should().BeEmpty("I1 starts its chain");
        }

        [Fact]
        public async Task Package_WritesEveryV2FileIntoOneStandaloneFileAndVerifies()
        {
            var entries = new[] { Entry("ft1#T1", "t1", 1), Entry("ft2#T2", "t2", 2, serial: "SERIAL-2", moment: "2026-06-01T10:00:00.0000000Z", previous: "t1") };
            var files = ArchiveSplitter.Split(entries, e => e.ReceiptMoment, ArchiveV2Files.KeyOf, new ArchivePeriod(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)));
            using var certificate = TestFixtures.EcdsaCertificate();
            using var output = new MemoryStream();

            files.Select(f => f.Key.CertificateSerialNumber).Should().Equal(new[] { "SERIAL-1", "SERIAL-2" }, "an SCU switch ends the file (ADR 0019)");
            await ArchiveV2Files.CreatePackager(_ => new X509ArchiveSigner(certificate)).WriteAsync(files[0], new ArchiveMetadata("12345678901234", DateTime.UtcNow), output);

            var result = ArchiveVerifier.Verify(new MemoryStream(output.ToArray()));
            result.Errors.Should().BeEmpty();
            result.Manifest!.Schema.Should().Be("LNE", "every manifest says how to read Signature, whoever signs");
            result.Manifest.ChainAnchors.Should().ContainSingle(a => a.Chain == "T");
            result.Manifest.FirstRecord!.ReceiptIdentification.Should().Be("ft1#T1");
            using var zip = new ZipArchive(new MemoryStream(output.ToArray()));
            zip.Entries.Select(e => e.Name).Should().Contain(ArchiveV2Files.Journals.Select(j => j.FileName)
                .Concat(new[]
                {
                    ArchiveV2Files.ChargeItemsFileName, ArchiveV2Files.PayItemsFileName, ArchiveV2Files.VatSummaryFileName, ArchiveV2Files.SignaturesFileName,
                    ArchiveV2Files.JetFileName, ArchiveV2Files.CaseDataFileName, ArchiveV2Files.ComplianceParametersFileName, ArchiveReadmeFile.ReadmeFileName,
                }));
            zip.Entries.Select(e => e.Name).Should().NotContain("journal-types.csv", "the journal names are in the file names and README.txt (decided 2026-11-06)");
            zip.Entries.Should().NotContain(e => e.Name.EndsWith(".zip"));
            using var reader = new StreamReader(zip.GetEntry(ArchiveReadmeFile.ReadmeFileName)!.Open(), Encoding.UTF8);
            var readme = await reader.ReadToEndAsync();
            readme.Should().Contain("== 4.1 Colonnes communes à tous les journaux ==").And.Contain("== 4.2 journal-T-ticket.csv ==");
            readme.Should().Contain("archive_<numéro de série>").And.NotContain("subarchive_").And.NotContain("scu_*.cer");
        }

        private const string StoredState = "{\"FR\":{\"Totals\":{\"Totalizer\":4.40,\"CINormal\":0,\"CIDiscounted1\":0,\"CIDiscounted2\":4.40,\"CISuperReduced1\":0,\"CIZero\":0,\"CIUnknown\":0,\"PICash\":4.40,\"PINonCash\":0,\"PIInternal\":0,\"PIUnknown\":0}," +
            "\"ChargeItems\":[{\"Position\":1,\"VATAmount\":0.40,\"AmountHT\":4.00,\"UnitPriceHT\":2.00,\"VATNature\":\"00\"}]," +
            "\"VATSummary\":{\"10\":{\"AmountHT\":4.00,\"VATAmount\":0.40,\"AmountTTC\":4.40,\"VATNature\":\"00\"}}}}";

        [Fact]
        public async Task SalesJournalAndChargeItems_ReadTheValuesTheQueueStored()
        {
            var t1 = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, Request(), Response("ft1#T1", "h.p.t1", stateData: StoredState), "1.3.80+abc")!;

            var journal = Rows(await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x001)!), t1));
            var lines = Rows(await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.ChargeItemsFileName), t1));
            var vat = Rows(await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.VatSummaryFileName), t1));

            journal[0]["SoftwareVersion"].Should().Be("1.3.80+abc");
            journal[0]["Totalizer"].Should().Be("4.40");
            journal[0]["CIDiscounted2"].Should().Be("4.40");
            journal[0]["PICash"].Should().Be("4.40");
            lines[0]["Amount"].Should().Be("4.40");
            lines[0]["AmountHT"].Should().Be("4.00");
            lines[0]["VATAmount"].Should().Be("0.40");
            lines[0]["UnitPriceHT"].Should().Be("2.00");
            lines[0]["PricesIncludeVAT"].Should().Be("true");
            lines[0]["VATNature"].Should().Be("00");
            vat.Should().ContainSingle();
            vat[0]["VATRate"].Should().Be("10");
            vat[0]["AmountHT"].Should().Be("4.00");
            vat[0]["AmountTTC"].Should().Be("4.40");
        }

        [Fact]
        public async Task ClosingJournal_ReadsThePeriodAndPerpetualTotals()
        {
            var state = "{\"FR\":{\"Totals\":{\"Totalizer\":10,\"PICash\":10,\"Perpetual\":{\"Totalizer\":250,\"PICash\":200,\"PINonCash\":50}}}}";
            var z1 = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, Request(0x4652_2000_0000_2011), Response("ft1#Z1", "z1", stateData: state))!;

            var rows = Rows(await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x00D)!), z1));

            rows[0]["Totalizer"].Should().Be("10");
            rows[0]["PerpetualTotalizer"].Should().Be("250");
            rows[0]["PerpetualPINonCash"].Should().Be("50");
            rows[0]["PerpetualCINormal"].Should().BeEmpty("a value the queue did not store stays empty");
        }

        [Fact]
        public async Task ClosingJournals_ReadTheClosingPeriod()
        {
            var state = "{\"FR\":{\"Closing\":{\"ClosingType\":\"daily\",\"PeriodStart\":\"2026-11-01T22:00:00.1230000Z\",\"PeriodEnd\":\"2026-11-02T22:00:00.4560000Z\"},\"Totals\":{\"Totalizer\":10}}}";
            var z1 = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, Request(0x4652_2000_0000_2011), Response("ft1#Z1", "z1", stateData: state))!;
            var e1 = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 2, Request(0x4652_2000_0000_2000), Response("ft2#E1", "e1", stateData: "{\"FR\":{\"Closing\":{\"ClosingType\":\"zero\"}}}"))!;

            var daily = Rows(await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x00D)!), z1));
            var technical = Rows(await Write(ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(0x004)!), e1));

            daily[0]["ClosingType"].Should().Be("daily");
            daily[0]["PeriodStart"].Should().Be("2026-11-01T22:00:00.1230000Z");
            daily[0]["PeriodEnd"].Should().Be("2026-11-02T22:00:00.4560000Z");
            technical[0]["ClosingType"].Should().Be("zero");
            technical[0]["PeriodStart"].Should().BeEmpty("a zero receipt closes no period");
        }

        [Fact]
        public async Task SignaturesAndJet_HaveOneRowPerItemAndPerJetEvent()
        {
            var jetRequest = "{\"ftReceiptCase\":" + 0x4652_2000_0000_3001L + ",\"cbUser\":\"op1\",\"ftReceiptCaseData\":{\"fr\":{\"code\":50,\"Message\":\"Clôture\",\"Information\":\"jour\"}}}";
            var l1 = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, jetRequest, Response("ft1#L1", "l1"))!;
            var t1 = Entry("ft2#T1", "h.p.t1", 2);

            var signatures = Rows(await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.SignaturesFileName), l1, t1));
            var jet = Rows(await Write(ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.JetFileName), l1, t1));

            var itemsPerResponse = Response("ft1#L1", "l1").Split(new[] { "\"ftSignatureType\"" }, StringSplitOptions.None).Length - 1;
            signatures.Should().HaveCount(2 * itemsPerResponse, "one row per signature item of each response");
            signatures[0]["Index"].Should().Be("0");
            signatures[0]["Data"].Should().EndWith("l1");
            jet.Should().ContainSingle("only the 0x3001 event on L is a JET row");
            jet[0]["EventCode"].Should().Be("50");
            jet[0]["EventLabel"].Should().Be("Clôture");
            jet[0]["Information"].Should().Be("jour");
            jet[0]["OperatorID"].Should().Be("op1");
        }

        [Fact]
        public async Task ComplianceParameters_AreWrittenAsGiven()
        {
            var context = new ArchiveV2Context { ComplianceParameters = new[] { new ComplianceParameter("SCU", "Siret", "12345678901234"), new ComplianceParameter("SCU", "Rcs", null) } };

            var csv = await Write(ArchiveV2Files.Files(context).Single(f => f.FileName == ArchiveV2Files.ComplianceParametersFileName));

            csv.Should().Be("Component;Parameter;Value\r\nSCU;Siret;12345678901234\r\nSCU;Rcs;\r\n");
        }

        [Fact]
        public void Readme_DescribesEveryFileAndColumnInFrench_InShortLines()
        {
            var files = ArchiveV2Files.Files();
            var readme = files.OfType<ArchiveReadmeFile>().Single().Build();
            var lines = readme.Split(new[] { "\r\n" }, StringSplitOptions.None);

            readme.Should().StartWith("README - Archive fiscale fiskaltrust").And.Contain("Sommaire").And.Contain("== 6. Contrôle d'intégrité ==");
            lines.Where(l => l.Length > 100).Should().BeEmpty("the README is read in a plain text viewer");
            foreach (var file in files.OfType<IDocumentedFile>())
            {
                readme.Should().Contain($" {file.FileName} ==");
                foreach (var (name, description) in file.ColumnDocs)
                {
                    description.Should().NotBeNullOrWhiteSpace($"{file.FileName}:{name} needs a description");
                    readme.Should().Contain($"  {name} : ");
                }
            }
            readme.Should().NotContain("journal-types.csv");
        }

        private static List<Dictionary<string, string>> Rows(string csv)
        {
            var lines = csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var header = lines[0].Split(';');
            return lines.Skip(1)
                .Select(SplitCsv)
                .Select(cells => header.Select((h, i) => (h, v: i < cells.Count ? cells[i] : "")).ToDictionary(x => x.h, x => x.v))
                .ToList();
        }

        private static List<string> SplitCsv(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ';')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            cells.Add(current.ToString());
            return cells;
        }
    }
}
