using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.PostFiscalization;
using fiskaltrust.Middleware.PostFiscalization.Contracts;
using fiskaltrust.Middleware.Queue.PostFiscalization;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest.PostFiscalization
{
    /// <summary>
    /// The RFC 712 seams in the legacy <see cref="SignProcessor"/>, exercised with scripted services: the v1 pair is mapped
    /// to the v2 contract at the boundary and the result merged back.
    /// </summary>
    public class SignProcessorPostFiscalizationTests
    {
        private const long ItReceiptCase = 0x4954_0000_0000_0001L; // a v1 request without the 0x2000 version nibble
        private const long ItSuccessState = 0x4954_0000_0000_0000L;

        private sealed class Harness
        {
            public Guid QueueId { get; } = Guid.NewGuid();
            public Guid CashBoxId { get; } = Guid.NewGuid();
            public ftQueue Queue { get; }
            public MiddlewareConfiguration Configuration { get; }
            public Mock<IMarketSpecificSignProcessor> CountryProcessor { get; } = new Mock<IMarketSpecificSignProcessor>(MockBehavior.Strict);
            public List<ftQueueItem> PersistedQueueItems { get; } = new List<ftQueueItem>();
            public List<ftActionJournal> ActionJournals { get; } = new List<ftActionJournal>();
            public List<ftReceiptJournal> ReceiptJournals { get; } = new List<ftReceiptJournal>();

            public Harness()
            {
                Queue = new ftQueue { ftCashBoxId = CashBoxId, ftQueueId = QueueId, ftCurrentRow = 1 };
                Configuration = new MiddlewareConfiguration { QueueId = QueueId, CashBoxId = CashBoxId, ProcessingVersion = "test" };
            }

            public ReceiptRequest Request() => new ReceiptRequest
            {
                ftCashBoxID = CashBoxId.ToString(),
                ftQueueID = QueueId.ToString(),
                cbTerminalID = "T1",
                cbReceiptReference = "R-1",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = ItReceiptCase,
                cbCustomer = "{\"CustomerName\":\"Max\"}",
                cbChargeItems = new[] { new ChargeItem { Amount = 12.5m, Quantity = 1, Description = "Item", ftChargeItemCase = 0x4954_0000_0000_0001L } },
                cbPayItems = new PayItem[0],
            };

            public ReceiptResponse Fiscalized(ReceiptRequest request, ftQueueItem queueItem) => new ReceiptResponse
            {
                ftCashBoxID = CashBoxId.ToString(),
                ftQueueID = QueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftCashBoxIdentification = "CB-1",
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = ItSuccessState,
                ftStateData = "{\"IT\":{\"Document\":1}}",
                ftSignatures = new[] { new SignaturItem { Caption = "rt-document", Data = "0001-0001", ftSignatureFormat = 1, ftSignatureType = 0x4954_0000_0000_0010L } },
            };

            public SignProcessor Create(FakePostFiscalizationService eInvoicing, FakePostFiscalizationService eReporting, Func<ReceiptRequest, ftQueueItem, ReceiptResponse> countryResponse)
            {
                var configurationRepository = new Mock<IConfigurationRepository>(MockBehavior.Strict);
                configurationRepository.Setup(x => x.GetQueueAsync(QueueId)).ReturnsAsync(Queue);
                configurationRepository.Setup(x => x.InsertOrUpdateQueueAsync(Queue)).Returns(Task.CompletedTask);
                var receiptJournalRepository = new Mock<IMiddlewareReceiptJournalRepository>(MockBehavior.Strict);
                receiptJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftReceiptJournal>())).Callback<ftReceiptJournal>(ReceiptJournals.Add).Returns(Task.CompletedTask);
                var actionJournalRepository = new Mock<IMiddlewareActionJournalRepository>(MockBehavior.Strict);
                actionJournalRepository.Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>())).Callback<ftActionJournal>(ActionJournals.Add).Returns(Task.CompletedTask);
                var cryptoHelper = new Mock<ICryptoHelper>(MockBehavior.Strict);
                cryptoHelper.Setup(x => x.GenerateBase64Hash(It.IsAny<string>())).Returns("hash");
                cryptoHelper.Setup(x => x.GenerateBase64ChainHash(It.IsAny<string>(), It.IsAny<ftReceiptJournal>(), It.IsAny<ftQueueItem>())).Returns("chain");
                var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
                queueItemRepository.Setup(x => x.InsertOrUpdateAsync(It.IsAny<ftQueueItem>())).Callback<ftQueueItem>(PersistedQueueItems.Add).Returns(Task.CompletedTask);

                CountryProcessor.Setup(x => x.FirstTaskAsync()).Returns(Task.CompletedTask);
                CountryProcessor.Setup(x => x.GetFtCashBoxIdentificationAsync(Queue)).ReturnsAsync("CB-1");
                CountryProcessor.Setup(x => x.FinalTaskAsync(Queue, It.IsAny<ftQueueItem>(), It.IsAny<ReceiptRequest>(), actionJournalRepository.Object, It.IsAny<IMiddlewareQueueItemRepository>(), receiptJournalRepository.Object)).Returns(Task.CompletedTask);
                if (countryResponse != null)
                {
                    CountryProcessor.Setup(x => x.ProcessAsync(It.IsAny<ReceiptRequest>(), Queue, It.IsAny<ftQueueItem>()))
                        .Returns<ReceiptRequest, ftQueue, ftQueueItem>((request, _, queueItem) => Task.FromResult((countryResponse(request, queueItem), new List<ftActionJournal>())));
                }

                var processor = new PostFiscalizationProcessor(Mock.Of<ILogger<PostFiscalizationProcessor>>(), eInvoicing, eReporting, PostFiscalizationMapper.LegacyFailureSignatureType);
                return new SignProcessor(Mock.Of<ILogger<SignProcessor>>(), configurationRepository.Object, queueItemRepository.Object, receiptJournalRepository.Object, actionJournalRepository.Object, cryptoHelper.Object, CountryProcessor.Object, Configuration, processor);
            }
        }

        [Fact]
        public async Task PreflightRejection_RefusesTheReceipt_WithoutQueueItemFiscalizationOrReceiptJournal()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService
            {
                OnValidate = _ => Task.FromResult(new ValidateResponse { Applies = true, Errors = new List<string> { "cbCustomer is required for B2B invoices" } }),
            };
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(eInvoicing, eReporting, countryResponse: null);
            var request = harness.Request();

            ReceiptResponse response = null;
            Func<Task> act = async () => response = await sut.ProcessAsync(request);

            await act.Should().NotThrowAsync("a rejection is returned like the other pre-fiscalization exits, even for a v1 request");
            response.Should().NotBeNull();
            response.ftState.Should().Be(unchecked((long) 0x4954_2000_FFFF_FFFFUL), "a response without a queue item carries the fail state, not the error state of a processed receipt");
            response.ftQueueItemID.Should().Be(Guid.Empty.ToString());
            response.ftQueueID.Should().Be(harness.QueueId.ToString());
            response.ftCashBoxID.Should().Be(harness.CashBoxId.ToString());
            response.ftCashBoxIdentification.Should().Be("CB-1");
            response.ftQueueRow.Should().Be(0);
            response.cbReceiptReference.Should().Be("R-1");
            var signature = response.ftSignatures.Should().ContainSingle().Subject;
            signature.Caption.Should().Be("einvoicing-rejected");
            signature.Data.Should().Be("cbCustomer is required for B2B invoices");
            signature.ftSignatureFormat.Should().Be(0x1);
            signature.ftSignatureType.Should().Be(unchecked((long) 0x4954_2000_0000_3000UL));

            harness.PersistedQueueItems.Should().BeEmpty("no queue item is created for a receipt refused before fiscalization");
            harness.ReceiptJournals.Should().BeEmpty();
            eReporting.ValidateCalls.Should().BeEmpty();
            eInvoicing.ProcessCalls.Should().BeEmpty();
            var journal = harness.ActionJournals.Should().ContainSingle().Subject;
            journal.Type.Should().Be("einvoicing-rejected");
            journal.ftQueueId.Should().Be(harness.QueueId);
            journal.ftQueueItemId.Should().Be(Guid.Empty);
            journal.DataJson.Should().Contain("\"phase\":\"validate\"").And.Contain("\"outcome\":\"rejected\"");

            var v2Request = eInvoicing.ValidateCalls.Should().ContainSingle().Subject.ReceiptRequest;
            v2Request.cbReceiptReference.Should().Be("R-1");
            v2Request.ftCashBoxID.Should().Be(harness.CashBoxId);
            v2Request.ftReceiptCase.Should().Be((V2.Cases.ReceiptCase) ItReceiptCase);
            v2Request.cbCustomer.Should().BeOfType<JsonElement>().Which.GetProperty("CustomerName").GetString().Should().Be("Max");
        }

        [Fact]
        public async Task PreflightTransportFailure_RefusesTheReceipt_NamingTheCause()
        {
            var harness = new Harness();
            var eReporting = new FakePostFiscalizationService { OnValidate = _ => throw new PostFiscalizationServiceException("timeout after 2 attempt(s) of 15000 ms each") };
            var sut = harness.Create(new FakePostFiscalizationService(), eReporting, countryResponse: null);

            var response = await sut.ProcessAsync(harness.Request());

            response.ftState.Should().Be(unchecked((long) 0x4954_2000_FFFF_FFFFUL));
            var signature = response.ftSignatures.Should().ContainSingle().Subject;
            signature.Caption.Should().Be("ereporting-rejected");
            signature.Data.Should().Be("eReporting validate call failed: timeout after 2 attempt(s) of 15000 ms each");
            harness.PersistedQueueItems.Should().BeEmpty();
            harness.ActionJournals.Should().ContainSingle().Which.DataJson.Should().Contain("\"outcome\":\"failed\"");
        }

        [Fact]
        public async Task FinalizeSuccess_MergesTheServiceSignaturesAndStateDataIntoTheV1Response()
        {
            var harness = new Harness();
            object stateDataSeenByTheService = null;
            var eInvoicing = new FakePostFiscalizationService
            {
                OnProcess = request =>
                {
                    stateDataSeenByTheService = request.ReceiptResponse.ftStateData;
                    request.ReceiptResponse.ftSignatures.Add(new V2.SignatureItem { Caption = "einvoice-id", Data = "urn:peppol:1", ftSignatureFormat = V2.Cases.SignatureFormat.Text, ftSignatureType = (V2.Cases.SignatureType) 0x4954_2000_0000_0010UL });
                    return Task.FromResult(new ProcessResponse { ReceiptResponse = request.ReceiptResponse });
                },
            };
            var sut = harness.Create(eInvoicing, null, (request, queueItem) => harness.Fiscalized(request, queueItem));

            var response = await sut.ProcessAsync(harness.Request());

            response.ftState.Should().Be(ItSuccessState);
            response.ftSignatures.Select(signature => signature.Caption).Should().Equal("rt-document", "einvoice-id");
            var added = response.ftSignatures[1];
            added.Data.Should().Be("urn:peppol:1");
            added.ftSignatureFormat.Should().Be(1);
            added.ftSignatureType.Should().Be(unchecked((long) 0x4954_2000_0000_0010UL));
            response.ftStateData.Should().Contain("\"IT\":{\"Document\":1}").And.Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"ok\"").And.Contain("\"EReporting\":\"disabled\"");
            response.ftReceiptIdentification.Should().Be("ft1#");
            harness.ReceiptJournals.Should().ContainSingle();
            harness.ActionJournals.Should().BeEmpty();
            harness.PersistedQueueItems.Last().response.Should().Contain("einvoice-id").And.Contain("PostFiscalization");

            var processRequest = eInvoicing.ProcessCalls.Should().ContainSingle().Subject;
            processRequest.ReceiptResponse.ftQueueItemID.Should().Be(harness.PersistedQueueItems.Last().ftQueueItemId);
            processRequest.ReceiptResponse.ftReceiptIdentification.Should().Be("ft1#");
            stateDataSeenByTheService.Should().BeOfType<JsonElement>().Which.GetProperty("IT").GetProperty("Document").GetInt32().Should().Be(1);
            processRequest.ReceiptResponse.ftSignatures.Should().Contain(signature => signature.Caption == "rt-document", "the service sees everything the queue and the RT device produced");
        }

        [Fact]
        public async Task FinalizeFailure_ReturnsTheErrorResponseInsteadOfThrowing_AndStillCreatesTheReceiptJournal()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService { OnProcess = _ => throw new PostFiscalizationServiceException("HTTP 503 Service Unavailable after 2 attempts", "maintenance") };
            var sut = harness.Create(eInvoicing, null, (request, queueItem) => harness.Fiscalized(request, queueItem));

            ReceiptResponse response = null;
            Func<Task> act = async () => response = await sut.ProcessAsync(harness.Request());

            await act.Should().NotThrowAsync("there is no processor exception to rethrow: the receipt is fiscalized");
            // the error state is set on the response the RT device produced, so its country and version bits are kept
            response.ftState.Should().Be(unchecked((long) 0x4954_0000_EEEE_EEEEUL));
            (response.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
            response.ftSignatures.Select(signature => signature.Caption).Should().Equal("rt-document", "einvoicing-failed");
            var failure = response.ftSignatures[1];
            failure.Data.Should().Contain("HTTP 503 Service Unavailable after 2 attempts");
            failure.ftSignatureFormat.Should().Be(1);
            failure.ftSignatureType.Should().Be(unchecked((long) 0x4954_2000_0000_3000UL));
            response.ftStateData.Should().Contain("\"IT\":{\"Document\":1}").And.Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"failed\"");

            harness.ReceiptJournals.Should().ContainSingle("the receipt is fiscalized, so it is journaled although it carries an error state");
            var persisted = harness.PersistedQueueItems.Last();
            var persistedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ReceiptResponse>(persisted.response);
            persistedResponse.ftSignatures.Should().Contain(signature => signature.Caption == "einvoicing-failed");
            persistedResponse.ftStateData.Should().Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"failed\"");
            var journal = harness.ActionJournals.Should().ContainSingle().Subject;
            journal.Type.Should().Be("einvoicing-failed");
            journal.ftQueueItemId.Should().Be(persisted.ftQueueItemId);
            journal.DataJson.Should().Contain("maintenance");
        }

        [Fact]
        public async Task FiscalizationFailure_SkipsFinalize_AndKeepsTheLegacyErrorHandling()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService();
            var sut = harness.Create(eInvoicing, null, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = PostFiscalizationMapper.LegacyErrorState(request.ftReceiptCase);
                response.ftSignatures = new SignaturItem[0];
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request());

            eInvoicing.ValidateCalls.Should().ContainSingle();
            eInvoicing.ProcessCalls.Should().BeEmpty();
            response.ftStateData.Should().NotContain("PostFiscalization");
            harness.ReceiptJournals.Should().BeEmpty();
            harness.ActionJournals.Should().ContainSingle().Which.Message.Should().Contain("0xEEEE_EEEE");
        }

        [Fact]
        public async Task NotApplicableServices_LeaveTheReceiptUntouched_ButRecordTheOutcome()
        {
            var harness = new Harness();
            var notApplying = new FakePostFiscalizationService { OnValidate = _ => Task.FromResult(new ValidateResponse { Applies = false }) };
            var sut = harness.Create(notApplying, null, (request, queueItem) => harness.Fiscalized(request, queueItem));

            var response = await sut.ProcessAsync(harness.Request());

            notApplying.ProcessCalls.Should().BeEmpty();
            response.ftState.Should().Be(ItSuccessState);
            response.ftSignatures.Select(signature => signature.Caption).Should().Equal("rt-document");
            response.ftStateData.Should().Contain("\"EInvoicing\":\"not-applicable\"");
            harness.ReceiptJournals.Should().ContainSingle();
        }
    }
}
