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
}
