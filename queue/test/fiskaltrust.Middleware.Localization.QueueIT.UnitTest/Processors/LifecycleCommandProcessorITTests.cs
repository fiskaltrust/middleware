using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Processors;

public class LifecycleCommandProcessorITTests
{
    private readonly ftQueue _queue = TestHelpers.CreateQueue();
    private readonly ftQueueItem _queueItem;
    private readonly ftQueueIT _queueIT;
    private readonly ftSignaturCreationUnitIT _scu;
    private readonly Mock<IConfigurationRepository> _configurationRepository;
    private readonly Mock<ILocalizedQueueStorageProvider> _queueStorageProvider = new();

    public LifecycleCommandProcessorITTests()
    {
        _queueItem = TestHelpers.CreateQueueItem(_queue);
        _queueIT = TestHelpers.CreateQueueIT(_queue);
        _scu = TestHelpers.CreateSignaturCreationUnitIT(_queueIT);
        _configurationRepository = TestHelpers.CreateConfigurationRepository(_queueIT, _scu);
        _queueStorageProvider.Setup(x => x.ActivateQueueAsync()).Returns(Task.CompletedTask);
        _queueStorageProvider.Setup(x => x.DeactivateQueueAsync()).Returns(Task.CompletedTask);
    }

    private LifecycleCommandProcessorIT CreateSut(IITSSCDProvider sscd) => new(sscd, _queueStorageProvider.Object, TestHelpers.Lazy(_configurationRepository.Object));

    [Fact]
    public async Task InitialOperation_ActivatesQueue_WritesActivationJournal_AndSignatures()
    {
        var scuSignature = TestHelpers.RTSignature(SignatureTypeIT.RTSerialNumber, "<rt-serialnumber>", TestHelpers.RTSerialNumber);
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.ftSignatures.Add(scuSignature));
        var request = TestHelpers.CreateRequest(ReceiptCase.InitialOperationReceipt0x4001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).InitialOperationReceipt0x4001Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftSignatures.Should().HaveCount(2);
        ((ulong) result.receiptResponse.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0001_1001, "the initial operation signature keeps its pre-v2 value including the archiving flag");
        result.receiptResponse.ftSignatures[0].Caption.Should().Be("Initial-operation receipt");
        result.receiptResponse.ftSignatures[0].Data.Should().Be($"Queue-ID: {_queue.ftQueueId} Serial-Nr: {TestHelpers.RTSerialNumber}");
        result.receiptResponse.ftSignatures[1].Should().BeSameAs(scuSignature);

        result.actionJournals.Should().HaveCount(1);
        result.actionJournals[0].Type.Should().Be("4954200000004001-ActivateQueueSCU");
        result.actionJournals[0].Message.Should().Be($"Initial-Operation receipt. Queue-ID: {_queue.ftQueueId}");
        result.actionJournals[0].ftQueueId.Should().Be(_queue.ftQueueId);
        result.actionJournals[0].ftQueueItemId.Should().Be(_queueItem.ftQueueItemId);
        var notification = JsonConvert.DeserializeObject<ActivateQueueSCU>(result.actionJournals[0].DataJson)!;
        notification.CashBoxId.Should().Be(request.ftCashBoxID!.Value);
        notification.QueueId.Should().Be(_queue.ftQueueId);
        notification.SCUId.Should().Be(_queueIT.ftSignaturCreationUnitITId!.Value);
        notification.IsStartReceipt.Should().BeTrue();
        notification.Version.Should().Be("V0");

