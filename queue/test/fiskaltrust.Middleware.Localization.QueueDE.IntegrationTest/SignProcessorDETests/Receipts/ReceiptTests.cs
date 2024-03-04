using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Fixtures;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.SignProcessorDETests.Receipts
{
    public class ReceiptTests
    {
        private readonly SignProcessorDependenciesFixture _fixture;

        public ReceiptTests(SignProcessorDependenciesFixture fixture) => _fixture = fixture;

        public async Task ExpectException(ReceiptRequest receiptRequest, string errorMessage,
            bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false)
        {
            Func<Task> act = () =>
                CallSignProcessor_ExpectException(receiptRequest, sourceIsScuSwitch, targetIsScuSwitch)();
            await FluentActions.Invoking(act).Should().ThrowAsync<Exception>().WithMessage(errorMessage);
        }

        public async Task ExpectExceptionReceiptcase(ReceiptRequest receiptRequest, string errorMessage)
        {
            Func<Task> act = () => CallSignProcessor_ExpectException(receiptRequest)();
            await FluentActions.Invoking(act).Should().ThrowAsync<Exception>().WithMessage(string.Format(errorMessage, receiptRequest.ftReceiptCase));
        }

        public async Task ExpectActionJournalEntryForErrorState(ReceiptRequest receiptRequest,
            string expectedErrorMessage)
        {
            Func<Task> act = () => CallSignProcessor_ExpectException(receiptRequest)();
            await FluentActions.Invoking(act).Invoke();

            _fixture.ActionJournalRepositoryMock.Verify(
                aj => aj.InsertAsync(It.Is<ftActionJournal>(journal =>
                    journal.Message.Contains(expectedErrorMessage) && journal.Type == "ReceiptProcessError")),
                Times.Once,
                "Expected ActionJournal entry with specific error message was not created."
            );
        }

        public async Task Transaction_WithOpenTransactionRepo_ExpectArgumentException(string transactionFolder,
            int transNo, string expectedErrorMessage)
        {
            var receiptRequest = GetReceipt(transactionFolder, "test-reference", null);
            await _fixture.AddOpenOrders(receiptRequest.cbReceiptReference, transNo);

            Func<Task> act = () => CallSignProcessor_ExpectException(receiptRequest)();
            await FluentActions.Invoking(act).Should().ThrowAsync<ArgumentException>()
                .WithMessage(expectedErrorMessage);
        }

        public async Task ExpectArgumentExceptionReceiptcase(ReceiptRequest receiptRequest, string errorMessage)
        {
            Func<Task> act = () => CallSignProcessor_ExpectException(receiptRequest)();
            await FluentActions.Invoking(act).Should().ThrowAsync<ArgumentException>()
                .WithMessage(string.Format(errorMessage, receiptRequest.ftReceiptCase));
        }

        public async Task ExpectArgumentExceptionReceiptReference(ReceiptRequest receiptRequest, string errorMessage)
        {
            Func<Task> act = () => CallSignProcessor_ExpectException(receiptRequest)();
            await FluentActions.Invoking(act).Should().ThrowAsync<ArgumentException>()
                .WithMessage(string.Format(errorMessage, receiptRequest.cbReceiptReference));
        }

        public ReceiptRequest GetReceipt(string basefolder, string receiptReference, long? receiptCase)
        {
            var receiptRequest = TestObjectFactory.GetReceipt(Path.Combine("Data", basefolder));
            receiptRequest.ftReceiptCase = receiptCase ?? receiptRequest.ftReceiptCase;
            receiptRequest.cbReceiptReference = receiptReference;
            return receiptRequest;
        }

        private Func<Task> CallSignProcessor_ExpectException(ReceiptRequest receiptRequest,
            bool sourceIsScuSwitch = false, bool targetIsScuSwitch = false)
        {
            var signProcessor = _fixture.CreateSignProcessorForSignProcessorDE(false, DateTime.Now.AddHours(-1), null,
                null, false, false, sourceIsScuSwitch, targetIsScuSwitch);
            return async () => await signProcessor.ProcessAsync(receiptRequest);
        }
    }
}