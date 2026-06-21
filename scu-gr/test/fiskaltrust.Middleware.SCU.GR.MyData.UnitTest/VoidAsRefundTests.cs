using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// myDATA providers cannot call CancelInvoice (see fiskaltrust/market-gr#133 — endpoint is for direct AADE/ERP
/// users only). Receipt-level Void on anything other than Order0x3004 must therefore be mapped to the refund
/// flow so it produces a valid credit-note / refund-slip document. Order0x3004 keeps its dedicated 8.6 path.
/// </summary>
public class VoidAsRefundTests
{
    [Fact]
    public void Receipt_VoidOnly_ResolvesToRetailRefundType()
    {
        var receiptRequest = BuildReceipt(ReceiptCase.PointOfSaleReceipt0x0001, ReceiptCaseFlags.Void);

        AADEMappings.GetInvoiceType(receiptRequest).Should().Be(InvoiceType.Item114);
    }

    [Fact]
    public void Invoice_VoidWithPreviousReference_ResolvesToCreditNoteAssociated()
    {
        var receiptRequest = BuildInvoiceB2B(ReceiptCaseFlags.Void, hasPreviousReceipt: true);

        AADEMappings.GetInvoiceType(receiptRequest).Should().Be(InvoiceType.Item51);
    }

    [Fact]
    public void Invoice_VoidWithoutPreviousReference_ResolvesToCreditNoteNonAssociated()
    {
        var receiptRequest = BuildInvoiceB2B(ReceiptCaseFlags.Void, hasPreviousReceipt: false);

        AADEMappings.GetInvoiceType(receiptRequest).Should().Be(InvoiceType.Item52);
    }

    [Fact]
    public void PaymentTransfer_Void_ResolvesToRefundPaymentTransfer()
    {
        var receiptRequest = BuildReceipt(ReceiptCase.PaymentTransfer0x0002, ReceiptCaseFlags.Void);

        AADEMappings.GetInvoiceType(receiptRequest).Should().Be(InvoiceType.Item85);
    }

    [Fact]
    public void Order_VoidStays86_RegressionGuardForExistingCancellationPath()
    {
        // The 8.6 cancellation pathway (totalCancelDeliveryOrders + SetInvoiceHeaderFieldsForVoid) MUST keep
        // firing for Void+Order0x3004 — only non-8.6 voids fall into the refund mapping.
        var receiptRequest = BuildReceipt(ReceiptCase.Order0x3004, ReceiptCaseFlags.Void);

        AADEMappings.GetInvoiceType(receiptRequest).Should().Be(InvoiceType.Item86);
    }

