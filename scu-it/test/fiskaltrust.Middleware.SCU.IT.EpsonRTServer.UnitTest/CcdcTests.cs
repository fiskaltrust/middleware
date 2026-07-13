using fiskaltrust.Middleware.SCU.IT.EpsonRTServer;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class CcdcTests
    {
        // Golden vector: a createReceipt request that was ACCEPTED (code=0) by an Epson RT Server (firmware 6.01,
        // device 99SEA004010). The CCDC is the lowercase hex SHA-256 of the whole <receipt> element as sent.
        private const string SectionA = "99SEA004010FISK0001838282026070207420004000070100";

        private const string PrinterFiscalReceipt =
            "<printerFiscalReceipt><beginFiscalReceipt /><printRecItem vatID=\"1\" quantity=\"1\" description=\"Articolo\" type=\"B\" unitPrice=\"1.00\" />" +
            "<printRecTotal payment=\"1.00\" index=\"0\" description=\"Contanti\" paymentType=\"0\" />" +
            "<fiscalInformation tillId=\"FISK0001\" zRepNumber=\"0742\" recNumber=\"4\" dateTime=\"20260702T120000\" recAmount=\"1.00\" recVAT=\"0.18\" " +
            "cashAmount=\"1.00\" ePayAmount=\"0.00\" noPayAmount=\"0.00\" changeAmount=\"0.00\" paidAmount=\"1.00\" rtSerialNumber=\"99SEA004010\" " +
            "srtUtcOffset=\"2\" docType=\"0\" dailyAmount=\"702.00\" /><endFiscalReceipt /></printerFiscalReceipt>";

        private const string ExpectedCcdc = "4582917fdeb49210f1578e08a07241f92920287b41871954b9491bcfef7293a0";

        [Fact]
        public void ComputeCcdc_Should_Match_DeviceAcceptedFingerprint()
        {
            var receiptElement = $"<receipt><hash fingerPrint=\"{SectionA}\"/>{PrinterFiscalReceipt}</receipt>";

            var ccdc = GlobalTools.ComputeCcdc(receiptElement);

            ccdc.Should().Be(ExpectedCcdc);
        }

        [Fact]
        public void GetSHA256Hex_Should_Be_Lowercase_64_Chars()
        {
            var hash = GlobalTools.GetSHA256Hex("anything");

            hash.Should().HaveLength(64);
            hash.Should().Be(hash.ToLowerInvariant());
        }
    }
}
