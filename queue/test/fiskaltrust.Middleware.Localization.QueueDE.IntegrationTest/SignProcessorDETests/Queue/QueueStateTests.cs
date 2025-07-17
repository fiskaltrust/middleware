using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using Xunit;
using FluentAssertions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Queue
{
    public class QueueStateTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        public QueueStateTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
        }
        [Fact]
        public async Task QueueState_IsFailedMode_ExpectValidResponse()
        {
            var receiptRequest = _receiptTests.GetReceipt("ImplicitOrderRequest", "ReceiptProcessType_ValidRequestTrain", 0x4445000100020001);
            await ProcessAsyncQueueFailedModeAsync(receiptRequest).ConfigureAwait(false);
        }
        [Fact]
        public async Task QueueStateIsFailed_ExplicitStartTrans_ExpectValidResponse()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "IsFailedExplicitStartTrans", 0x4445000000000008);
            await ProcessAsyncQueueFailedModeAsync(receiptRequest).ConfigureAwait(false);
            var failedStartTransExists = await _fixture.failedStartTransactionRepository.ExistsAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            failedStartTransExists.Should().BeTrue();
        }
        [Fact]
        public async Task QueueStateIsFailed_ExplicitReceiptFailedStartExists_FailedStartRemoved()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "FailExpliFailedStartExists", 0x4445000000000001);
            var failedStartTransaction = new FailedStartTransaction()
            {
                cbReceiptReference = receiptRequest.cbReceiptReference
            };
            await _fixture.failedStartTransactionRepository.InsertOrUpdateTransactionAsync(failedStartTransaction).ConfigureAwait(false);
            await ProcessAsyncQueueFailedModeAsync(receiptRequest).ConfigureAwait(false);
            var failedStartTransExists = await _fixture.failedStartTransactionRepository.ExistsAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            failedStartTransExists.Should().BeFalse();
        }
        [Fact]
        public async Task QueueStateIsFailed_ExplicitReceiptOpenTransExists_FailedFinishExists()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "FailExpliOpenTransExists", 0x4445000000000001);
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, 21).ConfigureAwait(false);
            await ProcessAsyncQueueFailedModeAsync(receiptRequest).ConfigureAwait(false);
            var failedFinishTransExists = await _fixture.failedFinishTransactionRepository.ExistsAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            failedFinishTransExists.Should().BeTrue();
        }
        [Fact]
        public async Task QueueNewAndDisalbed_DisabledQueue_ValidResult()
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false);
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "FailExpliOpenTransExists", 0x4445000000000001);

            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            var actionMessage = string.Format("QueueId {0} was not activated or already deactivated", _fixture.QUEUEID);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, actionMessage
                , 0x4445000000000001, 0, false).ConfigureAwait(false);
        }
        [Fact]
        public async Task QueueAktiv_DisabledScuReceiptRequest_ValidResult()
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), openTrans: new ulong[] { 1, 2 }, queueDECreationUnitIsNull: true);
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "DisabledScuReceiptRequest", 0x4445000000000001);
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, "SCU switching process initiated, but not yet finished.",
                0x4445000000000100, 0, false).ConfigureAwait(false);
        }
        private async Task ProcessAsyncQueueFailedModeAsync(ReceiptRequest receiptRequest)
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(true, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty, 0x4445000000000002, 0, false).ConfigureAwait(false);
            receiptResponse.ftSignatures.Should().NotBeNull();
            var tsefailsig = receiptResponse.ftSignatures.Where(x => x.ftSignatureType.Equals(4096)).FirstOrDefault();
            tsefailsig.Should().NotBeNull();
            tsefailsig.Caption.Should().Be("Kommunikation mit der technischen Sicherheitseinrichtung (TSE) fehlgeschlagen");
        }
    }
}
