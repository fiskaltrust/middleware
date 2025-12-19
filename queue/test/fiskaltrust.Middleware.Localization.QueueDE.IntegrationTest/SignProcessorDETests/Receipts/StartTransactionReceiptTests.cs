using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class StartTransactionReceiptTests
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        public StartTransactionReceiptTests()
        {
            _fixture = new();
            _receiptTests = new ReceiptTests(_fixture);
        }
        [Fact]
        public async Task StartTransaction_IsNoImplicitFlow_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptcase(_receiptTests.GetReceipt("StartTransactionReceipt", "StartTransNoImplFlow", 0x4445000100000008), "ReceiptCase {0:X} (Start-transaction receipt) can not use implicit-flow flag.").ConfigureAwait(false);
        [Fact]
        public async Task StartTransaction_WithOpenTransactionRepo_ExpectArgumentException() => await _receiptTests.Transaction_WithOpenTransactionRepo_ExpectArgumentException("StartTransactionReceipt", 4).ConfigureAwait(false);
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
    }
}
