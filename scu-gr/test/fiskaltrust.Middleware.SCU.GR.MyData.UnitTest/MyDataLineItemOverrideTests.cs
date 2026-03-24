using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.GR.MyData;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest;

/// <summary>
/// Tests for line-item-level mydataoverride via ftChargeItemCaseData.
/// The discountOption field (δικαίωμα έκπτωσης) indicates whether the
/// VAT on a given line is deductible for the buyer/recipient. It is NOT
/// related to price discounts.
/// </summary>
public class MyDataLineItemOverrideTests
{
    #region Helpers
    private static AADEFactory CreateFactory() =>
        new(new MasterDataConfiguration
        {
            Account = new AccountMasterData
            {
                VatId = "123456789"
            },
            Outlet = new OutletMasterData
            {
                LocationId = "0"
            }
        }, "https://receipts.example.com");

    private static ChargeItem CreateChargeItem(decimal amount, decimal vatRate, object? caseData = null) =>
        new()
        {
            Amount = amount,
            Quantity = 1,
            Description = "Test Item",
            ftChargeItemCase = ((ChargeItemCase)0x4752_2000_0000_0000).WithVat(ChargeItemCase.NormalVatRate),
            VATRate = vatRate,
            ftChargeItemCaseData = caseData
        };

    private static object BuildDiscountOptionOverride(bool value) =>
        new
        {
            GR = new
            {
                mydataoverride = new
                {
                    invoicedetails = new
                    {
                        discountOption = value
                    }
                }
            }
        };

    private static ReceiptRequest CreateRequest(params ChargeItem[] chargeItems)
    {
        var items = new List<ChargeItem>(chargeItems);
        var totalAmount = 0m;
        foreach (var item in items)
        {
            totalAmount += item.Amount;
        }

        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = new DateTime(2025, 6, 18, 10, 44, 19, DateTimeKind.Utc),
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            cbChargeItems = items,
            cbPayItems = new List<PayItem>
            {
                new()
                {
                    Amount = totalAmount,
                    ftPayItemCase = (PayItemCase)0x4752_2000_0000_0000
                }
            },
            cbReceiptAmount = totalAmount,
            ftReceiptCase = ((ReceiptCase)0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };
    }

    private static ReceiptResponse CreateResponse(ReceiptRequest request) =>
        new()
        {
            cbReceiptReference = request.cbReceiptReference,
            ftReceiptIdentification = "ft123ABC#",
            ftCashBoxIdentification = "TEST-001"
        };
    #endregion

    #region Tests
    [Fact]
    public void MapToInvoicesDoc_WithoutOverride_ShouldNotSetDiscountOption()
    {
        var factory = CreateFactory();
        var request = CreateRequest(CreateChargeItem(100, 24));
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        doc!.invoice[0].invoiceDetails[0].discountOptionSpecified.Should().BeFalse();
    }

    [Fact]
    public void MapToInvoicesDoc_WithDiscountOptionTrue_ShouldFlagVatAsDeductible()
    {
        var factory = CreateFactory();
        var request = CreateRequest(
            CreateChargeItem(100, 24, BuildDiscountOptionOverride(true)));
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var line = doc!.invoice[0].invoiceDetails[0];
        line.discountOptionSpecified.Should().BeTrue();
        line.discountOption.Should().BeTrue("VAT on this line should be deductible for the buyer");
    }

    [Fact]
    public void MapToInvoicesDoc_WithDiscountOptionFalse_ShouldFlagVatAsNonDeductible()
    {
        var factory = CreateFactory();
        var request = CreateRequest(
            CreateChargeItem(100, 24, BuildDiscountOptionOverride(false)));
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var line = doc!.invoice[0].invoiceDetails[0];
        line.discountOptionSpecified.Should().BeTrue();
        line.discountOption.Should().BeFalse("VAT on this line should not be deductible for the buyer");
    }

    [Fact]
    public void MapToInvoicesDoc_MultipleItems_OnlyOverriddenLineShouldHaveDiscountOption()
    {
        var factory = CreateFactory();
        var request = CreateRequest(
            CreateChargeItem(100, 24),
            CreateChargeItem(50, 24, BuildDiscountOptionOverride(true)));
        var response = CreateResponse(request);

        var (doc, error) = factory.MapToInvoicesDoc(request, response);

        error.Should().BeNull();
        doc.Should().NotBeNull();
        var details = doc!.invoice[0].invoiceDetails;
        details.Should().HaveCount(2);
        details[0].discountOptionSpecified.Should().BeFalse("first item has no override");
        details[1].discountOptionSpecified.Should().BeTrue();
        details[1].discountOption.Should().BeTrue("second item flags VAT as deductible");
    }
    #endregion
}
