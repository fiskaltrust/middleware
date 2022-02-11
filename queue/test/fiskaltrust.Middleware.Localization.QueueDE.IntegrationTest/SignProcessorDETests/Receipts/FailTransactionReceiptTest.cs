using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class FailTransactionReceiptTest : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        public FailTransactionReceiptTest(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
        }
        [Fact]
        public async Task FailTransaction_IsImplicitFlowOnSingle_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(_receiptTests.GetReceipt("StartTransactionReceipt", "FailTransNoImplFlow", 0x444500010000000b), "ReceiptCase {0:X} (fail-transaction-receipt) cannot use implicit-flow flag when a single transaction should be failed.").ConfigureAwait(false);

        [Fact]
        public async Task FailTransaction_IsNoImplicitFlowOnMulti_ExpectArgumentException()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "", 0x44450000000000b);
            receiptRequest.ftReceiptCaseData = "{\"CurrentStartedTransactionNumbers\": [8, 9]}";
            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, "ReceiptCase {0:X} (fail-transaction-receipt) must use implicit-flow flag when multiple transactions should be failed.").ConfigureAwait(false);
        }
        
        [Fact]
        public async Task FailTransaction_NoOpenTransOnSingle_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptReference(_receiptTests.GetReceipt("StartTransactionReceipt", "FailTransNoOpenTransSingle", 0x444500000000000b), "No open transaction found for cbReceiptReference '{0:X}'. If you want to close multiple transactions, pass an array value for 'CurrentStartedTransactionNumbers' via ftReceiptCaseData.").ConfigureAwait(false);
        
        [Fact]
        public async Task FailTransaction_SingleTrans_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(_receiptTests.GetReceipt("StartTransactionReceipt", "FailTransNoImplFlow", 0x444500010000000b), "ReceiptCase {0:X} (fail-transaction-receipt) cannot use implicit-flow flag when a single transaction should be failed.").ConfigureAwait(false);

        [Fact]
        public async Task FailTransaction_ValidSingleTransTraining_ExpectValid()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "UpdateTransaction_TrainingRequest", 0x444500000002000b);
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, 7);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            var i = receiptResponse.ftSignatures.Length - 1;
            receiptResponse.ftSignatures[i].ftSignatureType.Should().Be(4096);
            receiptResponse.ftSignatures[i].Caption.Should().Be("Trainingsbuchung");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
            var opentrans = await _fixture.openTransactionRepository.GetAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            opentrans.Should().BeNull();
        }

        [Fact]
        public async Task FailTransaction_ValidMultiTrans_ExpectValid()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", string.Empty, 0x444500010000000b);
            receiptRequest.ftReceiptCaseData = "{\"CurrentStartedTransactionNumbers\": [8, 9]}";
            await _fixture.AddOpenOrders("FailTransaction-X", 8);
            await _fixture.AddOpenOrders("FailTransaction-Y", 9);
            await _fixture.AddOpenOrders("FailTransaction-Z", 10);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
            var opentrans = await _fixture.openTransactionRepository.GetAsync("FailTransaction-Z").ConfigureAwait(false);
            opentrans.Should().NotBeNull();
            opentrans = await _fixture.openTransactionRepository.GetAsync("FailTransaction-Y").ConfigureAwait(false);
            opentrans.Should().BeNull();
            opentrans = await _fixture.openTransactionRepository.GetAsync("FailTransaction-X").ConfigureAwait(false);
            opentrans.Should().BeNull();
        }
    }
}
