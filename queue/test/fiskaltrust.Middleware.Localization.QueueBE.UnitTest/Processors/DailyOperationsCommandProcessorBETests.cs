using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.be;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.Processors
{
    public class DailyOperationsCommandProcessorBETests
    {
        private static ProcessCommandRequest CreateRequest(ReceiptCase receiptCase)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase) (0x4245_2000_0000_0000 | (long) receiptCase)
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State) 0x4245_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 42,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            return new ProcessCommandRequest(new ftQueue { ftQueueId = receiptResponse.ftQueueID }, receiptRequest, receiptResponse);
        }

        private static Mock<IBESSCD> CreateSscd(Action<ReceiptResponse>? mutateResponse = null)
        {
            var sscd = new Mock<IBESSCD>(MockBehavior.Strict);
            sscd.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                .ReturnsAsync((ProcessRequest processRequest) =>
                {
                    mutateResponse?.Invoke(processRequest.ReceiptResponse);
                    return new ProcessResponse { ReceiptResponse = processRequest.ReceiptResponse };
                });
            return sscd;
        }

        [Fact]
        public async Task DailyClosing0x2011Async_SignedByTheSscd_ReturnsTheSignedResponseAndAnActionJournal()
        {
            var request = CreateRequest(ReceiptCase.DailyClosing0x2011);
            var sscd = CreateSscd(response => response.ftSignatures.Add(new SignatureItem
            {
                Caption = "DigitalSignature",
                Data = "a-zwartedoos-z-report-signature",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureType.Unknown
            }));

            var sut = new DailyOperationsCommandProcessorBE(sscd.Object);
            var result = await sut.DailyClosing0x2011Async(request);

            sscd.Verify(x => x.ProcessReceiptAsync(It.Is<ProcessRequest>(r => r.ReceiptRequest == request.ReceiptRequest)), Times.Once);
            result.receiptResponse.ftState.Should().Be((State) 0x4245_2000_0000_0000);
            result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "DigitalSignature");
            result.actionJournals.Should().ContainSingle();
            result.actionJournals[0].ftQueueId.Should().Be(request.ReceiptResponse.ftQueueID);
            result.actionJournals[0].ftQueueItemId.Should().Be(request.ReceiptResponse.ftQueueItemID);
            result.actionJournals[0].Message.Should().Be("Daily-Closing receipt was processed.");
        }

        /// <summary>
        /// A Z report the FDM refused must never look like a closed day: the errored response is
        /// handed back untouched and no action journal entry is written for it.
        /// </summary>
        [Fact]
        public async Task DailyClosing0x2011Async_RefusedByTheSscd_ReturnsNoActionJournal()
        {
            var request = CreateRequest(ReceiptCase.DailyClosing0x2011);
            var sscd = CreateSscd(response => response.ftState = response.ftState.WithState(State.Error));

            var sut = new DailyOperationsCommandProcessorBE(sscd.Object);
            var result = await sut.DailyClosing0x2011Async(request);

            result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
            result.actionJournals.Should().BeEmpty();
        }

        /// <summary>
        /// The state alone is not proof: a Z report that came back with no signature closed
        /// nothing, so the day must not be journalled as closed on the strength of a clean state.
        /// </summary>
        [Fact]
        public async Task DailyClosing0x2011Async_CleanStateButNoSignature_FailsAndWritesNoActionJournal()
        {
            var request = CreateRequest(ReceiptCase.DailyClosing0x2011);
            var sscd = CreateSscd();

            var sut = new DailyOperationsCommandProcessorBE(sscd.Object);
            var result = await sut.DailyClosing0x2011Async(request);

            result.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
            result.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE");
            result.actionJournals.Should().BeEmpty();
        }

        [Fact]
        public async Task ZeroReceipt0x2000Async_IsHandedToTheSscd()
        {
            var request = CreateRequest(ReceiptCase.ZeroReceipt0x2000);
            var sscd = CreateSscd();

            var sut = new DailyOperationsCommandProcessorBE(sscd.Object);
            var result = await sut.ZeroReceipt0x2000Async(request);

            sscd.Verify(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()), Times.Once);
            result.receiptResponse.ftState.Should().Be((State) 0x4245_2000_0000_0000);
            result.actionJournals.Should().BeEmpty();
        }

        /// <summary>
        /// The remaining periodic closings have no ZwarteDoos counterpart yet. They must keep
        /// failing loudly rather than reporting a clean state for a closing the FDM never saw.
        /// </summary>
        [Theory]
        [InlineData(ReceiptCase.OneReceipt0x2001)]
        [InlineData(ReceiptCase.ShiftClosing0x2010)]
        [InlineData(ReceiptCase.MonthlyClosing0x2012)]
        [InlineData(ReceiptCase.YearlyClosing0x2013)]
        public async Task UnimplementedDailyOperations_Throw(ReceiptCase receiptCase)
        {
            var request = CreateRequest(receiptCase);
            var sut = new DailyOperationsCommandProcessorBE(new Mock<IBESSCD>(MockBehavior.Strict).Object);

            Func<Task> act = () => receiptCase switch
            {
                ReceiptCase.OneReceipt0x2001 => sut.OneReceipt0x2001Async(request),
                ReceiptCase.ShiftClosing0x2010 => sut.ShiftClosing0x2010Async(request),
                ReceiptCase.MonthlyClosing0x2012 => sut.MonthlyClosing0x2012Async(request),
                _ => sut.YearlyClosing0x2013Async(request),
            };

            await act.Should().ThrowAsync<NotImplementedException>();
        }
    }
}
