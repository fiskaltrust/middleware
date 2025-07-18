using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest.SCU.MyData;

public class AADEMappingsInvoiceTypeTests
{
    private readonly ReceiptRequest _baseRequest = new ReceiptRequest
    {
        cbTerminalID = "1",
        Currency = Currency.EUR,
        cbReceiptMoment = DateTime.UtcNow,
        cbReceiptReference = Guid.NewGuid().ToString(),
        ftPosSystemId = Guid.NewGuid()
    };

    private ChargeItem CreateChargeItem(int position, decimal amount, int vatRate, string description, ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery)
    {
        return new ChargeItem
        {
            Position = position,
            Amount = amount,
            VATRate = vatRate,
            VATAmount = decimal.Round(amount / (100M + vatRate) * vatRate, 2, MidpointRounding.ToEven),
            ftChargeItemCase = ((ChargeItemCase) 0x4752_2000_0000_0000).WithTypeOfService(typeOfService).WithVat(ChargeItemCase.NormalVatRate),
            Quantity = 1,
            Description = description
        };
    }
    
    private ReceiptRequest CreateReceipt(ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery, bool isRefund = false, bool hasPreviousReceipt = false)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = _baseRequest.cbTerminalID,
            Currency = _baseRequest.Currency,
            cbReceiptMoment = _baseRequest.cbReceiptMoment,
            cbReceiptReference = _baseRequest.cbReceiptReference,
            ftPosSystemId = _baseRequest.ftPosSystemId,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.PointOfSaleReceipt0x0001)
        };

        if (isRefund)
        {
            receiptRequest.ftReceiptCase = receiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        }

        if (hasPreviousReceipt)
        {
            receiptRequest.cbPreviousReceiptReference = "PREV12345";
        }

        receiptRequest.cbChargeItems = [CreateChargeItem(1, isRefund ? -100 : 100, 24, "Test Item", typeOfService)];
        receiptRequest.cbPayItems = [new PayItem
        {
            Position = 1,
            Amount = isRefund ? -100 : 100,
            ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
            Description = "Cash"
        }];

        return receiptRequest;
    }

    private ReceiptRequest CreateB2BInvoice(string customerCountry, ChargeItemCaseTypeOfService typeOfService = ChargeItemCaseTypeOfService.Delivery, bool isRefund = false, bool hasPreviousReceipt = false)
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = _baseRequest.cbTerminalID,
            Currency = _baseRequest.Currency,
            cbReceiptMoment = _baseRequest.cbReceiptMoment,
            cbReceiptReference = _baseRequest.cbReceiptReference,
            ftPosSystemId = _baseRequest.ftPosSystemId,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.InvoiceB2B0x1002)
        };

        if (isRefund)
        {
            receiptRequest.ftReceiptCase = receiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Refund);
        }

        if (hasPreviousReceipt)
        {
            receiptRequest.cbPreviousReceiptReference = "PREV12345";
        }

        receiptRequest.cbChargeItems = [CreateChargeItem(1, isRefund ? -100 : 100, 24, "Test Item", typeOfService)];
        receiptRequest.cbCustomer = new MiddlewareCustomer
        {
            CustomerVATId = $"{customerCountry}12345678",
            CustomerName = $"{customerCountry} Test Company Ltd",
            CustomerStreet = "Test Street 1",
            CustomerCity = "Test City",
            CustomerZip = "12345",
            CustomerCountry = customerCountry
        };

        return receiptRequest;
    }

    [Fact]
    public void Item_1_1_GetInvoiceType_B2BInvoice_WithGreekCustomer_ReturnsItem11()
    {
        var receiptRequest = CreateB2BInvoice("GR");

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item11);
    }

    [Theory]
    [InlineData("AT")]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("IT")]
    [InlineData("ES")]
    public void Item_1_2_GetInvoiceType_B2BInvoice_WithEUCustomer_ReturnsItem12(string country)
    {
        var receiptRequest = CreateB2BInvoice(country);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item12);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("CH")]
    [InlineData("JP")]
    [InlineData("CN")]
    public void Item_1_3_GetInvoiceType_B2BInvoice_WithNonEUCustomer_ReturnsItem13(string country)
    {
        var receiptRequest = CreateB2BInvoice(country);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item13);
    }

    [Fact]
    public void Item_1_4_GetInvoiceType_B2BInvoice_WithNotOwnSalesItems_ReturnsItem14()
    {
        var receiptRequest = CreateB2BInvoice("GR", ChargeItemCaseTypeOfService.NotOwnSales);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item14);
    }

    [Fact]
    public void Item_1_5_GetInvoiceType_B2BInvoice_WithReceivableItems_ReturnsItem15()
    {
        var receiptRequest = CreateB2BInvoice("GR", ChargeItemCaseTypeOfService.Receivable);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item15);
    }

    [Fact]
    public void Item_1_6_GetInvoiceType_B2BInvoice_WithGreekCustomer_WithPreviousReceipt_ReturnsItem16()
    {
        var receiptRequest = CreateB2BInvoice("GR", hasPreviousReceipt: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item16);
    }

    [Fact]
    public void Item_2_1_GetInvoiceType_B2BInvoice_WithOtherServiceItems_WithGreekCustomer_ReturnsItem21()
    {
        var receiptRequest = CreateB2BInvoice("GR", ChargeItemCaseTypeOfService.OtherService);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item21);
    }

    [Theory]
    [InlineData("AT")]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("IT")]
    [InlineData("ES")]
    public void Item_2_2_GetInvoiceType_B2BInvoice_WithOtherServiceItems_WithEUCustomer_ReturnsItem22(string country)
    {
        var receiptRequest = CreateB2BInvoice(country, ChargeItemCaseTypeOfService.OtherService);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item22);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("CH")]
    [InlineData("JP")]
    [InlineData("CN")]
    public void Item_2_3_GetInvoiceType_B2BInvoice_WithOtherServiceItems_WithNonEUCustomer_ReturnsItem23(string country)
    {
        var receiptRequest = CreateB2BInvoice(country, ChargeItemCaseTypeOfService.OtherService);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item23);
    }

    [Fact]
    public void Item_2_4_GetInvoiceType_B2BInvoice_WithOtherServiceItems_WithPreviousReceipt_ReturnsItem24()
    {
        var receiptRequest = CreateB2BInvoice("GR", ChargeItemCaseTypeOfService.OtherService, hasPreviousReceipt: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item24);
    }

    [Fact]
    public void Item_5_1_GetInvoiceType_B2BInvoice_WithGreekCustomer_IsRefund_WithPreviousReceipt_ReturnsItem51()
    {
        var receiptRequest = CreateB2BInvoice("GR", isRefund: true, hasPreviousReceipt: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item51);
    }

    [Fact]
    public void Item_5_2_GetInvoiceType_B2BInvoice_WithGreekCustomer_IsRefund_WithoutPreviousReceipt_ReturnsItem52()
    {
        var receiptRequest = CreateB2BInvoice("GR", isRefund: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item52);
    }
    
    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithGoodsItems_ReturnsItem111()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.Delivery);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithMixedItems_ReturnsItem111()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.Delivery);
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 50, 24, "Service Item", ChargeItemCaseTypeOfService.OtherService));

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }
    
    [Fact]
    public void Item_11_2_GetInvoiceType_RetailReceipt_WithOnlyServiceItems_ReturnsItem112()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.OtherService);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item112);
    }
    
    [Fact]
    public void Item_11_3_GetInvoiceType_DeliveryNote_ReturnsItem93()
    {
        var receiptRequest = new ReceiptRequest
        {
            cbTerminalID = _baseRequest.cbTerminalID,
            Currency = _baseRequest.Currency,
            cbReceiptMoment = _baseRequest.cbReceiptMoment,
            cbReceiptReference = _baseRequest.cbReceiptReference,
            ftPosSystemId = _baseRequest.ftPosSystemId,
            ftReceiptCase = ((ReceiptCase) 0x4752_2000_0000_0000).WithCase(ReceiptCase.DeliveryNote0x0005),
            cbChargeItems = [CreateChargeItem(1, 100, 24, "Test Item", ChargeItemCaseTypeOfService.Delivery)],
            cbPayItems = [new PayItem
            {
                Position = 1,
                Amount = 100,
                ftPayItemCase = ((PayItemCase) 0x4752_2000_0000_0000).WithCase(PayItemCase.CashPayment),
                Description = "Cash"
            }]
        };

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item93);
    }
    
    [Fact]
    public void Item_11_4_GetInvoiceType_RetailReceipt_WithRefund_ReturnsItem114()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.Delivery, isRefund: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item114);
    }
    
    [Fact]
    public void Item_11_5_GetInvoiceType_RetailReceipt_WithAgencyItems_ReturnsItem115()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.NotOwnSales);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item115);
    }
}
