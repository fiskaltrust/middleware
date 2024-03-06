using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class FailTransactionReceiptTest : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public FailTransactionReceiptTest(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture; 
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }
        
        [Fact]
        public async Task FailTransaction_IsImplicitFlowOnSingle_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "FailTransNoImplFlow", 0x444500010000000b);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task FailTransaction_IsNoImplicitFlowOnMulti_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "", 0x44450000000000b);
            receiptRequest.ftReceiptCaseData = "{\"CurrentStartedTransactionNumbers\": [8, 9]}";
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
        
        [Fact]
        public async Task StartTransaction_WithOpenTransactionRepo_ExpectErrorState()
        {
            var existingOpenTransactionRef = "TestRef";
            var openTransaction = new OpenTransaction
            {
                TransactionNumber = 4,
                StartTransactionSignatureBase64 = "exampleSignature",
                StartMoment = DateTime.UtcNow.AddHours(-1),
                cbReceiptReference = existingOpenTransactionRef
            };
            await _fixture.openTransactionRepository.InsertOrUpdateTransactionAsync(openTransaction);

            await _fixture.openTransactionRepository.RemoveAsync(existingOpenTransactionRef);

            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", existingOpenTransactionRef, null);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);


            Assert.Equal(0xEEEE_EEEE, response.ftState);
        }

        [Fact]
        public async Task FailTransaction_SingleTrans_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "FailTransNoImplFlow", 0x444500010000000b);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }
        
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

        [Fact]
        public async Task FailTransaction_WithFailedStartTransaction_ExpectValid()
        {
            var receiptRequest = _receiptTests.GetReceipt("FailTransactionReceipt", "FailedStartTransaction", 0x444500000000000B);
            await _fixture.AddFailedStartTransaction("FailedStartTransaction");
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty, checkReceiptId: false);
            var opentrans = await _fixture.failedFinishTransactionRepository.GetAsync("FailedStartTransaction").ConfigureAwait(false);
            opentrans.Should().BeNull();
        }
    }
}
