using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.SCU.GR.Abstraction;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// the UniqueDocumentIdentifier signature item
/// (caption "Μοναδικός αριθμός παραστατικού") is printed on the receipt as
///
///     AFM|dd/MM/yyyy|branch|type|series|aa
///
/// where type is the AADE invoice type code (11.1, 1.1, ...) — the same value myDATA receives, i.e. the
/// XmlEnum value of InvoiceType, not the C# member name (Item111, Item11, ...).
/// </summary>
public class InvoiceSignatureTests
{
    private static AADEFactory CreateFactory() => new(new MasterDataConfiguration
    {
        Account = new AccountMasterData { VatId = "123456789" },
        Outlet = new OutletMasterData { LocationId = "0" }
    }, "https://receipts.example.com");

    private static ReceiptRequest CreateRequest(ReceiptCase receiptCase, MiddlewareCustomer? customer = null, object? receiptCaseData = null) => new()
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = new DateTime(2026, 10, 7, 10, 44, 19, DateTimeKind.Utc),
        cbReceiptReference = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.NewGuid(),
        cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Amount = 100,
                Quantity = 1,
                Description = "Test Item",
                ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
                VATRate = 24
            }
        },
        cbPayItems = new List<PayItem>
        {
            new PayItem { Amount = 100, ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0000 }
        },
        ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase),
        cbCustomer = customer,
        ftReceiptCaseData = receiptCaseData
    };

    private static (InvoicesDoc doc, SignatureItem signature) SignInvoice(ReceiptRequest receiptRequest)
    {
        var receiptResponse = new ReceiptResponse
        {
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftReceiptIdentification = "ft17#",
            ftCashBoxIdentification = "ftCashBox01",
            ftSignatures = new List<SignatureItem>()
        };
        var (doc, error) = CreateFactory().MapToInvoicesDoc(receiptRequest, receiptResponse);
        error.Should().BeNull();

        var request = new ProcessRequest { ReceiptRequest = receiptRequest, ReceiptResponse = receiptResponse };
        SignatureItemFactoryGR.AddInvoiceSignature(request, doc!);

        var signature = request.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType == SignatureTypeGR.UniqueDocumentIdentifier.As<SignatureType>());
        return (doc!, signature);
    }

    private static string ExpectedData(InvoicesDoc doc, string invoiceTypeCode)
    {
        var invoice = doc.invoice[0];
        return $"123456789|07/10/2026|0|{invoiceTypeCode}|{invoice.invoiceHeader.series}|{invoice.invoiceHeader.aa}";
    }

    [Fact]
    public void AddInvoiceSignature_RetailReceipt_ShouldPrintInvoiceTypeCode111()
    {
        var (doc, signature) = SignInvoice(CreateRequest(ReceiptCase.PointOfSaleReceipt0x0001));

        doc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);
        signature.Data.Split('|')[3].Should().Be("11.1");
        signature.Data.Should().Be(ExpectedData(doc, "11.1"));
    }

    [Fact]
    public void AddInvoiceSignature_B2BInvoice_ShouldPrintInvoiceTypeCode11()
    {
        var (doc, signature) = SignInvoice(CreateRequest(ReceiptCase.InvoiceB2B0x1002,
            new MiddlewareCustomer { CustomerVATId = "987654321", CustomerCountry = "GR" }));

        doc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11);
        signature.Data.Split('|')[3].Should().Be("1.1");
        signature.Data.Should().Be(ExpectedData(doc, "1.1"));
    }

    [Fact]
    public void AddInvoiceSignature_WithInvoiceTypeOverride_ShouldPrintOverriddenInvoiceTypeCode()
    {
        var (doc, signature) = SignInvoice(CreateRequest(ReceiptCase.InvoiceB2B0x1002,
            new MiddlewareCustomer { CustomerVATId = "987654321", CustomerCountry = "GR" },
            new { GR = new { mydataoverride = new { invoice = new { invoiceHeader = new { invoiceType = "1.6" } } } } }));

        doc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item16);
        signature.Data.Split('|')[3].Should().Be("1.6");
        signature.Data.Should().Be(ExpectedData(doc, "1.6"));
    }
}
