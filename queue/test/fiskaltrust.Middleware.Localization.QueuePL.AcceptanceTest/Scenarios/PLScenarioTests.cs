using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueuePL.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePL.AcceptanceTest.Scenarios;

public class PLScenarioTests
{
    private readonly Func<string, Task<string>> _signProcessor;
    private readonly MockPLSSCD _sscd;
    private readonly Guid _queueId;
    private readonly Guid _cashBoxId;

    public PLScenarioTests() : this(startQueue: true) { }

    private PLScenarioTests(bool startQueue)
    {
        _queueId = Guid.NewGuid();
        _cashBoxId = Guid.NewGuid();
        _sscd = new MockPLSSCD();

        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox
                {
                    ftCashBoxId = _cashBoxId,
                    TimeStamp = DateTime.UtcNow.Ticks
                })
            },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
                {
                    new ftQueue
                    {
                        ftQueueId = _queueId,
                        ftCashBoxId = _cashBoxId,
                        StartMoment = startQueue ? DateTime.UtcNow : null,
                        CountryCode = "PL"
                    }
                })
            },
            { "init_ftQueuePL", JsonSerializer.Serialize(new List<ftQueuePL>
                {
                    new ftQueuePL
                    {
                        ftQueuePLId = _queueId,
                        CashBoxIdentification = MockPLSSCD.UniqueDeviceNumber
                    }
                })
            },
            { "init_ftSignaturCreationUnitPL", JsonSerializer.Serialize(new List<ftSignaturCreationUnitPL>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration
                {
                    Account = new AccountMasterData
                    {
                        AccountId = Guid.NewGuid(),
                        AccountName = "fiskaltrust sp. z o.o.",
                        VatId = "5260250274",
                        Street = "ul. Przykładowa 1",
                        Zip = "00-001",
                        City = "Warszawa",
                        Country = "PL",
                        TaxId = "5260250274"
                    }
                })
            }
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        var bootstrapper = new QueuePLBootstrapper(_queueId, loggerFactory, configuration, _sscd, storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var responseJson = await _signProcessor(JsonSerializer.Serialize(request));
        var response = JsonSerializer.Deserialize<ReceiptResponse>(responseJson);
        response.Should().NotBeNull();
        return response!;
    }

    private ReceiptRequest CreateRequest(ulong receiptCase, List<ChargeItem>? chargeItems = null, List<PayItem>? payItems = null, string? cbCustomer = null)
    {
        var request = new ReceiptRequest
        {
            ftCashBoxID = _cashBoxId,
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCase = (ReceiptCase)receiptCase,
            cbChargeItems = chargeItems ?? new List<ChargeItem>(),
            cbPayItems = payItems ?? new List<PayItem>(),
        };
        if (cbCustomer is not null)
        {
            request.cbCustomer = cbCustomer;
        }
        return request;
    }

    private static List<ChargeItem> Item(decimal amount, decimal vatRate = 23m, ulong vatCase = 0x3) => new()
    {
        new ChargeItem
        {
            Amount = amount,
            VATRate = vatRate,
            Quantity = 1,
            Description = "Item A",
            ftChargeItemCase = (ChargeItemCase)(0x504C_2000_0000_0000 | vatCase),
        }
    };

    private static List<PayItem> Cash(decimal amount) => new()
    {
        new PayItem
        {
            Amount = amount,
            Description = "Cash",
            ftPayItemCase = (PayItemCase)0x504C_2000_0000_0000,
        }
    };

    [Fact]
    public async Task InitialOperation_ShouldActivateQueue_AgainstFiscalizedDevice()
    {
        var harness = new PLScenarioTests(startQueue: false);
        var response = await harness.SignAsync(harness.CreateRequest(0x504C_2000_0000_4001));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE, response.ftSignatures.FirstOrDefault()?.Data);
        response.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.InitialOperationReceipt));
    }

    [Fact]
    public async Task CashSale_ShouldGetFiscalDocumentNumber_AndDeviceIdentity()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0000_0001, Item(10m), Cash(10m)));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        response.ftCashBoxIdentification.Should().Be(MockPLSSCD.UniqueDeviceNumber);
        response.ftReceiptIdentification.Should().EndWith("1");
        response.ftSignatures.Should().Contain(x => (ulong)x.ftSignatureType == 0x504C_2000_0000_0101);
    }

    [Fact]
    public async Task Return_ShouldPassThrough_AsOwnDocument()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0100_0001, Item(-10m), Cash(-10m)));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        _sscd.ProcessReceiptCalls.Should().Be(1);
    }

    [Fact]
    public async Task MixedSaleAndReturn_ShouldFail()
    {
        var chargeItems = Item(10m);
        chargeItems.AddRange(Item(-5m));

        var response = await SignAsync(CreateRequest(0x504C_2000_0000_0001, chargeItems, Cash(5m)));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
        _sscd.ProcessReceiptCalls.Should().Be(0);
    }

    [Fact]
    public async Task NipReceipt_ShouldSucceed_WithCustomerNip()
    {
        var receiptCase = 0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.ReceiverIsBusiness;
        var response = await SignAsync(CreateRequest(receiptCase, Item(100m), Cash(100m), cbCustomer: """{"CustomerVATId":"PL5260250274"}"""));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
    }

    [Fact]
    public async Task NipReceipt_ShouldFail_WithoutCustomerNip()
    {
        var receiptCase = 0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.ReceiverIsBusiness;
        var response = await SignAsync(CreateRequest(receiptCase, Item(100m), Cash(100m)));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task ZeroReceipt_ShouldQueryDeviceStatus()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0000_2000));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        _sscd.ProcessReceiptCalls.Should().Be(1);
        response.ftCashBoxIdentification.Should().Be(MockPLSSCD.UniqueDeviceNumber);
    }

    [Fact]
    public async Task DailyClosing_ShouldTriggerZReportOnDevice()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0000_2011));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        _sscd.ProcessReceiptCalls.Should().Be(1);
    }

    [Fact]
    public async Task Invoice_ShouldBeStoredNotFiscalized_WithoutDeviceCall()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0000_1001, Item(100m), Cash(100m)));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        _sscd.ProcessReceiptCalls.Should().Be(0);
        response.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.StoredNotFiscalized));
    }

    [Fact]
    public async Task OutOfOperation_ShouldDisableQueue()
    {
        var response = await SignAsync(CreateRequest(0x504C_2000_0000_4002));

        ((ulong)response.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        response.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.OutOfOperationReceipt));
    }
}
