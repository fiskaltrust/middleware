using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    /// <summary>
    /// The customer tax identifier is printed as a directIO ("scontrino parlante"): an 11 digit partita IVA
    /// goes to command 1060, a 16 character codice fiscale to 1061. The three request builders used to carry
    /// three byte-identical copies of this logic; they now share one helper, so these tests assert all three
    /// behave the same.
    /// </summary>
    public class CustomerTaxIdDirectIOTests
    {
        private const string PartitaIva = "01606720215";
        private const string CodiceFiscale = "RSSMRA80A01H501U";

        private static readonly EpsonRTPrinterSCUConfiguration _configuration = new();

        private static ReceiptRequest CreateReceiptRequest(string cbCustomer) => new ReceiptRequest
        {
            ftReceiptCase = 0x4954_2000_0000_0001,
            cbReceiptMoment = new DateTime(2026, 7, 2, 12, 0, 0),
            cbCustomer = cbCustomer,
            cbChargeItems = new[] { new ChargeItem { Amount = 1.00m, Quantity = 1, Description = "TEST", VATRate = 22m, ftChargeItemCase = 0x4954_2000_0000_0003 } },
            cbPayItems = new[] { new PayItem { Amount = 1.00m, Quantity = 1, Description = "CONTANTE", ftPayItemCase = 0x4954_2000_0000_0001 } }
        };

        /// <summary>The three builders that emit the customer tax identifier, as (name) rows.</summary>
        public static IEnumerable<object[]> RequestBuilders()
        {
            yield return new object[] { "invoice" };
            yield return new object[] { "refund" };
            yield return new object[] { "void" };
        }

        private static List<DirectIO> BuildDirectIOCommands(string builder, string cbCustomer)
        {
            var receiptRequest = CreateReceiptRequest(cbCustomer);
            var fiscalReceipt = builder switch
            {
                "invoice" => EpsonCommandFactory.CreateInvoiceRequestContent(_configuration, receiptRequest),
                "refund" => EpsonCommandFactory.CreateRefundRequestContent(_configuration, receiptRequest, 1, 1, new DateTime(2026, 7, 1), "99SEA004010"),
                "void" => EpsonCommandFactory.CreateVoidRequestContent(_configuration, receiptRequest, 1, 1, new DateTime(2026, 7, 1), "99SEA004010"),
                _ => throw new ArgumentOutOfRangeException(nameof(builder))
            };
            return fiscalReceipt.DirectIOCommands;
        }

        private static void AssertSingleCommand(List<DirectIO> commands, string expectedCommand, string expectedValue)
        {
            Assert.Single(commands.Where(x => x.Command == expectedCommand && x.Data == "01" + expectedValue));
            // The two commands are mutually exclusive: only one identifier is printed.
            var other = expectedCommand == "1060" ? "1061" : "1060";
            Assert.Empty(commands.Where(x => x.Command == other));
        }

        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithAPlainPartitaIva_EmitDirectIO1060(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerVATId\":\"" + PartitaIva + "\"}"), "1060", PartitaIva);
        }

        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithACountryPrefixedPartitaIva_StripThePrefix(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerVATId\":\"IT" + PartitaIva + "\"}"), "1060", PartitaIva);
        }

        /// <summary>Lower case and surrounding whitespace used to defeat the old ToUpper()/StartsWith check.</summary>
        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithAnUntrimmedLowerCasePartitaIva_StillEmitDirectIO1060(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerVATId\":\"  it" + PartitaIva + "  \"}"), "1060", PartitaIva);
        }

        /// <summary>A codice fiscale is 16 alphanumeric characters, so it needs command 1061, not 1060.</summary>
        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithACodiceFiscale_EmitDirectIO1061(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerId\":\"" + CodiceFiscale + "\"}"), "1061", CodiceFiscale);
        }

        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithAnUntrimmedLowerCaseCodiceFiscale_StillEmitDirectIO1061(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerId\":\"  rssmra80a01h501u \"}"), "1061", CodiceFiscale);
        }

        /// <summary>Same precedence rule as the Custom SCUs: the codice fiscale wins.</summary>
        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithBothIdentifiers_PreferTheCodiceFiscale(string builder)
        {
            var cbCustomer = "{\"CustomerId\":\"" + CodiceFiscale + "\",\"CustomerVATId\":\"" + PartitaIva + "\"}";

            AssertSingleCommand(BuildDirectIOCommands(builder, cbCustomer), "1061", CodiceFiscale);
        }

        /// <summary>An Italian legal entity uses its partita IVA as codice fiscale, so it belongs on 1060.</summary>
        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithAPartitaIvaInCustomerId_EmitDirectIO1060(string builder)
        {
            AssertSingleCommand(BuildDirectIOCommands(builder, "{\"CustomerId\":\"IT" + PartitaIva + "\"}"), "1060", PartitaIva);
        }

        /// <summary>An empty CustomerId must not suppress a usable partita IVA.</summary>
        [Theory]
        [MemberData(nameof(RequestBuilders))]
        public void AllBuilders_WithAnEmptyCustomerId_FallBackToThePartitaIva(string builder)
        {
            var cbCustomer = "{\"CustomerId\":\"\",\"CustomerVATId\":\"" + PartitaIva + "\"}";

            AssertSingleCommand(BuildDirectIOCommands(builder, cbCustomer), "1060", PartitaIva);
        }

        [Theory]
        [InlineData("{\"CustomerVATId\":\"12345\"}")]              // too short
        [InlineData("{\"CustomerVATId\":\"0160672021A\"}")]        // right length, not all digits
        [InlineData("{\"CustomerId\":\"RSSMRA80A01H501\"}")]       // 15 characters
        [InlineData("{\"CustomerId\":\"RSSMRA80A01H501U!\"}")]     // 16 characters, not alphanumeric
        [InlineData("{\"CustomerVATId\":\"\"}")]
        [InlineData("{\"CustomerName\":\"Mario Rossi\"}")]
        [InlineData("")]
        public void CreateInvoiceRequestContent_WithoutAUsableTaxId_EmitsNoDirectIO(string cbCustomer)
        {
            var commands = BuildDirectIOCommands("invoice", cbCustomer);

            Assert.Empty(commands.Where(x => x.Command == "1060" || x.Command == "1061"));
        }
    }
}
