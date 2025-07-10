using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests
{
    public class ReceiptTests
    {
        private readonly SignProcessorDependenciesFixture _fixture;
        public ReceiptTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;
        public async Task ExpectException(ReceiptRequest receiptRequest, string errorMessage, bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false)
        {
            var sutMethod = CallSignProcessor_ExpectException(receiptRequest, sourceIsScuSwitch, targetIsScuSwitch);
            await sutMethod.Should().ThrowAsync<Exception>().WithMessage(errorMessage).ConfigureAwait(false);
        }
        public async Task ExpectArgumentExceptionReceiptcase(ReceiptRequest receiptRequest, string errorMessage)
        {
            var sutMethod = CallSignProcessor_ExpectException(receiptRequest);
            await sutMethod.Should().ThrowAsync<ArgumentException>().WithMessage(string.Format(errorMessage, receiptRequest.ftReceiptCase)).ConfigureAwait(false);
        }
        public async Task ExpectExceptionReceiptcase(ReceiptRequest receiptRequest, string errorMessage)
        {
            var sutMethod = CallSignProcessor_ExpectException(receiptRequest);
            await sutMethod.Should().ThrowAsync<Exception>().WithMessage(string.Format(errorMessage, receiptRequest.ftReceiptCase)).ConfigureAwait(false);
        }
        public async Task ExpectArgumentExceptionReceiptReference(ReceiptRequest receiptRequest, string errorMessage)
        {
            var sutMethod = CallSignProcessor_ExpectException(receiptRequest);
            await sutMethod.Should().ThrowAsync<ArgumentException>().WithMessage(string.Format(errorMessage, receiptRequest.cbReceiptReference)).ConfigureAwait(false);
        }
        public async Task Transaction_WithOpenTransactionRepo_ExpectArgumentException(string transactionFolder, int transNo)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", transactionFolder));
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, transNo);
            await ExpectArgumentExceptionReceiptcase(receiptRequest, string.Format("Transactionnumber {0} was already started using cbReceiptReference '{1}'.", transNo, receiptRequest.cbReceiptReference)).ConfigureAwait(false);
        }
        public ReceiptRequest GetReceipt(string basefolder, string receiptReference, long? receiptCase)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", basefolder));
            receiptRequest.ftReceiptCase = receiptCase ?? receiptRequest.ftReceiptCase;
            receiptRequest.cbReceiptReference = receiptReference;
            return receiptRequest;
        }
        private Func<Task> CallSignProcessor_ExpectException(ReceiptRequest receiptRequest, bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false)
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), sourceIsScuSwitch: sourceIsScuSwitch, targetIsScuSwitch: targetIsScuSwitch);
            return async () => { var receiptResponse = await signProcessor.ProcessAsync(receiptRequest); };
        }
    }
}
