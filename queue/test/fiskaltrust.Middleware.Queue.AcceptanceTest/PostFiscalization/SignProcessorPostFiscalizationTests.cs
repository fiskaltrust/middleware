using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
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
        private const long ItReceiptCase = 0x4954_0000_0000_1001L; // an IT B2C invoice: invoice type nibble, no 0x2000 version nibble
        private const long ItPosReceiptCase = 0x4954_0000_0000_0001L;
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

            public ReceiptRequest Request(long ftReceiptCase = ItReceiptCase) => new ReceiptRequest
            {
                ftCashBoxID = CashBoxId.ToString(),
                ftQueueID = QueueId.ToString(),
                cbTerminalID = "T1",
                cbReceiptReference = "R-1",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCase = ftReceiptCase,
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

                var processor = new PostFiscalizationProcessor(Mock.Of<ILogger<PostFiscalizationProcessor>>(), eInvoicing, eReporting, PostFiscalizationMapper.LegacyFailureSignatureType, LegacyInvoiceReceiptCases.IsInvoiceDocument);
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
        public async Task NonInvoiceDocuments_NeverReachTheEInvoicingService()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService { OnValidate = _ => throw new InvalidOperationException("must not be asked for a POS receipt") };
            var sut = harness.Create(eInvoicing, null, (request, queueItem) => harness.Fiscalized(request, queueItem));

            var response = await sut.ProcessAsync(harness.Request(ItPosReceiptCase));

            eInvoicing.ValidateCalls.Should().BeEmpty();
            eInvoicing.ProcessCalls.Should().BeEmpty();
            response.ftState.Should().Be(ItSuccessState);
            response.ftStateData.Should().Contain("\"EInvoicing\":\"not-applicable\"");
            harness.ReceiptJournals.Should().ContainSingle();
        }

        [Fact]
        public async Task DeInvoiceCases_ReachTheEInvoicingServiceThroughTheAllowList()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService();
            var sut = harness.Create(eInvoicing, null, (request, queueItem) => harness.Fiscalized(request, queueItem));

            var response = await sut.ProcessAsync(harness.Request(0x4445_0000_0000_000CL)); // DE B2B-invoice

            eInvoicing.ValidateCalls.Should().ContainSingle();
            eInvoicing.ProcessCalls.Should().ContainSingle();
            response.ftStateData.Should().Contain("\"EInvoicing\":\"ok\"");
        }

        [Fact]
        public async Task V2InvoicesEmbeddedInTheV1CaseData_ReachTheEInvoicingService_WithoutAnAllowList()
        {
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService();
            var sut = harness.Create(eInvoicing, null, (request, queueItem) => harness.Fiscalized(request, queueItem));
            // The POS sent an AT v2 B2B invoice; the PosSystem API mapped it to a v1 case without a type nibble and embedded the original.
            var request = harness.Request(0x4154_0000_0000_0001L);
            request.ftReceiptCaseData = "{\"AT\":{},\"v2ReceiptRequest\":{\"ftReceiptCase\":" + 0x4154_2000_0000_1002UL + ",\"cbReceiptReference\":\"R-1\"}}";

            var response = await sut.ProcessAsync(request);

            eInvoicing.ValidateCalls.Should().ContainSingle().Which.ReceiptRequest.ftReceiptCaseData.Should().BeOfType<JsonElement>()
                .Which.GetProperty("v2ReceiptRequest").GetProperty("ftReceiptCase").GetUInt64().Should().Be(0x4154_2000_0000_1002UL, "the service judges the same embedded original");
            eInvoicing.ProcessCalls.Should().ContainSingle();
            response.ftStateData.Should().Contain("\"EInvoicing\":\"ok\"");
        }

        private const long SecurityMechanismDeactivatedFlag = 0x0000_0000_0000_0001L;

        [Theory]
        [InlineData(false, false)] // never activated
        [InlineData(true, true)]   // deactivated
        public async Task ReceiptsOnAQueueThatIsNotOperational_AreNotFinalized_ButKeepTheirReceiptJournal(bool started, bool stopped)
        {
            var harness = new Harness();
            harness.Queue.StartMoment = started ? DateTime.UtcNow.AddDays(-1) : (DateTime?) null;
            harness.Queue.StopMoment = stopped ? DateTime.UtcNow.AddHours(-1) : (DateTime?) null;
            var eReporting = new FakePostFiscalizationService();
            // The market's early exit: a non-error response carrying only the deactivated flag, nothing fiscalized.
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = ItSuccessState | SecurityMechanismDeactivatedFlag;
                response.ftSignatures = new SignaturItem[0];
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(ItPosReceiptCase));

            eReporting.ProcessCalls.Should().BeEmpty("nothing was fiscalized, so there is nothing to report");
            response.ftState.Should().Be(ItSuccessState | SecurityMechanismDeactivatedFlag);
            response.ftStateData.Should().NotContain("PostFiscalization");
            harness.ReceiptJournals.Should().ContainSingle("the receipt journal policy for these answers is unchanged");
        }

        [Theory]
        [InlineData(0x4445_0000_0000_0001L, 0x4445_0000_0000_0100L)] // DE: an SCU switch is in progress, the receipt was not signed
        [InlineData(0x4D45_0000_0000_0001L, 0x4D45_0000_0000_0004L)] // ME: the cash deposit of the day is outstanding, the receipt was not processed
        [InlineData(0x4D45_0000_0000_0001L, 0x4D45_0000_0000_0005L)] // ME: the cbReceiptReference was already used, the receipt was not processed
        public async Task NonFiscalizingAnswersOnAnOperationalQueue_AreNotFinalized_ButKeepTheirReceiptJournal(long receiptCase, long answerState)
        {
            var harness = new Harness();
            harness.Queue.StartMoment = DateTime.UtcNow.AddDays(-1);
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = answerState;
                response.ftSignatures = new SignaturItem[0];
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(receiptCase));

            eReporting.ProcessCalls.Should().BeEmpty("nothing was fiscalized, so there is nothing to report");
            response.ftState.Should().Be(answerState);
            response.ftStateData.Should().NotContain("PostFiscalization");
            harness.ReceiptJournals.Should().ContainSingle("the receipt journal policy for these answers is unchanged");
        }

        [Theory]
        [InlineData(0x4154_0000_0000_0003L)] // AT initial operation
        [InlineData(0x4154_0000_0000_0004L)] // AT out of operation
        public async Task AtLifecycleRequestsWithItems_AreAnsweredUnsigned_AndNotFinalized(long receiptCase)
        {
            var harness = new Harness();
            harness.Queue.StartMoment = DateTime.UtcNow.AddDays(-1);
            var eReporting = new FakePostFiscalizationService();
            // AT's lifecycle commands answer a request that carries charge or pay items with the unsigned default response.
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = 0x4154_0000_0000_0000L;
                response.ftSignatures = new SignaturItem[0];
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(receiptCase));

            eReporting.ProcessCalls.Should().BeEmpty("the request was rejected, nothing was signed");
            response.ftState.Should().Be(0x4154_0000_0000_0000L);
            response.ftStateData.Should().NotContain("PostFiscalization");
            harness.ReceiptJournals.Should().ContainSingle("the receipt journal policy for these answers is unchanged");
        }

        [Theory]
        [InlineData(true, false)] // already activated
        [InlineData(true, true)]  // deactivated
        public async Task ARepeatedMeInitialOperation_IsAnsweredWithoutProcessing_AndNotFinalized(bool started, bool stopped)
        {
            var harness = new Harness();
            harness.Queue.StartMoment = started ? DateTime.UtcNow.AddDays(-1) : (DateTime?) null;
            harness.Queue.StopMoment = stopped ? DateTime.UtcNow.AddHours(-1) : (DateTime?) null;
            var eReporting = new FakePostFiscalizationService();
            // ME answers the repeated request with the plain success state, no signatures and an action journal entry.
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = 0x4D45_0000_0000_0000L;
                response.ftSignatures = new SignaturItem[0];
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(0x4D45_0000_0000_0003L));

            eReporting.ProcessCalls.Should().BeEmpty("the request was rejected, the TCR was not registered");
            response.ftState.Should().Be(0x4D45_0000_0000_0000L);
            response.ftStateData.Should().NotContain("PostFiscalization");
            harness.ReceiptJournals.Should().ContainSingle("the receipt journal policy for these answers is unchanged");
        }

        [Fact]
        public async Task TheMeInitialOperationThatRegistersTheTcr_IsFinalized()
        {
            var harness = new Harness();
            harness.Queue.StartMoment = null;
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                harness.Queue.StartMoment = DateTime.UtcNow;
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = 0x4D45_0000_0000_0000L;
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(0x4D45_0000_0000_0003L));

            eReporting.ProcessCalls.Should().ContainSingle();
            response.ftStateData.Should().Contain("\"EReporting\":\"ok\"");
        }

        [Fact]
        public async Task ASignedAtReceipt_IsFinalized()
        {
            var harness = new Harness();
            harness.Queue.StartMoment = DateTime.UtcNow.AddDays(-1);
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = 0x4154_0000_0000_0002L; // SSCD failed mode: still signed, with the failure QR code
                response.ftSignatures = new[] { new SignaturItem { Caption = "Sicherheitseinrichtung ausgefallen", Data = "...", ftSignatureFormat = 3, ftSignatureType = 0x4154_0000_0000_0001L } };
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(0x4154_0000_0000_0001L));

            eReporting.ProcessCalls.Should().ContainSingle();
            response.ftStateData.Should().Contain("\"EReporting\":\"ok\"");
        }

        [Fact]
        public async Task AMappingFailure_MarksTheReceiptAsFailed_ButPersistsTheFiscalizationMarker()
        {
            // The IT processor fiscalized the receipt, but its response cannot be converted to the v2 contract (a signature
            // type the v2 contract cannot represent), so no service gets it. The error state and the failed signature say so;
            // the persisted outcome must still carry FiscalizationSucceeded, or a later refund, void or reprint would lose
            // the RT document references.
            var harness = new Harness();
            var eInvoicing = new FakePostFiscalizationService();
            var sut = harness.Create(eInvoicing, null, (request, queueItem) =>
            {
                var response = harness.Fiscalized(request, queueItem);
                response.ftSignatures = response.ftSignatures.Concat(new[] { new SignaturItem { Caption = "unrepresentable", Data = "x", ftSignatureFormat = 1, ftSignatureType = -1L } }).ToArray();
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request());

            eInvoicing.ValidateCalls.Should().ContainSingle();
            eInvoicing.ProcessCalls.Should().BeEmpty();
            (response.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
            response.ftSignatures.Select(signature => signature.Caption).Should().Equal("rt-document", "unrepresentable", "einvoicing-failed");
            response.ftSignatures.Last().Data.Should().Contain("could not be converted between the v1 and the v2 contract");
            response.ftStateData.Should().Contain("\"FiscalizationSucceeded\":true").And.Contain("\"EInvoicing\":\"failed\"").And.Contain("\"EReporting\":\"disabled\"");
            response.ftStateData.Should().Contain("\"IT\"", "the market's own state data is kept");
            response.IsFiscalized().Should().BeTrue("the v1 predicate reads the marker");
            harness.ReceiptJournals.Should().ContainSingle();
            harness.ActionJournals.Should().Contain(journal => journal.Type == "einvoicing-failed");
        }

        [Fact]
        public async Task TheInitialOperationReceipt_MakesTheQueueOperational_AndIsFinalized()
        {
            var harness = new Harness();
            harness.Queue.StartMoment = null;
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                harness.Queue.StartMoment = DateTime.UtcNow;
                return harness.Fiscalized(request, queueItem);
            });

            var response = await sut.ProcessAsync(harness.Request(0x4954_0000_0000_4001L));

            eReporting.ProcessCalls.Should().ContainSingle();
            response.ftStateData.Should().Contain("\"EReporting\":\"ok\"");
        }

        [Fact]
        public async Task TheOutOfOperationReceipt_IsFinalized_AlthoughItCarriesTheDeactivatedFlag()
        {
            var harness = new Harness();
            harness.Queue.StartMoment = DateTime.UtcNow.AddDays(-1);
            var eReporting = new FakePostFiscalizationService();
            var sut = harness.Create(null, eReporting, (request, queueItem) =>
            {
                harness.Queue.StopMoment = DateTime.UtcNow;
                var response = harness.Fiscalized(request, queueItem);
                response.ftState = ItSuccessState | SecurityMechanismDeactivatedFlag;
                return response;
            });

            var response = await sut.ProcessAsync(harness.Request(0x4954_0000_0000_4002L));

            eReporting.ProcessCalls.Should().ContainSingle("the out-of-operation receipt is fiscalized on the still operational queue");
            response.ftStateData.Should().Contain("\"EReporting\":\"ok\"");
            response.ftState.Should().Be(ItSuccessState | SecurityMechanismDeactivatedFlag);
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
