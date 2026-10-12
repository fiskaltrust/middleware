using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class AbortReceiptTests
    {
        private readonly ReceiptTests _receiptTests;
        private readonly SignProcessorDependenciesFixture _fixture;

        public AbortReceiptTests()
        {
            _fixture = new SignProcessorDependenciesFixture();
            _receiptTests = new ReceiptTests(_fixture);
        }

        private ReceiptRequest GetAbortReceipt(string receiptReference, long receiptCase)
        {
            var receiptRequest = _receiptTests.GetReceipt("ImplicitPosReceipt", receiptReference, receiptCase);
            receiptRequest.cbPayItems = Array.Empty<PayItem>();
            return receiptRequest;
        }

        [Fact]
        public async Task AbortReceipt_ValidImplicitFlow_ExpectValid()
        {
            var receiptRequest = GetAbortReceipt("AbortReceipt", 0x444500010000001A);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));

            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);

            receiptResponse.Should().NotBeNull();
            receiptResponse.ftSignatures.Should().NotBeNullOrEmpty();
            receiptResponse.ftReceiptIdentification.Should().Contain("#IT");
            receiptResponse.ftSignatures.Should().NotContain(x => x.Caption == "Trainingsbuchung");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
            var openTransaction = await _fixture.openTransactionRepository.GetAsync(receiptRequest.cbReceiptReference).ConfigureAwait(false);
            openTransaction.Should().BeNull();
        }

        [Fact]
        public async Task AbortReceipt_ValidImplicitFlowTraining_ExpectTrainingSignature()
        {
            var receiptRequest = GetAbortReceipt("AbortReceiptTraining", 0x444500010002001A);
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1));

            var receiptResponse = await signProcessor.ProcessAsync(receiptRequest);

            receiptResponse.Should().NotBeNull();
            var lastSignature = receiptResponse.ftSignatures.Last();
            lastSignature.ftSignatureType.Should().Be(4096);
            lastSignature.Caption.Should().Be("Trainingsbuchung");
            await ReceiptTestResults.IsResponseValidAsync(_fixture, receiptResponse, receiptRequest, string.Empty);
        }

        [Fact]
        public async Task AbortReceipt_WithPayItems_ExpectArgumentException()
        {
            var receiptRequest = _receiptTests.GetReceipt("ImplicitPosReceipt", "AbortReceiptWithPayment", 0x444500010000001A);
            receiptRequest.cbPayItems.Should().NotBeEmpty();

            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, "ReceiptCase {0:X} (abort-receipt) must not contain any pay items, as no payment is allowed for an aborted receipt.").ConfigureAwait(false);
        }

        [Fact]
        public async Task AbortReceipt_NoImplicitFlow_ExpectArgumentException()
        {
            var receiptRequest = GetAbortReceipt("AbortReceiptExplicit", 0x444500000000001A);

            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, "ReceiptCase {0:X} (abort-receipt) must use the implicit-flow flag.").ConfigureAwait(false);
        }

        [Fact]
        public async Task AbortReceipt_NoReceiptReference_ExpectArgumentException()
        {
            var receiptRequest = GetAbortReceipt(string.Empty, 0x444500010000001A);

            await _receiptTests.ExpectArgumentExceptionReceiptcase(receiptRequest, "ReceiptCase {0:X} (abort-receipt) requires a cbReceiptReference.").ConfigureAwait(false);
        }
    }
}
