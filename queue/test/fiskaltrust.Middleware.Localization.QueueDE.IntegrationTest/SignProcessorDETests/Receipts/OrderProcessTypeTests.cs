using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class OrderProcessTypeTests
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        public OrderProcessTypeTests()
        {
            _fixture = new();
            _receiptTests = new ReceiptTests(_fixture);
        }
        [Fact]
        public async Task OrderProcessType_IsNoImplicitFlowAndOpenTrans_ExpectArgumentException() => await _receiptTests.ExpectArgumentExceptionReceiptReference(_receiptTests.GetReceipt("OrderProcessType", "OrderTypeNoImplNoOpen", 0x4445000000000010), "No transactionnumber found for cbReceiptReference '{0}'.").ConfigureAwait(false);

        [Fact]
        public async Task OrderProcessType_ValidRequestTraining_ExpectValidResponse()
        {
            var receiptRequest = _receiptTests.GetReceipt("OrderProcessType", "OrderProcessType_ValidRequestTrain", 0x4445000100020010);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty).ConfigureAwait(false);
            receiptResponse.ftSignatures.Should().NotBeNull();
            var trainingsignature = receiptResponse.ftSignatures.Where(x => x.ftSignatureType.Equals(4096)).FirstOrDefault();
            trainingsignature.Should().NotBeNull();
            trainingsignature.Caption.Should().Be("Trainingsbuchung");
        }
    }
}
