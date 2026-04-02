using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Models;
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
    public void AddMyDataXmlSignature_StoredXmlShouldContainReturnValues()
    {
        // Arrange
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
                counterpart = new PartyType
                {
                    vatNumber = "987654321",
                    country = CountryType.GR,
                    branch = 0
                },
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A",
                    aa = "42",
                    issueDate = new DateTime(2026, 3, 27),
                    invoiceType = InvoiceType.Item111,
                    currency = CurrencyType.EUR,
                    currencySpecified = true,
                },
                paymentMethods = new[]
                {
                    new PaymentMethodDetailType
                    {
                        type = MyDataPaymentMethods.Cash,
                        amount = 148.80m,
                    }
                },
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1,
                        netValue = 50.00m,
                        vatCategory = 1,
                        vatAmount = 12.00m,
                        quantity = 2,
                        quantitySpecified = true,
                    },
                    new InvoiceRowType
                    {
                        lineNumber = 2,
                        netValue = 70.00m,
                        vatCategory = 1,
                        vatAmount = 16.80m,
                        quantity = 1,
                        quantitySpecified = true,
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 120.00m,
                    totalVatAmount = 28.80m,
                    totalWithheldAmount = 0,
                    totalFeesAmount = 0,
                    totalStampDutyAmount = 0,
                    totalOtherTaxesAmount = 0,
                    totalDeductionsAmount = 0,
                    totalGrossValue = 148.80m,
                },
                downloadingInvoiceUrl = "https://receipts-sandbox.fiskaltrust.eu/aaa/bbb",

                uid = "F4C9B10E629690BDC6EC410455FAC9158995A29A",
                mark = 400001951868897,
                markSpecified = true,
                authenticationCode = "3F48BDCC0AB443EB84114F721F958BE6",
            }
        }
        };

        var enrichedPayload = AADEFactory.GenerateInvoicePayload(doc);

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

        // Act
        SignatureItemFactoryGR.AddMyDataXmlSignature(request, enrichedPayload);

        // Assert — the XML from the signature must contain everything
        var sig = request.ReceiptResponse.ftSignatures.First(s => s.Caption == "mydata-xml");
        var xml = sig.Data;

        // AADE return values
        xml.Should().Contain("<uid>F4C9B10E629690BDC6EC410455FAC9158995A29A</uid>");
        xml.Should().Contain("<mark>400001951868897</mark>");
        xml.Should().Contain("<authenticationCode>3F48BDCC0AB443EB84114F721F958BE6</authenticationCode>");

        // Invoice data is still there
        xml.Should().Contain("<vatNumber>123456789</vatNumber>");
        xml.Should().Contain("<vatNumber>987654321</vatNumber>");
        xml.Should().Contain("<series>A</series>");
        xml.Should().Contain("<aa>42</aa>");
        xml.Should().Contain("<invoiceType>11.1</invoiceType>");
        xml.Should().Contain("<totalGrossValue>148.80</totalGrossValue>");
        xml.Should().Contain("<downloadingInvoiceUrl>https://receipts-sandbox.fiskaltrust.eu/aaa/bbb</downloadingInvoiceUrl>");
    }
    [Fact]
    public void AddMyDataXmlSignature_StoredXmlShouldContainAllInvoiceFields()
    {
        // Arrange
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
                counterpart = new PartyType
                {
                    vatNumber = "987654321",
                    country = CountryType.GR,
                    branch = 0
                },
                invoiceHeader = new InvoiceHeaderType
                {
                    series = "A",
                    aa = "42",
                    issueDate = new DateTime(2026, 3, 27),
                    invoiceType = InvoiceType.Item111,
                    currency = CurrencyType.EUR,
                    currencySpecified = true,
                },
                paymentMethods = new[]
                {
                    new PaymentMethodDetailType
                    {
                        type = MyDataPaymentMethods.Cash,
                        amount = 148.80m,
                    }
                },
                invoiceDetails = new[]
                {
                    new InvoiceRowType
                    {
                        lineNumber = 1,
                        netValue = 50.00m,
                        vatCategory = 1,
                        vatAmount = 12.00m,
                        quantity = 2,
                        quantitySpecified = true,
                    },
                    new InvoiceRowType
                    {
                        lineNumber = 2,
                        netValue = 70.00m,
                        vatCategory = 1,
                        vatAmount = 16.80m,
                        quantity = 1,
                        quantitySpecified = true,
                    }
                },
                invoiceSummary = new InvoiceSummaryType
                {
                    totalNetValue = 120.00m,
                    totalVatAmount = 28.80m,
                    totalWithheldAmount = 0,
                    totalFeesAmount = 0,
                    totalStampDutyAmount = 0,
                    totalOtherTaxesAmount = 0,
                    totalDeductionsAmount = 0,
                    totalGrossValue = 148.80m,
                },
                downloadingInvoiceUrl = "https://receipts-sandbox.fiskaltrust.eu/aaa/bbb"
            }
        }
        };

        var payload = AADEFactory.GenerateInvoicePayload(doc);

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

        // Act
        SignatureItemFactoryGR.AddMyDataXmlSignature(request, payload);

        // Assert 
        var sig = request.ReceiptResponse.ftSignatures.First(s => s.Caption == "mydata-xml");
        var xml = sig.Data;

        xml.Should().StartWith("<invoicesDoc ");
        xml.Should().EndWith("</invoicesDoc>");
        xml.Should().NotStartWith("<?xml");

        // Issuer
        xml.Should().Contain("<issuer>");
        xml.Should().Contain("<vatNumber>123456789</vatNumber>");

        // Counterpart
        xml.Should().Contain("<counterpart>");
        xml.Should().Contain("<vatNumber>987654321</vatNumber>");

        // Invoice header
        xml.Should().Contain("<invoiceHeader>");
        xml.Should().Contain("<series>A</series>");
        xml.Should().Contain("<aa>42</aa>");
        xml.Should().Contain("<invoiceType>11.1</invoiceType>");
        xml.Should().Contain("<currency>EUR</currency>");

        // Payment methods
        xml.Should().Contain("<paymentMethods>");
        xml.Should().Contain("<amount>148.80</amount>");

        // Invoice details — both lines
        xml.Should().Contain("<lineNumber>1</lineNumber>");
        xml.Should().Contain("<netValue>50.00</netValue>");
        xml.Should().Contain("<vatAmount>12.00</vatAmount>");
        xml.Should().Contain("<lineNumber>2</lineNumber>");
        xml.Should().Contain("<netValue>70.00</netValue>");
        xml.Should().Contain("<vatAmount>16.80</vatAmount>");

        // Invoice summary / totals
        xml.Should().Contain("<invoiceSummary>");
        xml.Should().Contain("<totalNetValue>120.00</totalNetValue>");
        xml.Should().Contain("<totalVatAmount>28.80</totalVatAmount>");
        xml.Should().Contain("<totalGrossValue>148.80</totalGrossValue>");

        // downloadingInvoiceUrl
        xml.Should().Contain("<downloadingInvoiceUrl>https://receipts-sandbox.fiskaltrust.eu/aaa/bbb</downloadingInvoiceUrl>");
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