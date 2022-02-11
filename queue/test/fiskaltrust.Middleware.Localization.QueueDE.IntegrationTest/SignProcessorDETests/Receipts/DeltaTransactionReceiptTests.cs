using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class DeltaTransactionReceiptTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        public DeltaTransactionReceiptTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
        }
        [Fact]
        public async Task DeltaTransaction_IsNoImplicitFlow_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(_receiptTests.GetReceipt("StartTransactionReceipt", "DeltaTransNoImplFlow", 0x444500010000000A), "ReceiptCase {0:X} (Delta-transaction receipt) can not use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task DeltaTransaction_NoOpenTransactionRepo_ExpectArgumentException()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "UpTransNoOpenTrans", 0x444500000000000A);
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, string.Format("No transactionnumber found for cbReceiptReference '{0}'.", receiptRequest.cbReceiptReference)).ConfigureAwait(false);
        }
        [Fact]
        public async Task DeltaTransaction_TrainingRequest_ExpectTrainingSign()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "DeltaTransaction_TrainingRequest", 0x444500000002000A);
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, 7);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            var i = receiptResponse.ftSignatures.Length - 1;
            receiptResponse.ftSignatures[i].ftSignatureType.Should().Be(4096);
            receiptResponse.ftSignatures[i].Caption.Should().Be("Trainingsbuchung");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
        }
    }
}
