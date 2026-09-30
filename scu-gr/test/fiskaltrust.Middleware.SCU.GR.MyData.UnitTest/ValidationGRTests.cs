using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.Validation;
using fiskaltrust.Middleware.SCU.GR.MyData.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.GR.MyData.UnitTest.SCU.MyData;

public class ValidationGRTests
{
    private ChargeItem CreateChargeItem(decimal amount, int vatRate, ChargeItemCaseTypeOfService typeOfService)
    {
        return new ChargeItem
        {
            Position = 1,
            Amount = amount,
            VATRate = vatRate,
            VATAmount = decimal.Round(amount / (100M + vatRate) * vatRate, 2, MidpointRounding.ToEven),
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(typeOfService).WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = "Test Item"
        };
    }

    private ReceiptRequest CreateReceipt(List<ChargeItem> chargeItems, ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001)
    {
        return new ReceiptRequest
        {
            cbTerminalID = "1",
            Currency = Currency.EUR,
            cbReceiptMoment = DateTime.UtcNow,
            cbReceiptReference = Guid.NewGuid().ToString(),
            ftPosSystemId = Guid.NewGuid(),
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(receiptCase),
            cbChargeItems = chargeItems,
            cbPayItems = new List<PayItem>
            {
                new PayItem
                {
                    Position = 1,
                    Amount = chargeItems.Sum(x => x.Amount),
                    ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                    Description = "Cash"
                }
            }
        };
    }

    // ── NotOwnSales + special taxes ─────────────────────────────────

    [Fact]
    public void Validate_NotOwnSales_WithSpecialTaxItems_ShouldPass()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(100, 24, ChargeItemCaseTypeOfService.NotOwnSales),
            CreateChargeItem(20, 24, (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_NotOwnSales_MixedWithDelivery_ShouldFail()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(100, 24, ChargeItemCaseTypeOfService.NotOwnSales),
            CreateChargeItem(50, 24, ChargeItemCaseTypeOfService.Delivery)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorMessage.Should().Contain("NotOwnSales");
    }

    // ── OwnConsumption + special taxes ──────────────────────────────

    [Fact]
    public void Validate_OwnConsumption_WithSpecialTaxItems_ShouldPass()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(100, 24, ChargeItemCaseTypeOfService.OwnConsumption),
            CreateChargeItem(20, 24, (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);
        receiptRequest.cbCustomer = new MiddlewareCustomer
        {
            CustomerVATId = "026883248",
            CustomerName = "Test",
            CustomerStreet = "Street",
            CustomerZip = "12345",
            CustomerCity = "Athens",
            CustomerCountry = "GR"
        };

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_OwnConsumption_MixedWithDelivery_ShouldFail()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(100, 24, ChargeItemCaseTypeOfService.OwnConsumption),
            CreateChargeItem(50, 24, ChargeItemCaseTypeOfService.Delivery)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorMessage.Should().Contain("OwnConsumption");
    }

    // ── Zero VATAmount with non-zero VATRate (market-gr#309) ────────

    [Fact]
    public void Validate_ZeroVatAmount_WithNormalVatRate_ShouldFail()
    {
        var chargeItem = CreateChargeItem(1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        chargeItem.Quantity = 0;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("ZeroVatAmountWithNonZeroVatRate");
        error.ErrorMessage.Should().Contain("position 1").And.Contain("VATRate is 24");
    }

    [Fact]
    public void Validate_ZeroVatAmount_OnRefundLine_ShouldFail()
    {
        var chargeItem = CreateChargeItem(-1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("ZeroVatAmountWithNonZeroVatRate");
    }

    [Fact]
    public void Validate_ZeroVatAmount_OnHandWrittenReceipt_ShouldFail()
    {
        var chargeItem = CreateChargeItem(1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        var receiptRequest = CreateReceipt([chargeItem]);
        receiptRequest.ftReceiptCase = receiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.HandWritten);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("ZeroVatAmountWithNonZeroVatRate");
    }

    [Theory]
    [InlineData(10, 1.90)] // non-zero deviation from 1.94 is not this rule's concern (e.g. per-unit rounding)
    [InlineData(1.24, 0.24)]
    [InlineData(-1.24, -0.24)] // refund / negative line
    public void Validate_NonZeroVatAmount_ShouldPass(decimal amount, decimal vatAmount)
    {
        var chargeItem = CreateChargeItem(amount, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = vatAmount;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 24)] // free item
    [InlineData(0.01, 6)] // VAT 0.0006 rounds to 0.00
    [InlineData(0.02, 24)] // VAT 0.0039 rounds to 0.00
    public void Validate_ZeroVatAmount_WhenCalculatedVatRoundsToZero_ShouldPass(decimal amount, int vatRate)
    {
        var chargeItem = CreateChargeItem(amount, vatRate, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_VatAmountNotProvided_ShouldPass()
    {
        var chargeItem = CreateChargeItem(1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = null;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_ZeroVatAmount_WithZeroVatRate_ShouldPass()
    {
        var chargeItem = CreateChargeItem(1.24m, 0, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    // ── Final invoice rows (after line-level overrides) ─────────────

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 100)]
    [InlineData(9, 100)]
    [InlineData(10, 100)]
    [InlineData(1, -100)] // refund row
    public void ValidateInvoiceDetails_ZeroVatAmount_OnNonZeroRateCategory_ShouldFail(int vatCategory, decimal netValue)
    {
        var rows = new[] { new InvoiceRowType { lineNumber = 3, vatCategory = vatCategory, netValue = netValue, vatAmount = 0 } };

        var (valid, error) = ValidationGR.ValidateInvoiceDetails(rows);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("ZeroVatAmountWithNonZeroVatCategory");
        error.ErrorMessage.Should().Contain("Invoice row 3");
    }

    [Theory]
    [InlineData(7, 100, 0)]      // 0% excluding VAT
    [InlineData(8, 100, 0)]      // registrations without VAT (e.g. 8.6 void)
    [InlineData(1, 0, 0)]        // zero net value (e.g. 8.2 special tax rows)
    [InlineData(3, 0.05, 0)]     // 6% of 0.05 rounds to 0.00
    [InlineData(1, 100, 24)]
    public void ValidateInvoiceDetails_ValidRows_ShouldPass(int vatCategory, decimal netValue, decimal vatAmount)
    {
        var rows = new[] { new InvoiceRowType { lineNumber = 1, vatCategory = vatCategory, netValue = netValue, vatAmount = vatAmount } };

        var (valid, error) = ValidationGR.ValidateInvoiceDetails(rows);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }
}
