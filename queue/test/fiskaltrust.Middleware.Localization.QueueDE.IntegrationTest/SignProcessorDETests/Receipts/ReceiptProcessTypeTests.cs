using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Helpers;
using Xunit;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class QueueStateTests : IClassFixture<SignProcessorDependenciesFixture>
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;
        private readonly ReceiptProcessorHelper _receiptProcessorHelper;

        public QueueStateTests(SignProcessorDependenciesFixture fixture)
        {
            _receiptTests = new ReceiptTests(fixture);
            _fixture = fixture;
            _receiptProcessorHelper = new ReceiptProcessorHelper(_fixture.SignProcessor);
        }
        
        [Fact]
        public async Task ReceiptProcessType_IsNoImplicitFlowAndOpenTrans_ExpectErrorState()
        {
            var receiptRequest = _receiptTests.GetReceipt("ExplicitPosReceipt", "ReceiptProcessTypeNoImplNoOpen", 0x4445000000000001);
            var response = await _receiptProcessorHelper.ProcessReceiptRequestAsync(receiptRequest);

            response.ftState.Should().Be(0xEEEE_EEEE);
        }

        [Fact]
        public async Task ReceiptProcessType_ValidRequestTraining_ExpectValidResponse()
        {
            var receiptRequest = _receiptTests.GetReceipt("ImplicitOrderRequest", "ReceiptProcessType_ValidRequestTrain", 0x4445000100020001);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));
            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty).ConfigureAwait(false);
            receiptResponse.ftSignatures.Should().NotBeNull();
            var trainingsignature = receiptResponse.ftSignatures.Where(x => x.ftSignatureType.Equals(4096)).FirstOrDefault();
            trainingsignature.Should().NotBeNull();
            trainingsignature.Caption.Should().Be("Trainingsbuchung");
            var signatureForPosReceiptActionStartMoment = receiptResponse.ftSignatures.Where(x => x.Caption.Equals("<vorgangsbeginn>")).FirstOrDefault();
            signatureForPosReceiptActionStartMoment.Should().NotBeNull();
        }
    }
}
