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

// SCU packages subclass their unreachable exception (e.g. the PosNet SCU's ambiguous-response
// exception) — the name-based detection must walk the type hierarchy.
internal class DerivedDeviceUnreachableException : PLDeviceUnreachableException
{
    public DerivedDeviceUnreachableException(string message) : base(message) { }
}

internal class FakePLSSCD : IPLSSCD
{
    public int ProcessReceiptCalls { get; private set; }
    public bool Fiscalized { get; set; } = true;
    public Exception? ThrowOnProcessReceipt { get; set; }

    public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

    /// <summary>Returned verbatim when set — for the blob shapes a well-behaved SCU would never send.</summary>
    public PLSSCDInfo? InfoOverride { get; set; }

    public Exception? ThrowOnGetInfo { get; set; }

    public Task<PLSSCDInfo> GetInfoAsync()
    {
        if (ThrowOnGetInfo is not null)
        {
            throw ThrowOnGetInfo;
        }
        return Task.FromResult(InfoOverride ?? new PLSSCDInfo
        {
            InfoData = JsonSerializer.Serialize(new { FiscalizationState = Fiscalized ? 2 : 1, UniqueDeviceNumber = "ZAS0000000001" })
        });
    }

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

    /// <summary>
    /// The queue reads FiscalizationState out of the InfoData blob as a JSON number. Anything it
    /// cannot read that way — a string-encoded enum, a missing property, a malformed blob — must
    /// leave the queue deactivated rather than activate it on a guess. The guard that the SCU keeps
    /// producing the numeric form lives on the SCU side, in PLDeviceInfoTests.
    /// </summary>
    [Theory]
    [InlineData("""{"FiscalizationState":"Fiscalized"}""")]
    [InlineData("""{"FiscalizationState":null}""")]
    [InlineData("{}")]
    [InlineData("not json at all")]
    public async Task InitialOperation_ShouldFail_WhenTheDeviceInfoCannotBeRead(string infoData)
    {
        var sscd = new FakePLSSCD { InfoOverride = new PLSSCDInfo { InfoData = infoData } };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        storage.Activated.Should().BeFalse();
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task InitialOperation_ShouldFail_WhenTheDeviceReportsNoInfoAtAll()
    {
        var sscd = new FakePLSSCD { InfoOverride = new PLSSCDInfo() };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        storage.Activated.Should().BeFalse();
        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task InitialOperation_ShouldCarryDeviceUnreachableState_WhenTheRegisterCannotBeRead()
    {
        var sscd = new FakePLSSCD { ThrowOnGetInfo = new PLDeviceUnreachableException("printer offline") };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        // Whether the register is fiscalized is unknown while it cannot be reached — the caller has
        // to see that this is a connectivity failure it can retry, not a wrong device state.
        storage.Activated.Should().BeFalse();
        ((ulong)result.receiptResponse.ftState).Should().Be(0x504C_2001_EEEE_EEEEUL);
        result.actionJournals.Should().HaveCount(1);
    }

    [Fact]
    public async Task InitialAndOutOfOperationSignatures_AreDistinguishable()
    {
        var sut = new LifecycleCommandProcessorPL(new FakePLSSCD(), new FakeLocalizedQueueStorageProvider());

        var started = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));
        var stopped = await sut.OutOfOperationReceipt0x4002Async(CreateRequest(0x504C_2000_0000_4002));

        // One signature type per lifecycle event: sharing a value makes the two indistinguishable
        // for anyone reading the journal.
        started.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.InitialOperationReceipt));
        started.receiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypePL.OutOfOperationReceipt));
        stopped.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.OutOfOperationReceipt));
        stopped.receiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypePL.InitialOperationReceipt));
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
    [InlineData(typeof(DerivedDeviceUnreachableException))]
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
        // The action journal of a closing records that the report was printed and the counters
        // advanced — after an unreachable register it would claim a Z report that never ran.
        result.actionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task MonthlyClosing_ShouldNotWriteAnActionJournal_WhenScuIsUnreachable()
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = new HttpRequestException("connection refused") };
        var sut = new DailyOperationsCommandProcessorPL(sscd);

        var result = await sut.MonthlyClosing0x2012Async(CreateRequest(0x504C_2000_0000_2012));

        ((ulong)result.receiptResponse.ftState).Should().Be(0x504C_2001_EEEE_EEEEUL);
        result.actionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task DailyClosing_ShouldWriteTheActionJournal_WhenTheReportWasPrinted()
    {
        var sut = new DailyOperationsCommandProcessorPL(new FakePLSSCD());

        var result = await sut.DailyClosing0x2011Async(CreateRequest(0x504C_2000_0000_2011));

        result.actionJournals.Should().ContainSingle().Which.Message.Should().Contain("Daily-Closing");
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
