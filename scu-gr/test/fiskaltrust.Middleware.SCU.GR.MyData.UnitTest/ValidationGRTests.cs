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

    // ── VATAmount consistency (market-gr#309) ───────────────────────

    [Fact]
    public void Validate_ZeroVatAmount_WithNormalVatRate_ShouldFail()
    {
        var chargeItem = CreateChargeItem(1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        chargeItem.Quantity = 0;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("VatAmountMismatch");
        error.ErrorMessage.Should().Contain("0.24");
    }

    [Theory]
    [InlineData(10, 1.94)] // calculated 1.9355, rounded
    [InlineData(10, 1.93)] // within 0.01 tolerance
    [InlineData(-1.24, -0.24)] // refund / negative line
    public void Validate_VatAmountWithinTolerance_ShouldPass(decimal amount, decimal vatAmount)
    {
        var chargeItem = CreateChargeItem(amount, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = vatAmount;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_VatAmountOutsideTolerance_ShouldFail()
    {
        var chargeItem = CreateChargeItem(10, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 1.90m;
        var receiptRequest = CreateReceipt([chargeItem]);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeFalse();
        error!.ErrorCode.Should().Be("VatAmountMismatch");
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

    [Fact]
    public void Validate_ZeroVatAmount_OnHandWrittenReceipt_ShouldBeSkipped()
    {
        var chargeItem = CreateChargeItem(1.24m, 24, ChargeItemCaseTypeOfService.Delivery);
        chargeItem.VATAmount = 0;
        var receiptRequest = CreateReceipt([chargeItem]);
        receiptRequest.ftReceiptCase = receiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.HandWritten);

        var (valid, error) = ValidationGR.ValidateReceiptRequest(receiptRequest);

        valid.Should().BeTrue();
        error.Should().BeNull();
    }
}
