using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>The two-step SCU switch, 0x4011 then 0x4012 (market-fr#177, ADR 0005).</summary>
public class ScuSwitchTests
{
    private static FRSigningPipeline CreatePipeline(IFRSSCD sscd, params ftQueueItem[] existingQueueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(existingQueueItems);
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
    }

    private static LifecycleCommandProcessorFR Lifecycle(IFRSSCD sscd, FRSigningPipeline pipeline)
        => new(sscd, pipeline, new Mock<ILocalizedQueueStorageProvider>().Object);

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    private static bool IsError(ProcessCommandResponse response) => response.receiptResponse.ftState.IsState(State.Error);

    private static FRScuSwitch? ScuSwitchOf(ProcessCommandResponse response)
        => MiddlewareStateData.FromReceiptResponse(response.receiptResponse).FR?.ScuSwitch;

    [Fact]
    public async Task InitScuSwitch_IsSignedByTheOutgoingScuAndRecordsIt()
    {
        var sscd = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var pipeline = CreatePipeline(sscd);

        var response = await Lifecycle(sscd, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        IsError(response).Should().BeFalse();
        sscd.Calls.Should().ContainSingle();
        response.actionJournals.Should().ContainSingle();
        ScuSwitchOf(response)!.Outgoing.CertificateSerialNumber.Should().Be("OLD");
        ScuSwitchOf(response)!.Incoming.Should().BeNull();
        (await pipeline.Chains.GetPendingScuSwitchAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task WhileASwitchIsPending_EveryOtherSignedReceiptIsRefused()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);
        await Lifecycle(sscd, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        var ticket = await new ReceiptCommandProcessorFR(pipeline).PointOfSaleReceipt0x0001Async(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));
        var closing = await new DailyOperationsCommandProcessorFR(pipeline).DailyClosing0x2011Async(CommandRequest(ReceiptCase.DailyClosing0x2011));

        IsError(ticket).Should().BeTrue();
        IsError(closing).Should().BeTrue();
        sscd.Calls.Should().ContainSingle("only the 0x4011 receipt may have been signed");
    }

    [Fact]
    public async Task InitScuSwitch_WhileOneIsPending_IsRefused()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);
        await Lifecycle(sscd, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        var second = await Lifecycle(sscd, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        IsError(second).Should().BeTrue();
        sscd.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task FinishScuSwitch_WithoutInit_IsRefused()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        var response = await Lifecycle(sscd, pipeline).FinishSCUSwitch0x4012Async(CommandRequest(ReceiptCase.FinishSCUSwitch0x4012));

        IsError(response).Should().BeTrue();
        sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task FinishScuSwitch_WithTheOutgoingScuStillConfigured_IsRefusedAndStaysPending()
    {
        var sscd = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var pipeline = CreatePipeline(sscd);
        await Lifecycle(sscd, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        var response = await Lifecycle(sscd, pipeline).FinishSCUSwitch0x4012Async(CommandRequest(ReceiptCase.FinishSCUSwitch0x4012));

        IsError(response).Should().BeTrue();
        sscd.Calls.Should().ContainSingle();
        (await pipeline.Chains.GetPendingScuSwitchAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task FinishScuSwitch_WithAnIncomingScuThatCannotSign_IsRefused()
    {
        var outgoing = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var pipeline = CreatePipeline(outgoing);
        await Lifecycle(outgoing, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        var incoming = new FakeFRSSCD { CertificateSerialNumber = "NEW", HasSignatureCreationData = false };
        var response = await Lifecycle(incoming, pipeline).FinishSCUSwitch0x4012Async(CommandRequest(ReceiptCase.FinishSCUSwitch0x4012));

        IsError(response).Should().BeTrue();
        incoming.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task FinishScuSwitch_ChainsOnTheOutgoingScusLastSignatureAndReopensTheQueue()
    {
        var outgoing = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var pipeline = CreatePipeline(outgoing);
        var init = await Lifecycle(outgoing, pipeline).InitSCUSwitch0x4011Async(CommandRequest(ReceiptCase.InitSCUSwitch0x4011));

        // The incoming SCU shares the chain state, as the restarted queue rebuilds it from storage.
        var incoming = new FakeFRSSCD { CertificateSerialNumber = "NEW" };
        var incomingPipeline = new FRSigningPipeline(incoming, pipeline.Chains);
        var finish = await Lifecycle(incoming, incomingPipeline).FinishSCUSwitch0x4012Async(CommandRequest(ReceiptCase.FinishSCUSwitch0x4012));
        var ticket = await new ReceiptCommandProcessorFR(incomingPipeline).PointOfSaleReceipt0x0001Async(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));

        IsError(finish).Should().BeFalse();
        incoming.Calls[0].previousSignature.Should().Be("sig-1", "ADR 0005: the incoming SCU's first entry covers the outgoing SCU's last signature");
        finish.receiptResponse.ftReceiptIdentification.Should().Be("ft1#G2");
        finish.actionJournals.Should().ContainSingle();
        ScuSwitchOf(finish)!.Outgoing.CertificateSerialNumber.Should().Be("OLD");
        ScuSwitchOf(finish)!.Incoming!.CertificateSerialNumber.Should().Be("NEW");
        IsError(ticket).Should().BeFalse();
        (await pipeline.Chains.GetPendingScuSwitchAsync()).Should().BeNull();
        init.receiptResponse.ftReceiptIdentification.Should().Be("ft1#G1");
    }

    [Fact]
    public async Task APendingSwitch_IsRebuiltFromTheQueueItems()
    {
        var sscd = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var request = CommandRequest(ReceiptCase.InitSCUSwitch0x4011);
        var init = await Lifecycle(sscd, CreatePipeline(sscd)).InitSCUSwitch0x4011Async(request);

        var restarted = CreatePipeline(new FakeFRSSCD { CertificateSerialNumber = "NEW" }, Stored(1, request.ReceiptRequest, init.receiptResponse));
        var pending = await restarted.Chains.GetPendingScuSwitchAsync();

        pending.Should().NotBeNull("a restart between 0x4011 and 0x4012 is the normal way to configure the incoming SCU");
        pending!.Outgoing.CertificateSerialNumber.Should().Be("OLD");
    }

    [Fact]
    public async Task AFinishedSwitch_IsNotPendingAfterARestart()
    {
        var outgoing = new FakeFRSSCD { CertificateSerialNumber = "OLD" };
        var pipeline = CreatePipeline(outgoing);
        var initRequest = CommandRequest(ReceiptCase.InitSCUSwitch0x4011);
        var init = await Lifecycle(outgoing, pipeline).InitSCUSwitch0x4011Async(initRequest);
        var incoming = new FakeFRSSCD { CertificateSerialNumber = "NEW" };
        var finishRequest = CommandRequest(ReceiptCase.FinishSCUSwitch0x4012);
        var finish = await Lifecycle(incoming, new FRSigningPipeline(incoming, pipeline.Chains)).FinishSCUSwitch0x4012Async(finishRequest);

        var restarted = CreatePipeline(incoming, Stored(1, initRequest.ReceiptRequest, init.receiptResponse), Stored(2, finishRequest.ReceiptRequest, finish.receiptResponse));

        (await restarted.Chains.GetPendingScuSwitchAsync()).Should().BeNull();
    }

    private static ftQueueItem Stored(long queueRow, ReceiptRequest request, ReceiptResponse response) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        ftQueueRow = queueRow,
        request = JsonSerializer.Serialize(request),
        response = JsonSerializer.Serialize(response),
    };
}
