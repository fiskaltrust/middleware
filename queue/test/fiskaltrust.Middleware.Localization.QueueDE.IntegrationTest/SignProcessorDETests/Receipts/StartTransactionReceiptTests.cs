using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using fiskaltrust.storage.V0;
using Xunit;
using FluentAssertions;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class StartTransactionReceiptTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public StartTransactionReceiptTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }
        
        [Fact]
        public async Task StartTransaction_IsNoImplicitFlow_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "StartTransNoImplFlow", 0x4445000100000008);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task StartTransaction_WithOpenTransactionRepo_ExpectErrorState()
        {
            var openTransaction = new OpenTransaction
            {
                TransactionNumber = 4,
                StartTransactionSignatureBase64 = "TestSignature",
                StartMoment = DateTime.UtcNow.AddHours(-1),
                cbReceiptReference = "TestRef"
            };
            await _fixture.openTransactionRepository.InsertAsync(openTransaction);

            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", openTransaction.cbReceiptReference, null);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            Assert.Equal(0xEEEE_EEEE, response.ftState);
        }

        [Fact]
        public async Task StartTransaction_ValidRequest_ExpectOpenTransaction()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "StartTransaction_ValidRequest", null);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty).ConfigureAwait(false);
            var opentranses = await _fixture.openTransactionRepository.GetAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            opentranses.Should().NotBeNull();
            opentranses.cbReceiptReference.Should().Be(receiptRequest.cbReceiptReference);
        }
        
        [Fact]
        public async Task StartTransaction_TrainingRequest_ExpectTrainingSign()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "StartTransaction_TrainingRequest", 0x4445000000020008);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNull();
            receiptResponse.ftSignatures.Count().Should().Be(2);
            receiptResponse.ftSignatures[1].ftSignatureType.Should().Be(4096);
            receiptResponse.ftSignatures[1].Caption.Should().Be("Trainingsbuchung");
        }
        [Fact]
        public async Task StartTransaction_WithErrorState_ExpectActionJournalEntry()
        {
            var receiptRequest = _receiptTests.GetReceipt("StartTransactionReceipt", "StartTransactionReceiptWithError", null);
            var expectedErrorMessage = "Transaction failed due to invalid receipt format.";

            _fixture.ActionJournalRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>()))
                .Callback((ftActionJournal journal) =>
                {
                    Assert.Contains(expectedErrorMessage, journal.Message);
                });

            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            _fixture.ActionJournalRepositoryMock.Verify(x => x.InsertAsync(It.IsAny<ftActionJournal>()), Times.Once);
        }
    }
}