        _queueStorageProvider.Verify(x => x.ActivateQueueAsync(), Times.Once);
        _configurationRepository.Verify(x => x.InsertOrUpdateSignaturCreationUnitITAsync(It.Is<ftSignaturCreationUnitIT>(s => s.InfoJson.Contains(TestHelpers.RTSerialNumber))), Times.Once, "the RT info is stored on the SCU when it has none yet");
    }

    [Fact]
    public async Task InitialOperation_KeepsExistingScuInfo()
    {
        _scu.InfoJson = "{\"SerialNumber\":\"stored\"}";
        var sscd = TestHelpers.CreateSscd();
        var request = TestHelpers.CreateRequest(ReceiptCase.InitialOperationReceipt0x4001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).InitialOperationReceipt0x4001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        _configurationRepository.Verify(x => x.InsertOrUpdateSignaturCreationUnitITAsync(It.IsAny<ftSignaturCreationUnitIT>()), Times.Never);
    }

    [Fact]
    public async Task InitialOperation_WhenScuFails_DoesNotActivate()
    {
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.SetReceiptResponseError("RT device not reachable"));
        var request = TestHelpers.CreateRequest(ReceiptCase.InitialOperationReceipt0x4001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).InitialOperationReceipt0x4001Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data == "RT device not reachable");
        result.actionJournals.Should().BeEmpty();
        _queueStorageProvider.Verify(x => x.ActivateQueueAsync(), Times.Never);
    }

    [Fact]
    public async Task InitialOperation_WithoutAssignedScu_Fails()
    {
        _queueIT.ftSignaturCreationUnitITId = null;
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(ReceiptCase.InitialOperationReceipt0x4001);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).InitialOperationReceipt0x4001Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE" && x.Data.Contains("no ftSignaturCreationUnitIT assigned"));
        _queueStorageProvider.Verify(x => x.ActivateQueueAsync(), Times.Never);
    }

    [Fact]
    public async Task OutOfOperation_DeactivatesQueue_MarksDisabled_AndWritesDeactivationJournal()
    {
        var scuSignature = TestHelpers.RTSignature(SignatureTypeIT.RTSerialNumber, "<rt-serialnumber>", TestHelpers.RTSerialNumber);
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.ftSignatures.Add(scuSignature));
        var request = TestHelpers.CreateRequest(ReceiptCase.OutOfOperationReceipt0x4002);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).OutOfOperationReceipt0x4002Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(0x4954_2000_0000_0001, "the security mechanism is reported as deactivated, like every other v2 market does");
        result.receiptResponse.ftSignatures.Should().HaveCount(2);
        ((ulong) result.receiptResponse.ftSignatures[0].ftSignatureType).Should().Be(0x4954_2000_0001_1002);
        result.receiptResponse.ftSignatures[0].Caption.Should().Be("Out-of-operation receipt");
        result.receiptResponse.ftSignatures[1].Should().BeSameAs(scuSignature);

        result.actionJournals.Should().HaveCount(1);
        result.actionJournals[0].Type.Should().Be("4954200000004002-DeactivateQueueSCU");
        var notification = JsonConvert.DeserializeObject<DeactivateQueueSCU>(result.actionJournals[0].DataJson)!;
        notification.IsStopReceipt.Should().BeTrue();
        notification.SCUId.Should().Be(_queueIT.ftSignaturCreationUnitITId!.Value);
        _queueStorageProvider.Verify(x => x.DeactivateQueueAsync(), Times.Once);
    }

    [Fact]
    public async Task OutOfOperation_WhenScuFails_DoesNotDeactivate()
    {
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.SetReceiptResponseError("RT device not reachable"));
        var request = TestHelpers.CreateRequest(ReceiptCase.OutOfOperationReceipt0x4002);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).OutOfOperationReceipt0x4002Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.actionJournals.Should().BeEmpty();
        _queueStorageProvider.Verify(x => x.DeactivateQueueAsync(), Times.Never);
    }

    [Theory]
    [InlineData(ReceiptCase.InitSCUSwitch0x4011)]
    [InlineData(ReceiptCase.FinishSCUSwitch0x4012)]
    public async Task ScuSwitch_IsNoOp(ReceiptCase receiptCase)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var sut = new ReceiptProcessor(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), CreateSut(sscd.Object), null!, null!, null!, null!);
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await sut.ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.actionJournals.Should().BeEmpty();
    }
}
