using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

public class PreviousReceiptReferenceTests
{
    private static AADEFactory CreateFactory() => new(new MasterDataConfiguration
    {
        Account = new AccountMasterData { VatId = "112545020" },
        Outlet = new OutletMasterData { LocationId = "0" }
    }, "https://receipts.example.com");

    private static ReceiptRequest CreatePosReceiptRequest()
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "T-Shirt Red XL",
                    Amount = -35.80m,
                    VATRate = 24m,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Description = "Cash",
                    Amount = -35.80m,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            }
        };
    }

    private static ReceiptResponse CreatePosReceiptResponse(ReceiptRequest request) => new()
    {
        cbReceiptReference = request.cbReceiptReference,
        ftCashBoxIdentification = "TEST-001",
        ftReceiptIdentification = "ft123#"
    };

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMark_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            PreviousReceiptReference = new
            {
                GR = new
                {
                    invoiceMark = "400123456789"
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAsArray_PopulatesAllMarks()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            PreviousReceiptReference = new
            {
                GR = new
                {
                    invoiceMark = new[] { "400123456789", "400987654321" }
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L, 400987654321L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAsLong_PopulatesCorrelatedInvoices()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.ftReceiptCaseData = new
        {
            PreviousReceiptReference = new
            {
                GR = new
                {
                    invoiceMark = 400123456789L
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_NonRefund_PosReceipt_WithExternalInvoiceMark_PopulatesMultipleConnectedMarks()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.cbChargeItems[0].Amount = 35.80m;
        request.cbPayItems[0].Amount = 35.80m;
        request.ftReceiptCaseData = new
        {
            PreviousReceiptReference = new
            {
                GR = new
                {
                    invoiceMark = "400123456789"
                }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        // Retail/POS (Item11 family) supports both correlatedInvoices and multipleConnectedMarks;
        // the factory prefers multipleConnectedMarks for non-refund flows when available.
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_WithExternalInvoiceMarkAndReceiptReferences_MergesBoth()
    {
        var factory = CreateFactory();
        var request = CreatePosReceiptRequest();
        request.ftReceiptCase = request.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        request.cbPreviousReceiptReference = "internal-ref";
        request.ftReceiptCaseData = new
        {
            PreviousReceiptReference = new
            {
                GR = new
                {
                    invoiceMark = "400999999999"
                }
            }
        };

        var refRequest = new ReceiptRequest
        {
            cbTerminalID = "1",
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = "internal-ref",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };
        var refResponse = new ReceiptResponse
        {
            cbReceiptReference = "internal-ref",
            ftCashBoxIdentification = "TEST-001",
            ftReceiptIdentification = "ft100#",
            ftSignatures = new List<SignatureItem>
            {
                new SignatureItem { Caption = "invoiceMark", Data = "400111111111" }
            }
        };

        var response = CreatePosReceiptResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(
            request,
            response,
            new List<(ReceiptRequest, ReceiptResponse)> { (refRequest, refResponse) });

        error.Should().BeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400111111111L, 400999999999L });
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_WithOnlyExternalInvoiceMark_Succeeds()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>(),
            ftReceiptCaseData = new
            {
                PreviousReceiptReference = new
                {
                    GR = new
                    {
                        invoiceMark = "400123456789"
                    }
                }
            }
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item86);
        header.totalCancelDeliveryOrders.Should().BeTrue();
        header.multipleConnectedMarks.Should().BeEquivalentTo(new[] { 400123456789L });
    }

    [Fact]
    public void MapToInvoicesDoc_RestaurantOrderVoid_NoMarksAtAll_ReturnsError()
    {
        var factory = CreateFactory();
        var request = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000)
                .WithCase(ReceiptCase.Order0x3004)
                .WithFlag(ReceiptCaseFlags.Void),
            cbTerminalID = "1",
            cbCustomer = new MiddlewareCustomer { CustomerCountry = "GR", CustomerVATId = "026883248" },
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbArea = "105",
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "VOID item",
                    Amount = 0.0m,
                    VATRate = 0,
                    VATAmount = 0,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000)
                        .WithFlag(ChargeItemCaseFlags.Void)
                        .WithVat(ChargeItemCase.NotTaxable)
                }
            },
            cbPayItems = new List<PayItem>()
        };

        var response = new ReceiptResponse
        {
            cbReceiptReference = request.cbReceiptReference,
            ftCashBoxIdentification = "CB001",
            ftReceiptIdentification = "ft123#"
        };

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().NotBeNull();
        error!.Exception.Message.Should().Contain("cbPreviousReceiptReference");
        doc.Should().BeNull();
    }
}
