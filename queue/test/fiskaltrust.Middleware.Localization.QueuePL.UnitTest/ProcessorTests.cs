using System.Net.Sockets;
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

// Stands in for the SCU abstraction's exception, which GetInfoAsync may still throw.
internal class PLDeviceUnreachableException : Exception
{
    public PLDeviceUnreachableException(string message) : base(message) { }
}

internal class FakePLSSCD : IPLSSCD
{
    public int ProcessReceiptCalls { get; private set; }
    public bool Fiscalized { get; set; } = true;
    public Exception? ThrowOnProcessReceipt { get; set; }

    /// <summary>When set, the SCU answers with an error response carrying this message — how a PL SCU reports failures.</summary>
    public string? ErrorMessage { get; set; }

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
        if (ErrorMessage is not null)
        {
            request.ReceiptResponse.SetReceiptResponseError(ErrorMessage);
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
    public async Task InitialOperation_ShouldActivateQueue_WhenDeviceIsNotFiscalizedInSandbox()
    {
        var sscd = new FakePLSSCD { Fiscalized = false };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage, isSandbox: true);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        storage.Activated.Should().BeTrue();
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypePL.InitialOperationReceipt));
    }

    /// <summary>
    /// Sandbox only relaxes an explicit "not fiscalized": a state the queue cannot read is still
    /// unverified, so a broken or misbehaving SCU is caught in sandbox just as in production.
    /// </summary>
    [Theory]
    [InlineData("""{"FiscalizationState":"NonFiscal"}""")]
    [InlineData("""{"FiscalizationState":0}""")]
    [InlineData("""{"FiscalizationState":3}""")]
    [InlineData("{}")]
    [InlineData("not json at all")]
    public async Task InitialOperation_ShouldFailInSandbox_WhenTheDeviceDoesNotReportNonFiscal(string infoData)
    {
        var sscd = new FakePLSSCD { InfoOverride = new PLSSCDInfo { InfoData = infoData } };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage, isSandbox: true);

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
    public async Task InitialOperation_ShouldFail_WhenTheRegisterCannotBeRead()
    {
        var sscd = new FakePLSSCD { ThrowOnGetInfo = new PLDeviceUnreachableException("printer offline") };
        var storage = new FakeLocalizedQueueStorageProvider();
        var sut = new LifecycleCommandProcessorPL(sscd, storage);

        var result = await sut.InitialOperationReceipt0x4001Async(CreateRequest(0x504C_2000_0000_4001));

        // GetInfoAsync still reports failure by throwing: whether the register is fiscalized is
        // unknown, so the queue stays inactive and the caller gets an error response, not an exception.
        storage.Activated.Should().BeFalse();
        ((ulong)result.receiptResponse.ftState).Should().Be(0xEEEE_EEEEUL);
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

/// <summary>
/// The SCU reports device failures as error responses it built itself; the queue passes them on as
/// they are and must not record a closing for a report that did not run.
/// </summary>
public class ScuErrorResponseTests : ProcessorTestsBase
{
    private static List<ChargeItem> SingleItem() => new()
    {
        new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
    };

    [Fact]
    public async Task Receipt_ShouldPassTheScuErrorResponseThrough()
    {
        var sscd = new FakePLSSCD { ErrorMessage = "Do NOT resend blindly: check the register's last document first." };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle().Which.Data.Should().Be("Do NOT resend blindly: check the register's last document first.");
    }

    [Theory]
    [InlineData(0x504C_2000_0000_2011)]
    [InlineData(0x504C_2000_0000_2012)]
    public async Task Closing_ShouldNotWriteAnActionJournal_WhenTheScuReturnsAnError(ulong receiptCase)
    {
        var sut = new DailyOperationsCommandProcessorPL(new FakePLSSCD { ErrorMessage = "no answer to dailyrep" });

        var result = receiptCase == 0x504C_2000_0000_2011
            ? await sut.DailyClosing0x2011Async(CreateRequest(receiptCase))
            : await sut.MonthlyClosing0x2012Async(CreateRequest(receiptCase));

        ((ulong)result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
        // The action journal of a closing records that the report was printed and the counters
        // advanced — after an error response it would claim a report that is not known to have run.
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

/// <summary>What is left for the queue to catch: the transport to a (remote) SCU failing.</summary>
public class ScuCommunicationFailureTests : ProcessorTestsBase
{
    private static List<ChargeItem> SingleItem() => new()
    {
        new() { Amount = 10m, ftChargeItemCase = (ChargeItemCase)0x504C_2000_0000_0003 },
    };

    [Fact]
    public async Task Receipt_ShouldTellTheCallerToRetry_WhenTheScuRefusedTheConnection()
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = new HttpRequestException("connection refused", new SocketException((int)SocketError.ConnectionRefused)) };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        ((ulong)result.receiptResponse.ftState).Should().Be(0xEEEE_EEEEUL);
        result.receiptResponse.ftSignatures.Should().ContainSingle().Which.Data.Should().Contain("Retry once the SCU is available");
    }

    /// <summary>
    /// A timeout or a dropped connection may hit after the SCU handed the receipt to the register —
    /// the caller must check the register instead of retrying into a second fiscal receipt.
    /// </summary>
    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    public async Task Receipt_ShouldAskToCheckTheRegister_WhenTheConnectionToTheScuFailedMidCall(Type exceptionType)
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = (Exception)Activator.CreateInstance(exceptionType, "timeout")! };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var result = await sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        ((ulong)result.receiptResponse.ftState).Should().Be(0xEEEE_EEEEUL);
        result.receiptResponse.ftSignatures.Should().ContainSingle().Which.Data.Should().Contain("Do NOT resend blindly").And.NotContain("Retry once");
    }

    [Fact]
    public async Task Receipt_ShouldLetOtherFailuresBubble()
    {
        var sscd = new FakePLSSCD { ThrowOnProcessReceipt = new InvalidOperationException("bug") };
        var sut = new ReceiptCommandProcessorPL(sscd);

        var act = () => sut.PointOfSaleReceipt0x0001Async(CreateRequest(0x504C_2000_0000_0001, SingleItem()));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0x504C_2000_0000_2011)]
    [InlineData(0x504C_2000_0000_2012)]
    public async Task Closing_ShouldNotWriteAnActionJournal_WhenTheScuCannotBeReached(ulong receiptCase)
    {
        var sut = new DailyOperationsCommandProcessorPL(new FakePLSSCD { ThrowOnProcessReceipt = new HttpRequestException("connection refused") });

        var result = receiptCase == 0x504C_2000_0000_2011
            ? await sut.DailyClosing0x2011Async(CreateRequest(receiptCase))
            : await sut.MonthlyClosing0x2012Async(CreateRequest(receiptCase));

        ((ulong)result.receiptResponse.ftState).Should().Be(0xEEEE_EEEEUL);
        result.actionJournals.Should().BeEmpty();
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
