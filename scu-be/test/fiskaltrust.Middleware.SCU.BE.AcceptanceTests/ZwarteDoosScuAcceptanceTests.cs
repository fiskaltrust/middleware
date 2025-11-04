using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.ZwartedoosApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.BE.AcceptanceTests;

[Collection("ZwarteDoosScuTests")]
public class ZwarteDoosScuAcceptanceTests : IBESSCDAcceptanceTests
{
    public ZwarteDoosScuAcceptanceTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override IBESSCD GetSystemUnderTest(Dictionary<string, object>? configuration = null)
    {
        var config = new Dictionary<string, object> 
        {
            ["BaseUrl"] = "https://sdk.zwartedoos.be",
            ["SharedSecret"] = "6fab7067-bc9e-45fa-bd76-93ed1d1fde3b",
            ["DeviceId"] = "FDM02030462",
            ["CompanyId"] = configuration?.GetValueOrDefault("CompanyId", "test-company") as string ?? "test-company",
            ["TimeoutSeconds"] = 30,
            ["Language"] = Language.DE,
            ["VatNo"] = "BE0000000097",
            ["EstNo"] = "2000000042",
        };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var bootStrapper = new ScuBootstrapper
        {
            Id = Guid.NewGuid(),
            Configuration = config
        };
        bootStrapper.ConfigureServices(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();     
        return serviceProvider.GetRequiredService<IBESSCD>();
    }

    [Fact]
    public async Task ProcessReceiptAsync_BelgianSpecificCases_ShouldSucceed()
    {       
        var scu = GetSystemUnderTest();
        
        var request = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems[0].ftChargeItemCase = (ChargeItemCase)0x4245000000000001;
        
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        var result = await scu.ProcessReceiptAsync(processRequest);

        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    [Fact]
    public async Task ProcessReceiptAsync_BelgianVATRates_ShouldSucceed()
    {
        var scu = GetSystemUnderTest();

        var request = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 1,
                Description = "Standard VAT Item",
                Quantity = 1.0m,
                Amount = 12.10m,
                VATRate = 21.0m, 
                ftChargeItemCase = (ChargeItemCase)0x4245000000000001 
            },
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 2,
                Description = "Reduced VAT Item",
                Quantity = 1.0m,
                Amount = 11.20m,
                VATRate = 12.0m, 
                ftChargeItemCase = (ChargeItemCase)0x4245000000000002 
            },
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 3,
                Description = "Super Reduced VAT Item",
                Quantity = 1.0m,
                Amount = 10.60m,
                VATRate = 6.0m, 
                ftChargeItemCase = (ChargeItemCase)0x4245000000000003 
            }
        };

        var totalAmount = request.cbChargeItems.Sum(x => x.Amount);
        request.cbPayItems = new List<PayItem>
        {
            new PayItem
            {
                ftPayItemId = Guid.NewGuid(),
                Position = 1,
                Description = "Cash Payment",
                Quantity = 1.0m,
                Amount = totalAmount,
                ftPayItemCase = (PayItemCase)0x4245000000000001 
            }
        };

        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        var result = await scu.ProcessReceiptAsync(processRequest);

        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    [Fact]
    public void CreateInstance_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        var invalidConfig = new Dictionary<string, object>
        {
            { "ServiceUrl", "" }, // Empty endpoint
            { "ApiKey", null! } // Null API key
        };
        var scu = GetSystemUnderTest(invalidConfig);
        scu.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInfoAsync_ShouldReturnZwarteDoosSpecificInfo()
    {
        var scu = GetSystemUnderTest();
        
        var info = await scu.GetInfoAsync();
        
        info.Should().NotBeNull();
    }
}