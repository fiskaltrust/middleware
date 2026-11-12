using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// The columns the archive-v2 contract promises on every journal (§1.1 issuer claims, §1.2 sales and customer
    /// columns) and on the charge items (§2 <c>CountsAsLine</c>, discounts on the line they apply to).
    /// </summary>
    public class ArchiveContractColumnsTests
    {
        private static readonly Guid QueueId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        private static async Task<List<Dictionary<string, string>>> Rows(ICsvFileDefinition<ArchiveEntry> file, params ArchiveEntry[] entries)
        {
            using var writer = new StringWriter();
            await file.WriteAsync(entries, writer, new CsvOptions(), default);
            var lines = writer.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var header = Split(lines[0]);
            return lines.Skip(1).Select(l => header.Zip(Split(l)).ToDictionary(p => p.First, p => p.Second)).ToList();
        }

        private static string[] Split(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else { quoted = !quoted; }
                }
                else if (c == ';' && !quoted) { fields.Add(current.ToString()); current.Clear(); }
                else { current.Append(c); }
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private static ICsvFileDefinition<ArchiveEntry> Journal(int code) => ArchiveV2Files.JournalFile(ArchiveV2Files.JournalOf(code)!);

        private static ICsvFileDefinition<ArchiveEntry> ChargeItems() => ArchiveV2Files.Files().Single(f => f.FileName == ArchiveV2Files.ChargeItemsFileName);

        private static ArchiveEntry Ticket(string request, string stateData = "", string extraItems = "")
            => ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, request, ArchiveV2FilesTests.Response("ft1#T1", "t1", stateData: stateData, extraItems: extraItems))!;

        // §1.1 issuer claims, read from the signed content.

        [Fact]
        public async Task IssuerClaims_AreReadFromTheLneDataSet()
        {
            var id = Guid.NewGuid();
            var dataSet = $"12345678901234|RCS Paris B 1|62.01Z|SAS, au capital de 500000 EUR|debits|123456789|{QueueId}|{id}||cashbox-fr|ft1#T1|2026-03-01T10:00:01.4560000Z|4652200000000001||1|EUR|4.40|||||SERIAL-1|1|N|";
            var entry = ArchiveEntry.Read(id, QueueId, 1, ArchiveV2FilesTests.Request(), ArchiveV2FilesTests.Response("ft1#T1", dataSet + ".sig"))!;

            var row = (await Rows(Journal(0x001), entry))[0];

            row["Rcs"].Should().Be("RCS Paris B 1");
            row["Naf"].Should().Be("62.01Z");
            row["LegalForm"].Should().Be("SAS, au capital de 500000 EUR");
            row["VatOption"].Should().Be("debits");
            row["ElectronicAddress"].Should().Be("123456789");
        }

        [Fact]
        public async Task IssuerClaims_AreReadFromTheJwsPayload()
        {
            var claims = "{\"siret\":\"12345678901234\",\"rcs\":\"RCS Lyon B 2\",\"naf\":\"47.11F\",\"legalform\":\"SARL\",\"vatoption\":\"receipts\",\"csn\":\"SERIAL-1\",\"chainCounter\":1,\"previousSignatureCarriedOver\":\"N\",\"previousSignature\":\"\"}";
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(claims)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, ArchiveV2FilesTests.Request(), ArchiveV2FilesTests.Response("ft1#T1", $"eyJhbGciOiJFUzI1NiJ9.{payload}.sig"))!;

            var row = (await Rows(Journal(0x001), entry))[0];

            row["Rcs"].Should().Be("RCS Lyon B 2");
            row["Naf"].Should().Be("47.11F");
            row["LegalForm"].Should().Be("SARL");
            row["VatOption"].Should().Be("receipts");
            row["ElectronicAddress"].Should().BeEmpty("not configured, so not signed");
        }

        // §1.2 sales columns.

        [Fact]
        public async Task SalesColumns_ReadWhatTheQueueStored()
        {
            var state = "{\"FR\":{\"DocumentType\":\"ticket\",\"OperationType\":\"refund\",\"DiscountTotal\":-1.50,\"LineCount\":3,\"Totals\":{\"Totalizer\":10}}}";

            var row = (await Rows(Journal(0x001), Ticket(ArchiveV2FilesTests.Request(), state)))[0];

            row["DocumentType"].Should().Be("ticket");
            row["OperationType"].Should().Be("refund");
            row["DiscountTotal"].Should().Be("-1.50");
            row["LineCount"].Should().Be("3");
        }

        /// <summary>market-fr#221, decided 2026-11-12: TotalHT and PrintCount as the queue stored them at signing.</summary>
        [Fact]
        public async Task TotalHTAndPrintCount_ReadWhatTheQueueStored()
        {
            var state = "{\"FR\":{\"DocumentType\":\"ticket\",\"TotalHT\":1500.00,\"PrintCount\":1}}";

            var row = (await Rows(Journal(0x001), Ticket(ArchiveV2FilesTests.Request(), state)))[0];

            row["TotalHT"].Should().Be("1500.00");
            row["PrintCount"].Should().Be("1");
        }

        private static string WithGuests(string caseData) => ArchiveV2FilesTests.Request(extra: ",\"ftReceiptCaseData\":" + caseData);

        [Theory]
        [InlineData("ticket", "{\"FR\":{\"GuestCount\":4}}", "4")]
        [InlineData("note", "{\"FR\":{\"guestcount\":\"2\"}}", "2")]
        [InlineData("note", "\"{\\\"FR\\\":{\\\"GuestCount\\\":6}}\"", "6")]
        [InlineData("ticket", "{\"FR\":{\"ManagerId\":\"m\"}}", "")]
        [InlineData("invoice", "{\"FR\":{\"GuestCount\":4}}", "")]
        [InlineData("payment-proof", "{\"FR\":{\"GuestCount\":4}}", "")]
        public async Task GuestCount_IsArchivedAsSent_OnTicketsAndNotesOnly(string documentType, string caseData, string expected)
        {
            var entry = Ticket(WithGuests(caseData), $"{{\"FR\":{{\"DocumentType\":\"{documentType}\"}}}}");

            (await Rows(Journal(0x001), entry))[0]["GuestCount"].Should().Be(expected);
        }

        [Fact]
        public async Task TheTrainingChain_CarriesTheManagerAndTheMention()
        {
            var request = ArchiveV2FilesTests.Request(0x4652_2000_0002_0001, ",\"ftReceiptCaseData\":{\"FR\":{\"ManagerId\":\"manager-7\"}}");
            var mention = ",{\"ftSignatureFormat\":1,\"ftSignatureType\":" + 0x4652_2000_0000_0003L + ",\"Caption\":\"Mode école\",\"Data\":\"SIMULATION - NON VALABLE POUR ENCAISSEMENT\"}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, request, ArchiveV2FilesTests.Response("ft1#X1", "x1", extraItems: mention))!;

            var row = (await Rows(Journal(0x00B), entry))[0];

            row["ManagerId"].Should().Be("manager-7");
            row["TrainingMention"].Should().Be("SIMULATION - NON VALABLE POUR ENCAISSEMENT");
        }

        // §1.2 customer columns (D8).

        [Fact]
        public async Task CustomerColumns_ReadTheRequestsCustomer()
        {
            var customer = ",\"cbCustomer\":{\"CustomerName\":\"Client SARL\",\"CustomerId\":\"C-42\",\"CustomerStreet\":\"1 rue de la Paix\",\"CustomerZip\":\"75002\",\"CustomerCity\":\"Paris\"," +
                "\"CustomerCountrySubentity\":\"IDF\",\"CustomerCountry\":\"FR\",\"CustomerVATId\":\"FR40123456789\",\"CustomerTaxId\":\"123456789\",\"CustomerReference\":\"PO-9\"}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, ArchiveV2FilesTests.Request(0x4652_2000_0000_1002, customer),
                ArchiveV2FilesTests.Response("ft1#I1", "i1", stateData: "{\"FR\":{\"CustomerEndpointId\":\"0225:123456789\"}}"))!;

            var row = (await Rows(Journal(0x003), entry))[0];

            row["CustomerName"].Should().Be("Client SARL");
            row["CustomerId"].Should().Be("C-42");
            row["CustomerStreet"].Should().Be("1 rue de la Paix");
            row["CustomerZip"].Should().Be("75002");
            row["CustomerCity"].Should().Be("Paris");
            row["CustomerCountrySubentity"].Should().Be("IDF");
            row["CustomerCountry"].Should().Be("FR");
            row["CustomerVATId"].Should().Be("FR40123456789");
            row["CustomerTaxId"].Should().Be("123456789");
            row["CustomerEndpointId"].Should().Be("0225:123456789", "inferred by the queue when it signed");
            row["CustomerReference"].Should().Be("PO-9");
        }

        [Fact]
        public async Task ASentEndpoint_IsArchivedAsSent()
        {
            var customer = ",\"cbCustomer\":{\"CustomerName\":\"Client SARL\",\"CustomerEndpointId\":\"0225:987654321_X\"}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, ArchiveV2FilesTests.Request(0x4652_2000_0000_1002, customer), ArchiveV2FilesTests.Response("ft1#I1", "i1"))!;

            (await Rows(Journal(0x003), entry))[0]["CustomerEndpointId"].Should().Be("0225:987654321_X");
        }

        // archive-v2 §2 column order and the Conventions' decimals.

        [Fact]
        public void TheChargeItemColumns_FollowTheContractTable()
            => ArchiveV2Files.ChargeItemColumns.Select(c => c.Name).Should().Equal(
                "ftQueueItemID", "ftReceiptIdentification", "Position", "ProductNumber", "Description", "Quantity", "Unit", "UnitPrice", "UnitPriceHT",
                "Amount", "PricesIncludeVAT", "AmountHT", "VATRate", "VATAmount", "VATNature", "ftChargeItemCase", "Moment", "CountsAsLine",
                "DiscountCode", "DiscountRate", "DiscountAmount", "AppliesTo", "Downpayment", "ExtraOrDiscount", "ReturnableDeposit");

        [Fact]
        public async Task ANumberSentWithAnExponent_IsWrittenInPlainDecimals()
        {
            var request = "{\"ftReceiptCase\":" + 0x4652_2000_0000_0001L + ",\"cbChargeItems\":[{\"Position\":1,\"Description\":\"a\",\"Quantity\":1E-05,\"Amount\":1.5e2,\"VATRate\":20,\"ftChargeItemCase\":" + 0x4652_2000_0000_0013L + "}],\"cbPayItems\":[]}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, request, ArchiveV2FilesTests.Response("ft1#T1", "t1"))!;

            var row = (await Rows(ChargeItems(), entry))[0];

            row["Amount"].Should().Be("150");
            row["Quantity"].Should().Be("0.00001");
        }

        // §1.2 Copy chain (D).

        [Fact]
        public async Task TheCopyJournal_CarriesTheCopyColumns()
        {
            var request = "{\"ftReceiptCase\":" + 0x4652_2000_0000_3010L + ",\"cbPreviousReceiptReference\":\"R-7\",\"ftReceiptCaseData\":{\"FR\":{\"CopyReason\":\"ticket perdu\"}},\"cbChargeItems\":[],\"cbPayItems\":[]}";
            var state = "{\"FR\":{\"CopyNumber\":2,\"CopyType\":\"ticket\",\"CopiedReceiptIdentification\":\"ft1#T7\"}}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, request, ArchiveV2FilesTests.Response("ft1#D1", "d1", stateData: state))!;

            var row = (await Rows(Journal(0x00A), entry))[0];

            row["CopiedReceiptReference"].Should().Be("R-7");
            row["CopiedReceiptIdentification"].Should().Be("ft1#T7");
            row["CopyType"].Should().Be("ticket");
            row["CopyNumber"].Should().Be("2");
            row["CopyReason"].Should().Be("ticket perdu");
        }

        [Fact]
        public async Task TheLogJournal_KeepsTheCommonColumnsOnly()
        {
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, ArchiveV2FilesTests.Request(0x4652_2000_0000_3001), ArchiveV2FilesTests.Response("ft1#L1", "l1"))!;

            (await Rows(Journal(0x009), entry))[0].Should().NotContainKey("CopyType");
        }

        // §2 charge items.

        [Fact]
        public async Task ChargeItems_ReadCountsAsLineAndTheDiscountOfTheLine()
        {
            var request = "{\"ftReceiptCase\":" + 0x4652_2000_0000_0001L + ",\"cbChargeItems\":[" +
                "{\"Position\":1,\"Description\":\"Café\",\"Amount\":4,\"VATRate\":10,\"ftChargeItemCase\":" + 0x4652_2000_0000_0013L + "}," +
                "{\"Position\":2,\"Description\":\"Remise\",\"Amount\":-1,\"VATRate\":10,\"ftChargeItemCase\":" + (0x4652_2000_0004_0013L) + ",\"ftChargeItemCaseData\":{\"FR\":{\"DiscountCode\":\"D1\",\"DiscountRate\":25}}}],\"cbPayItems\":[]}";
            var state = "{\"FR\":{\"ChargeItems\":[" +
                "{\"Position\":1,\"VATAmount\":0.27,\"AmountHT\":2.73,\"VATNature\":\"00\",\"CountsAsLine\":true,\"DiscountAmount\":-1,\"DiscountCode\":\"D1\",\"DiscountRate\":\"25\"}," +
                "{\"Position\":2,\"VATAmount\":-0.09,\"AmountHT\":-0.91,\"VATNature\":\"00\",\"CountsAsLine\":false,\"AppliesTo\":1}]}}";
            var entry = ArchiveEntry.Read(Guid.NewGuid(), QueueId, 1, request, ArchiveV2FilesTests.Response("ft1#T1", "t1", stateData: state))!;

            var rows = await Rows(ChargeItems(), entry);

            rows[0]["CountsAsLine"].Should().Be("true");
            rows[1]["CountsAsLine"].Should().Be("false");
            rows[0]["DiscountCode"].Should().Be("D1", "the discount columns are filled on the sales line the discount applies to");
            rows[0]["DiscountRate"].Should().Be("25");
            rows[1]["DiscountCode"].Should().BeEmpty();
        }
    }
}
