using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.QueuePL.Models;
using fiskaltrust.Middleware.Localization.QueuePL.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePL.UnitTest;

internal class PLDeviceUnreachableException : Exception
{
    public PLDeviceUnreachableException(string message) : base(message) { }
}

internal class FakePLSSCD : IPLSSCD
{
    public int ProcessReceiptCalls { get; private set; }
    public bool Fiscalized { get; set; } = true;
    public Exception? ThrowOnProcessReceipt { get; set; }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    public Task<PLSSCDInfo> GetInfoAsync() => Task.FromResult(new PLSSCDInfo
    {
        InfoData = JsonSerializer.Serialize(new { FiscalizationState = Fiscalized ? 2 : 1, UniqueDeviceNumber = "ZAS0000000001" })
    });

    public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        ProcessReceiptCalls++;
        if (ThrowOnProcessReceipt is not null)
        {
            throw ThrowOnProcessReceipt;
        }
        return Task.FromResult(new ProcessResponse { ReceiptResponse = request.ReceiptResponse });
    }
}

internal class FakeLocalizedQueueStorageProvider : ILocalizedQueueStorageProvider
{
    public bool Activated { get; private set; }
    public bool Deactivated { get; private set; }

    public Task ActivateQueueAsync()
    {
        Activated = true;
        return Task.CompletedTask;
    }

    public Task DeactivateQueueAsync()
    {
        Deactivated = true;
        return Task.CompletedTask;
    }
}

public class ProcessorTestsBase
{
    internal static ProcessCommandRequest CreateRequest(ulong receiptCase, List<ChargeItem>? chargeItems = null, string? cbCustomer = null)
    {
        var queue = new ftQueue { ftQueueId = Guid.NewGuid(), ftCashBoxId = Guid.NewGuid() };
        var request = new ReceiptRequest
        {
            ftCashBoxID = queue.ftCashBoxId,
            cbReceiptReference = "unit-test",
            cbReceiptMoment = DateTime.UtcNow,
            ftReceiptCase = (ReceiptCase)receiptCase,
            cbChargeItems = chargeItems ?? new List<ChargeItem>(),
            cbPayItems = new List<PayItem>(),
        };
        if (cbCustomer is not null)
        {
            request.cbCustomer = cbCustomer;
        }
        var response = new ReceiptResponse
        {
            ftQueueID = queue.ftQueueId,
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptIdentification = "ft1#",
            ftCashBoxIdentification = "ZAS0000000001",
        };
        return new ProcessCommandRequest(queue, request, response);
    }
}

public class LifecycleCommandProcessorPLTests : ProcessorTestsBase
{
    [Fact]
    public async Task InitialOperation_ShouldActivateQueue_WhenDeviceIsFiscalized()
    {
        var sscd = new FakePLSSCD { Fiscalized = true };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        storage.Activated.Should().BeTrue();
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.InitialOperationReceipt));
        result.actionJournals.Should().HaveCount(1);
    }

    [Fact]
    public async Task InitialOperation_ShouldFail_WhenDeviceIsNotFiscalized()
    {
        var sscd = new FakePLSSCD { Fiscalized = false };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        storage.Activated.Should().BeFalse();
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task OutOfOperation_ShouldDeactivateQueue()
    {
        var sscd = new FakePLSSCD();
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.OutOfOperationReceipt0x4002Async(CreateRequest(0x504C_2000_0000_4002));

        storage.Deactivated.Should().BeTrue();
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.OutOfOperationReceipt));
    }
}

public class ReceiptCommandProcessorPLTests : ProcessorTestsBase
{
    [Fact]
    public async Task PointOfSaleReceipt_ShouldPassThroughToSCU()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);

        await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
        }));

        sscd.ProcessReceiptCalls.Should().Be(1);
    }

    [Fact]
    public async Task PointOfSaleReceipt_ShouldFail_WhenSaleAndReturnAreMixed()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
            new() { Amount = -5m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
        }));

        sscd.ProcessReceiptCalls.Should().Be(0);
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task DiscountedSale_ShouldNotBeTreatedAsMixedReturn()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);

        await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
            new() { Amount = -2m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0004_0003 },
        }));

        sscd.ProcessReceiptCalls.Should().Be(1);
    }

    [Fact]
    public async Task NipReceipt_ShouldFail_WithCustomerDataButNoVatId()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);
        var receiptCase = 0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.ReceiverIsBusiness;

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(receiptCase, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
        }, cbCustomer: """{"CustomerName":"ACME"}"""));

        sscd.ProcessReceiptCalls.Should().Be(0);
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task NipReceipt_ShouldFail_WithoutCustomerData()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);
        var receiptCase = 0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.ReceiverIsBusiness;

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(receiptCase, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
        }));

        sscd.ProcessReceiptCalls.Should().Be(0);
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task NipReceipt_ShouldPassThrough_WithCustomerData()
    {
        var sscd = new FakePLSSCD();
        var sut = new ReceiptCommandProcessorPL(sscd);
        var receiptCase = 0x504C_2000_0000_0001UL | (ulong)ReceiptCaseFlags.ReceiverIsBusiness;

        await sut.PointOfSaleReceipt0x0001Async(CreateRequest(receiptCase, new List<ChargeItem>
        {
            new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
        }, cbCustomer: /* language=json */ """{"CustomerVATId":"PL5260250274"}"""));

        sscd.ProcessReceiptCalls.Should().Be(1);
    }
}

public class DeviceUnreachableTests : ProcessorTestsBase
{
    private static List<ChargeItem> SingleItem() => new()
    {
        new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
    };

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(PLDeviceUnreachableException))]
    public async Task Receipt_ShouldCarryDeviceUnreachableState_WhenScuIsUnreachable(Type exceptionType)
    {
        var sscd = new FakePLSSCD
        {
            ThrowOnProcessReceipt = (Exception)Activator.CreateInstance(exceptionType, "printer offline")!,
        };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        ((ulong)result.receiptResponse.ftState).Should().Be(0x504C_2001_EEEE_EEEEUL);
    }

    [Fact]
    public async Task Receipt_ShouldLetOtherScuFailuresBubble()
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = new InvalidOperationException("device error 2005") };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var act = () => sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DailyClosing_ShouldCarryDeviceUnreachableState_WhenScuIsUnreachable()
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = new HttpRequestException("connection refused") };
        var sut = new DailyOperationsCommandProcessorPL(sscd);

        var result = await sut.DailyClosing0x2011Async(CreateRequest(0x504C_2000_0000_2011));

        ((ulong)result.receiptResponse.ftState).Should().Be(0x504C_2001_EEEE_EEEEUL);
    }
}

public class InvoiceCommandProcessorPLTests : ProcessorTestsBase
{
    [Fact]
    public async Task Invoice_ShouldBeStoredNotFiscalized_WithoutTouchingTheSCU()
    {
        var sut = new InvoiceCommandProcessorPL();

        var result = await sut.InvoiceB2C0x1001Async(CreateRequest(0x504C_2000_0000_1001));

        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.StoredNotFiscalized));
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
    }
}
