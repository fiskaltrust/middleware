using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.Processors;

public class InvoiceCommandProcessorITTests
{
    private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), Mock.Of<IMarketValidator>(), null!, null!, null!, new InvoiceCommandProcessorIT(), null!);

    [Theory]
    [InlineData(ReceiptCase.InvoiceUnknown0x1000)]
    [InlineData(ReceiptCase.InvoiceB2C0x1001)]
    [InlineData(ReceiptCase.InvoiceB2B0x1002)]
    [InlineData(ReceiptCase.InvoiceB2G0x1003)]
    public async Task Invoices_AreStoredWithoutTheScu(ReceiptCase receiptCase)
    {
        var queue = TestHelpers.CreateQueue();
        var queueItem = TestHelpers.CreateQueueItem(queue);
        var request = TestHelpers.CreateRequest(receiptCase);
        var response = TestHelpers.CreateResponse(queue, queueItem, request);

        var result = await _sut.ProcessAsync(request, response, queue, queueItem);

        result.receiptResponse.Should().BeSameAs(response);
        result.receiptResponse.State().Should().Be(TestHelpers.BaseState);
        result.receiptResponse.ftSignatures.Should().BeEmpty();
        result.actionJournals.Should().BeEmpty();
    }
}
