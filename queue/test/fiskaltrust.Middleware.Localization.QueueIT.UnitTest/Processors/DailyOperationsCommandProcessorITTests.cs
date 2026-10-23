using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Processors;

public class DailyOperationsCommandProcessorITTests
{
    private readonly ftQueue _queue = TestHelpers.CreateQueue();
    private readonly ftQueueItem _queueItem;
    private readonly ftQueueIT _queueIT;
    private readonly Mock<IConfigurationRepository> _configurationRepository;
    private readonly Mock<IMiddlewareJournalITRepository> _journalITRepository = TestHelpers.CreateJournalITRepository();

    public DailyOperationsCommandProcessorITTests()
    {
        _queueItem = TestHelpers.CreateQueueItem(_queue);
        _queueIT = TestHelpers.CreateQueueIT(_queue);
        _configurationRepository = TestHelpers.CreateConfigurationRepository(_queueIT);
    }

    private DailyOperationsCommandProcessorIT CreateSut(IITSSCDProvider sscd) => new(sscd, TestHelpers.Lazy(_journalITRepository.Object), TestHelpers.Lazy(_configurationRepository.Object));

    private ReceiptProcessor CreateReceiptProcessor(IITSSCDProvider sscd) => new(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, CreateSut(sscd), null!, null!);

    [Fact]
    public async Task ZeroReceipt_ResetsTheScuFailureCounters_AndReturnsWhatTheScuAnswered()
    {
        _queueIT.SSCDFailCount = 3;
        _queueIT.SSCDFailMoment = DateTime.UtcNow;
        _queueIT.SSCDFailQueueItemId = Guid.NewGuid();
        var sscd = TestHelpers.CreateSscd(request =>
        {
            request.ReceiptResponse.ftSignatures.Add(TestHelpers.RTSignature(SignatureTypeIT.RTSerialNumber, "<rt-serialnumber>", TestHelpers.RTSerialNumber));
            request.ReceiptResponse.ftStateData = "{\"CashStatus\":\"ok\"}";
        });
        var request = TestHelpers.CreateRequest(ReceiptCase.ZeroReceipt0x2000);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).ZeroReceipt0x2000Async(new ProcessCommandRequest(_queue, request, response));

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftSignatures.Should().ContainSingle();
        result.receiptResponse.ftStateData.Should().Be("{\"CashStatus\":\"ok\"}");
        result.actionJournals.Should().BeEmpty();
        _configurationRepository.Verify(x => x.InsertOrUpdateQueueITAsync(It.Is<ftQueueIT>(q => q.SSCDFailCount == 0 && q.SSCDFailMoment == null && q.SSCDFailQueueItemId == null)), Times.Once);
    }

    [Fact]
    public async Task ZeroReceipt_WithoutFailures_DoesNotTouchTheQueueIT()
    {
        var sscd = TestHelpers.CreateSscd();
        var request = TestHelpers.CreateRequest(ReceiptCase.ZeroReceipt0x2000);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        await CreateSut(sscd.Object).ZeroReceipt0x2000Async(new ProcessCommandRequest(_queue, request, response));

        _configurationRepository.Verify(x => x.InsertOrUpdateQueueITAsync(It.IsAny<ftQueueIT>()), Times.Never);
    }

    [Theory]
    [InlineData(ReceiptCase.DailyClosing0x2011, "Daily-Closing receipt was processed.")]
    [InlineData(ReceiptCase.MonthlyClosing0x2012, "Monthly-Closing receipt was processed.")]
    [InlineData(ReceiptCase.YearlyClosing0x2013, "Yearly-Closing receipt was processed.")]
    public async Task Closings_TakeTheZNumber_JournalTheReport_AndWriteAnActionJournal(ReceiptCase receiptCase, string message)
    {
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.ftSignatures.Add(TestHelpers.RTSignature(SignatureTypeIT.RTZNumber, "<rt-z-number>", "0012")));
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        using var scope = new AssertionScope();
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftReceiptIdentification.Should().Be("ft1#Z0012");
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.ftSignatureType.IsType(SignatureTypeIT.RTZNumber));
        result.actionJournals.Should().HaveCount(1);
        result.actionJournals[0].Type.Should().Be(request.ftReceiptCase.ToString("X"));
        result.actionJournals[0].Message.Should().Be(message);
        result.actionJournals[0].DataJson.Should().Be("{\"ftReceiptNumerator\":2}");
        _journalITRepository.Verify(x => x.InsertAsync(It.Is<ftJournalIT>(j => j.ZRepNumber == 12 && j.ReceiptNumber == 0 && j.JournalType == (long) receiptCase && j.ftQueueItemId == _queueItem.ftQueueItemId)), Times.Once);
    }

    [Fact]
    public async Task Closing_WithoutZNumberInTheScuResponse_Fails()
    {
        var sscd = TestHelpers.CreateSscd();
        var request = TestHelpers.CreateRequest(ReceiptCase.DailyClosing0x2011);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).DailyClosing0x2011Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.actionJournals.Should().BeEmpty();
        _journalITRepository.Verify(x => x.InsertAsync(It.IsAny<ftJournalIT>()), Times.Never);
    }

    [Fact]
    public async Task Closing_WhenTheScuFails_ReturnsTheFailure()
    {
        var sscd = TestHelpers.CreateSscd(request => request.ReceiptResponse.SetReceiptResponseError("Z report failed"));
        var request = TestHelpers.CreateRequest(ReceiptCase.DailyClosing0x2011);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateSut(sscd.Object).DailyClosing0x2011Async(new ProcessCommandRequest(_queue, request, response));

        result.receiptResponse.State().Should().Be(0x4954_2000_EEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Data == "Z report failed");
        result.actionJournals.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ReceiptCase.OneReceipt0x2001)]
    [InlineData(ReceiptCase.ShiftClosing0x2010)]
    public async Task OneReceiptAndShiftClosing_AreNoOps(ReceiptCase receiptCase)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(_queue, _queueItem, request);

        var result = await CreateReceiptProcessor(sscd.Object).ProcessAsync(request, response, _queue, _queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.actionJournals.Should().BeEmpty();
    }
}
