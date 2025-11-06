using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData.Models;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.SCU.GR.MyData;
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

    private ReceiptRequest CreateReceipt(List<ChargeItem> chargeItems, bool isRefund = false, bool hasPreviousReceipt = false)
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

        receiptRequest.cbChargeItems = chargeItems;
        receiptRequest.cbPayItems = [new PayItem
        {
            Position = 1,
            Amount = chargeItems.Sum(x => x.Amount),
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
    public void Item_2_2_GetInvoiceType_B2BInvoice_WithOtherServiceItems_AndSpecialTaxes_WithEUCustomer_ReturnsItem22()
    {
        var receiptRequest = CreateB2BInvoice("DE", ChargeItemCaseTypeOfService.OtherService);
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 30, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0));

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item22);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("GB")]
    [InlineData("CH")]
    [InlineData("JP")]
    [InlineData("CN")]
    public void Item_2_3_GetInvoiceType_B2BInvoice_WithOtherServiceItems_AndSpecialTaxes_WithNonEUCustomer_ReturnsItem23(string country)
    {
        var receiptRequest = CreateB2BInvoice(country, ChargeItemCaseTypeOfService.OtherService);
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 30, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0));

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item23);
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
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithDeliveryAndUnknowns_ReturnsItem111()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.Delivery);
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 50, 24, "Service Item", ChargeItemCaseTypeOfService.UnknownService));

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithMixedTypesAndUnknowns_ReturnsItem111()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.CatalogService);
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 50, 24, "Service Item", ChargeItemCaseTypeOfService.Delivery));
        receiptRequest.cbChargeItems.Add(CreateChargeItem(2, 50, 24, "Service Item", ChargeItemCaseTypeOfService.UnknownService));

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
    public void Item_11_2_GetInvoiceType_RetailReceipt_WithOnlyServiceItems_AndUnknown_ReturnsItem112()
    {
        var receiptRequest = CreateReceipt(new List<ChargeItem>
        {
             CreateChargeItem(1, 100, 24, "Test Item", ChargeItemCaseTypeOfService.OtherService),
             CreateChargeItem(1, 100, 24, "Test Item", ChargeItemCaseTypeOfService.UnknownService)
        });

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item112);
    }

    [Fact]
    public void Item_11_4_GetInvoiceType_RetailReceipt_WithRefund_ReturnsItem114()
    {
        var receiptRequest = CreateReceipt(ChargeItemCaseTypeOfService.Delivery, isRefund: true);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item114);
    }

    [Fact]
    public void Item_11_2_GetInvoiceType_RetailReceipt_WithOnlyServiceItems_AndSpecialTaxes_ReturnsItem112()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 100, 24, "Service Item", ChargeItemCaseTypeOfService.OtherService),
            CreateChargeItem(2, 20, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item112);
    }

    [Fact]
    public void Item_11_2_GetInvoiceType_RetailReceipt_WithMixedServiceAndSpecialTaxes_ReturnsItem112()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 100, 24, "Service Item", ChargeItemCaseTypeOfService.OtherService),
            CreateChargeItem(2, 50, 24, "Unknown Service Item", ChargeItemCaseTypeOfService.UnknownService),
            CreateChargeItem(3, 30, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item112);
    }

    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithDeliveryAndSpecialTaxes_ReturnsItem111()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 100, 24, "Delivery Item", ChargeItemCaseTypeOfService.Delivery),
            CreateChargeItem(2, 20, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithOnlySpecialTaxes_ReturnsItem111()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 20, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0),
            CreateChargeItem(2, 30, 24, "Another Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    [Fact]
    public void Item_11_1_GetInvoiceType_RetailReceipt_WithMixedDeliveryServiceAndSpecialTaxes_ReturnsItem111()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 100, 24, "Delivery Item", ChargeItemCaseTypeOfService.Delivery),
            CreateChargeItem(2, 50, 24, "Service Item", ChargeItemCaseTypeOfService.OtherService),
            CreateChargeItem(3, 20, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item111);
    }

    [Fact]
    public void Item_11_2_GetInvoiceType_RetailReceipt_WithServiceUnknownAndSpecialTaxes_ReturnsItem112()
    {
        var chargeItems = new List<ChargeItem>
        {
            CreateChargeItem(1, 100, 24, "Service Item", ChargeItemCaseTypeOfService.OtherService),
            CreateChargeItem(2, 50, 24, "Unknown Item", ChargeItemCaseTypeOfService.UnknownService),
            CreateChargeItem(3, 30, 24, "Special Tax Item", (ChargeItemCaseTypeOfService) 0xF0)
        };
        var receiptRequest = CreateReceipt(chargeItems);

        var result = AADEMappings.GetInvoiceType(receiptRequest);

        result.Should().Be(InvoiceType.Item112);
    }
}
