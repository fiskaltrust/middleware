using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class MyDataXmlSignatureTests
{
    [Fact]
    public void AddMyDataXmlSignature_ShouldAddSignatureWithXmlPayload()
    {
        // Arrange
        var request = new ProcessRequest
        {
            ReceiptRequest = new ReceiptRequest
            {
                cbTerminalID = "1",
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0001,
                cbChargeItems = [],
                cbPayItems = []
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftState = (State) 0x4752_2000_0000_0000,
                ftSignatures = new List<SignatureItem>()
            }
        };

        var testXml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><InvoicesDoc xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><invoice><issuer><vatNumber>123456789</vatNumber></issuer></invoice></InvoicesDoc>";

        // Act
        SignatureItemFactoryGR.AddMyDataXmlSignature(request, testXml);

        // Assert
        request.ReceiptResponse.ftSignatures.Should().ContainSingle(s => s.Caption == "mydata-xml");
        var sig = request.ReceiptResponse.ftSignatures.First(s => s.Caption == "mydata-xml");

        sig.Data.Should().StartWith("<invoicesDoc ");
        sig.Data.Should().EndWith("</invoicesDoc>");
        sig.Data.Should().NotStartWith("<?xml");
        sig.Data.Should().Contain("<vatNumber>123456789</vatNumber>");
        sig.ftSignatureFormat.Should().Be(SignatureFormat.Text);
    }

    [Fact]
    public void GenerateInvoicePayload_OutputShouldBeSerializableXml()
    {
        // Arrange — build the InvoicesDoc directly
        var doc = new InvoicesDoc
        {
            invoice = new[]
            {
            new AadeBookInvoiceType
            {
                issuer = new PartyType
                {
                    vatNumber = "123456789",
                    country = CountryType.GR,
                    branch = 0
                },
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "TEST",
                    aa = "1",
                    issueDate = DateTime.UtcNow.Date,
                    invoiceType = InvoiceType.Item111,
                    currency = CurrencyType.EUR,
                    currencySpecified = true,
                },
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1,
                        netValue = 10.00m,
                        vatCategory = 1,
                        vatAmount = 2.40m,
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 10.00m,
                    totalVatAmount = 2.40m,
                    totalWithheldAmount = 0,
                    totalFeesAmount = 0,
                    totalStampDutyAmount = 0,
                    totalOtherTaxesAmount = 0,
                    totalDeductionsAmount = 0,
                    totalGrossValue = 12.40m,
                },
                downloadingInvoiceUrl = "https://receipts-sandbox.fiskaltrust.eu/00000000-0000-0000-0000-000000000000/00000000-0000-0000-0000-000000000001"
            }
        }
        };

        // Act
        var payload = AADEFactory.GenerateInvoicePayload(doc);

        // Assert
        payload.Should().NotBeNullOrEmpty();
        payload.Should().Contain("InvoicesDoc");
        payload.Should().Contain("downloadingInvoiceUrl");
        payload.Should().Contain("https://receipts-sandbox.fiskaltrust.eu");

        // Verify the XML starts with a declaration (which AddMyDataXmlSignature strips)
        payload.Should().StartWith("<?xml");
    }
}