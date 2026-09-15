using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Issue #290 — Climate Crisis Resilience Fee (Τέλος Ανθεκτικότητας Κλιματικής Κρίσης)
/// as a myDATA invoiceType 8.2 record. 8.2 is reachable only via mydataoverride (there is no
/// native ftReceiptCase -> 8.2). For 8.2 the special tax must be reported at LINE level
/// (invoiceRow.otherTaxesAmount + otherTaxesPercentCategory), NOT as a document-level taxesTotals
/// element — otherwise AADE rejects with XMLSyntaxError 101.
///
/// These tests currently FAIL: the invoice body is built from the ftReceiptCase-derived invoice
/// type (B2B), so the 8.2 line-level routing is bypassed — the special tax line is dropped from
/// invoiceDetails and the fee is emitted as taxesTotals.
/// </summary>
public class ClimateResilienceFee82Tests
{
    private static AADEFactory CreateFactory() =>
        new(new MasterDataConfiguration
        {
            Account = new AccountMasterData { VatId = "123456789" },
            Outlet = new OutletMasterData { LocationId = "0" }
        }, "https://receipts.example.com");

    private static ReceiptRequest CreateClimateFeeRequest(string description, decimal amount)
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            // B2B carrier case; invoiceType is forced to 8.2 by the override below.
            ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_1002,
            cbCustomer = new MiddlewareCustomer
            {
                CustomerVATId = "026883248",
                CustomerName = "Πελάτης A.E.",
                CustomerCountry = "GR"
            },
            cbChargeItems = new List<ChargeItem>
            {
                new ChargeItem
                {
                    Position = 1,
                    Quantity = 1,
                    Description = description,
                    Amount = amount,
                    VATRate = 0,
                    // type-of-service 0xF0 (special tax) + slot 8 (not taxable)
                    ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_00F8
                }
            },
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Description = "Cash",
                    Amount = amount,
                    ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001
                }
            },
            ftReceiptCaseData = new
            {
                GR = new
                {
                    mydataoverride = new
                    {
                        invoice = new
                        {
                            invoiceHeader = new
                            {
                                invoiceType = "8.2"
                            }
                        }
                    }
                }
            }
        };
    }

    private static ReceiptResponse CreateResponse(ReceiptRequest request) =>
        new()
        {
            cbReceiptReference = request.cbReceiptReference,
            ftReceiptIdentification = "ft123ABC#",
            ftCashBoxIdentification = "TEST-001"
        };

    [Theory]
    // 5-star hotel, 10,00 EUR per room/night -> otherTaxes code 23
    [InlineData("Ξενοδοχεία 5 αστέρων 10,00€ (ανά Δωμ./Διαμ.)", 10.0, 23)]
    // 4-star hotel, 7,00 EUR per room/night -> otherTaxes code 22 (proves it is not tier-specific)
    [InlineData("Ξενοδοχεία 4 αστέρων 7,00€ (ανά Δωμ./Διαμ.)", 7.0, 22)]
    public void MapToInvoicesDoc_ClimateFee82_ReportsFeeAtLineLevel(string description, decimal amount, int expectedOtherTaxCode)
    {
        // Arrange
        var factory = CreateFactory();
        var request = CreateClimateFeeRequest(description, amount);
        var response = CreateResponse(request);

        // Act
        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        // Assert
        error.Should().BeNull();
        doc.Should().NotBeNull();

        var invoice = doc!.invoice[0];
        invoice.invoiceHeader.invoiceType.Should().Be(InvoiceType.Item82);

        // For 8.2 the tax MUST be on the invoice line, and there must be NO document-level taxesTotals.
        invoice.taxesTotals.Should().BeNull("for invoiceType 8.2 the fee must be reported at line level, not in taxesTotals");

        invoice.invoiceDetails.Should().HaveCount(1);
        var line = invoice.invoiceDetails[0];
        line.netValue.Should().Be(0m);
        line.vatCategory.Should().Be(8);
        line.otherTaxesPercentCategorySpecified.Should().BeTrue();
        line.otherTaxesPercentCategory.Should().Be(expectedOtherTaxCode);
        line.otherTaxesAmountSpecified.Should().BeTrue();
        line.otherTaxesAmount.Should().Be(amount);

        var summary = invoice.invoiceSummary;
        summary.totalNetValue.Should().Be(0m);
        summary.totalVatAmount.Should().Be(0m);
        summary.totalOtherTaxesAmount.Should().Be(amount);
        summary.totalGrossValue.Should().Be(amount);
    }
}