    [Fact]
    public void Receipt_Void_NegatesAmountsAndPopulatesMultipleConnectedMarks()
    {
        var previousMark = 4000019580341891L;
        var receiptRequest = BuildReceipt(ReceiptCase.PointOfSaleReceipt0x0001, ReceiptCaseFlags.Void);
        receiptRequest.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Quantity = 2,
                Description = "Item",
                Amount = 248m,
                VATRate = 24m,
                VATAmount = 48m,
                Position = 1,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0013)
            }
        };
        receiptRequest.cbPayItems = new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 248m, Description = "Cash",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment) }
        };
        receiptRequest.cbPreviousReceiptReference = "prev-ref";

        var receiptResponse = NewReceiptResponse(receiptRequest);
        var factory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

        var (doc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse, BuildReceiptReferences(previousMark));

        error.Should().BeNull();
        doc.Should().NotBeNull();

        var invoice = doc!.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item114);
        invoice.invoiceHeader.multipleConnectedMarks.Should().BeEquivalentTo(new[] { previousMark });

        var line = invoice.invoiceDetails.Single();
        line.quantity.Should().Be(-2m);
        line.netValue.Should().Be(-200m);
        line.vatAmount.Should().Be(-48m);

        invoice.invoiceSummary.totalNetValue.Should().Be(-200m);
        invoice.invoiceSummary.totalVatAmount.Should().Be(-48m);
        invoice.invoiceSummary.totalGrossValue.Should().Be(-248m);
    }

    [Fact]
    public void Invoice_VoidWithPreviousMark_PopulatesCorrelatedInvoices()
    {
        var previousMark = 4000019580341899L;
        var receiptRequest = BuildInvoiceB2B(ReceiptCaseFlags.Void, hasPreviousReceipt: true);
        receiptRequest.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                Quantity = 1,
                Description = "Service",
                Amount = 124m,
                VATRate = 24m,
                VATAmount = 24m,
                Position = 1,
                ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0013)
            }
        };
        receiptRequest.cbPayItems = new List<PayItem>
        {
            new PayItem { Position = 1, Amount = 124m, Description = "Cash",
                ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment) }
        };

        var receiptResponse = NewReceiptResponse(receiptRequest);
        var factory = new AADEFactory(MockMasterData(), "https://test.receipts.example.com");

        var (doc, error) = factory.MapToInvoicesDoc(receiptRequest, receiptResponse, BuildReceiptReferences(previousMark));

        error.Should().BeNull();
        doc.Should().NotBeNull();

        var header = doc!.invoice[0].invoiceHeader;
        header.invoiceType.Should().Be(InvoiceType.Item51, "B2B invoice + Void with prior MARK → credit note - associated (5.1)");
        header.correlatedInvoices.Should().BeEquivalentTo(new[] { previousMark }, "5.1 carries prior MARKs via correlatedInvoices, not multipleConnectedMarks");
        header.multipleConnectedMarks.Should().BeNull();
    }

    [Fact]
    public void Void_NonOrder86_ThrowsOnlyIfNotEffectiveRefund_GuardCaseRequiresInvoiceTypeOverride()
    {
        // Defense-in-depth: Void on Order0x3004 → 8.6 path; Void on anything else → refund path.
        // The throw remains as a safety net for the (synthetic) case where someone overrides
        // invoiceType away from 8.6 while keeping Void+Order0x3004 — we can't infer intent then.
        // This test simply pins the helper semantics that drive the dispatch.
        var voidOrder = BuildReceipt(ReceiptCase.Order0x3004, ReceiptCaseFlags.Void);
        voidOrder.IsEffectiveRefund().Should().BeFalse("Void+Order0x3004 stays on the 8.6 cancellation path");

        var voidReceipt = BuildReceipt(ReceiptCase.PointOfSaleReceipt0x0001, ReceiptCaseFlags.Void);
        voidReceipt.IsEffectiveRefund().Should().BeTrue("Void+Receipt routes through the refund mapping");

        var refundReceipt = BuildReceipt(ReceiptCase.PointOfSaleReceipt0x0001, ReceiptCaseFlags.Refund);
        refundReceipt.IsEffectiveRefund().Should().BeTrue("explicit Refund is always effective-refund");

        var plainReceipt = BuildReceipt(ReceiptCase.PointOfSaleReceipt0x0001, flags: 0);
        plainReceipt.IsEffectiveRefund().Should().BeFalse();
    }

    private static ReceiptRequest BuildReceipt(ReceiptCase @case, ReceiptCaseFlags flags)
    {
        var receiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(@case);
        if (flags != 0)
        {
            receiptCase = receiptCase.WithFlag(flags);
        }

        return new ReceiptRequest
        {
            ftReceiptCase = receiptCase,
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Item",
                    Amount = 124m,
                    VATRate = 24m,
                    VATAmount = 24m,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0013)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 124m,
                    Description = "Cash",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            }
        };
    }

    private static ReceiptRequest BuildInvoiceB2B(ReceiptCaseFlags flags, bool hasPreviousReceipt)
    {
        var req = new ReceiptRequest
        {
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002).WithFlag(flags),
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "GR12345678",
                CustomerName = "Test Company Ltd",
                CustomerStreet = "Test Street 1",
                CustomerCity = "Athens",
                CustomerZip = "12345",
                CustomerCountry = "GR"
            },
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Quantity = 1,
                    Description = "Item",
                    Amount = 124m,
                    VATRate = 24m,
                    VATAmount = 24m,
                    Position = 1,
                    ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0013)
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = 124m,
                    Description = "Cash",
                    ftPayItemCase = ((PayItemCase)0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment)
                }
            }
        };

        if (hasPreviousReceipt)
        {
            req.cbPreviousReceiptReference = "prev-ref";
        }

        return req;
    }

    private static ReceiptResponse NewReceiptResponse(ReceiptRequest req) => new ReceiptResponse
    {
        cbReceiptReference = req.cbReceiptReference,
        ftCashBoxIdentification = "CB001",
        ftReceiptIdentification = "ft123#"
    };

    private static List<(ReceiptRequest, ReceiptResponse)> BuildReceiptReferences(params long[] marks)
    {
        return marks.Select(mark =>
        {
            var refRequest = new ReceiptRequest
            {
                ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001),
                cbTerminalID = "1",
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                ftPosSystemId = Guid.NewGuid(),
                cbChargeItems = new List<ChargeItem>(),
                cbPayItems = new List<PayItem>()
            };
            var refResponse = new ReceiptResponse
            {
                cbReceiptReference = refRequest.cbReceiptReference,
                ftCashBoxIdentification = "CB001",
                ftReceiptIdentification = "ft100#",
                ftSignatures = new List<SignatureItem>
                {
                    new SignatureItem { Caption = "invoiceMark", Data = mark.ToString() }
                }
            };
            return (refRequest, refResponse);
        }).ToList();
    }

    private static storage.V0.MasterData.MasterDataConfiguration MockMasterData() => new storage.V0.MasterData.MasterDataConfiguration
    {
        Account = new storage.V0.MasterData.AccountMasterData { VatId = "112545020" }
    };
}
