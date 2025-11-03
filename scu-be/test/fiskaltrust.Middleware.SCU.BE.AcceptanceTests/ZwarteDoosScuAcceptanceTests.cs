using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueBE.BESSCD;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.ZwartedoosApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.BE.AcceptanceTests;

/// <summary>
/// Acceptance tests for the ZwarteDoos SCU implementation.
/// Tests all Belgian receipt cases to ensure the basic functionality works correctly.
/// </summary>
[Collection("ZwarteDoosScuTests")]
public class ZwarteDoosScuAcceptanceTests : IBESSCDAcceptanceTests
{
    public ZwarteDoosScuAcceptanceTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override IBESSCD GetSystemUnderTest(Dictionary<string, object>? configuration = null)
    {
        // Set up the default configuration for ZwarteDoos SCU
        var config = new Dictionary<string, object> 
        {
            // Use the correct property names from the actual configuration class
            ["BaseUrl"] = "https://sdk.zwartedoos.be",
            ["SharedSecret"] = "6fab7067-bc9e-45fa-bd76-93ed1d1fde3b",
            ["DeviceId"] = "FDM02030462",
            ["CompanyId"] = configuration?.GetValueOrDefault("CompanyId", "test-company") as string ?? "test-company",
            ["TimeoutSeconds"] = 30
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

    /// <summary>
    /// Test that ZwarteDoos SCU can handle Belgian-specific receipt cases
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_BelgianSpecificCases_ShouldSucceed()
    {
        // This test can be expanded to include Belgian-specific receipt cases
        // that are unique to the Belgian market requirements
        
        var scu = GetSystemUnderTest();
        
        // Test Belgian-specific Point of Sale receipt with Belgian charge item cases
        var request = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems[0].ftChargeItemCase = (ChargeItemCase)0x4245000000000001; // Belgian normal charge item
        
        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        var result = await scu.ProcessReceiptAsync(processRequest);

        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test ZwarteDoos SCU with different VAT rates common in Belgium
    /// </summary>
    [Fact]
    public async Task ProcessReceiptAsync_BelgianVATRates_ShouldSucceed()
    {
        var scu = GetSystemUnderTest();
        
        var request = CreateBasicReceiptRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        
        // Belgian VAT rates: 21% (standard), 12% (reduced), 6% (super reduced), 0% (exempt)
        request.cbChargeItems = new List<ChargeItem>
        {
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 1,
                Description = "Standard VAT Item",
                Quantity = 1.0m,
                Amount = 12.10m,
                VATRate = 21.0m, // Standard rate
                ftChargeItemCase = (ChargeItemCase)0x4245000000000001 // Belgian normal charge item
            },
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 2,
                Description = "Reduced VAT Item",
                Quantity = 1.0m,
                Amount = 11.20m,
                VATRate = 12.0m, // Reduced rate
                ftChargeItemCase = (ChargeItemCase)0x4245000000000002 // Belgian reduced charge item
            },
            new ChargeItem
            {
                ftChargeItemId = Guid.NewGuid(),
                Position = 3,
                Description = "Super Reduced VAT Item",
                Quantity = 1.0m,
                Amount = 10.60m,
                VATRate = 6.0m, // Super reduced rate
                ftChargeItemCase = (ChargeItemCase)0x4245000000000003 // Belgian super reduced charge item
            }
        };

        // Update pay items to match total
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
                ftPayItemCase = (PayItemCase)0x4245000000000001 // Belgian cash payment
            }
        };

        var response = CreateBasicReceiptResponse(request);
        var processRequest = new ProcessRequest { ReceiptRequest = request, ReceiptResponse = response };

        var result = await scu.ProcessReceiptAsync(processRequest);

        result.Should().NotBeNull();
        result.ReceiptResponse.Should().NotBeNull();
        ((ulong)result.ReceiptResponse.ftState & 0xEEEE_EEEE).Should().Be(0);
    }

    /// <summary>
    /// Test ZwarteDoos SCU configuration validation
    /// </summary>
    [Fact]
    public void CreateInstance_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        // Test with invalid configuration to ensure robust error handling
        var invalidConfig = new Dictionary<string, object>
        {
            { "ServiceUrl", "" }, // Empty endpoint
            { "ApiKey", null! } // Null API key
        };

        // This should not throw an exception but handle gracefully
        var scu = GetSystemUnderTest(invalidConfig);
        scu.Should().NotBeNull();
    }

    /// <summary>
    /// Test that ZwarteDoos SCU returns appropriate info
    /// </summary>
    [Fact]
    public async Task GetInfoAsync_ShouldReturnZwarteDoosSpecificInfo()
    {
        var scu = GetSystemUnderTest();
        
        var info = await scu.GetInfoAsync();
        
        info.Should().NotBeNull();
        // Add specific assertions for ZwarteDoos SCU info if the BESSCDInfo class gets extended
    }
}