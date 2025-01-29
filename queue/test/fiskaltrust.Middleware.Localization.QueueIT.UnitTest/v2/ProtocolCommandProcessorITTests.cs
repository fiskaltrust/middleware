using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class ProtocolCommandProcessorITTests
{
    [Fact]
    public async Task CopyReceiptPrintExistingReceipt0x3010Async_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> { }.ToAsyncEnumerable());

        var itSSCDProvider = Mock.Of<IITSSCDProvider>(MockBehavior.Strict);
        var journalITRepository = Mock.Of<IJournalITRepository>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<ProtocolCommandProcessorIT>>();

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var processor = new ProtocolCommandProcessorIT(itSSCDProvider, journalITRepository, queueItemRepositoryMock.Object, logger);

        var result = await processor.CopyReceiptPrintExistingReceipt0x3010Async(request);

        (result.receiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task CopyReceiptPrintExistingReceipt0x3010Async_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();


        var documentNumberSignature = new SignaturItem
        {
            Caption = "<doc-number>",
            Data = "1239",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber
        };
        var documentZNumber = new SignaturItem
        {
            Caption = "<z-number>",
            Data = "344",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber
        };
        var documentMoment = new SignaturItem
        {
            Caption = "<timestamp>",
            Data = "2024-23-01",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment
        };
        var signatures = new List<SignaturItem> {
            documentNumberSignature,
            documentZNumber,
            documentMoment
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> {
        new ftQueueItem
            {
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures.ToArray()
                })
            }
        }.ToAsyncEnumerable());

        var itSSCDProviderMock = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        itSSCDProviderMock.Setup(itSSCDProviderMock => itSSCDProviderMock.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).ReturnsAsync((ProcessRequest request) => new ProcessResponse
        {
            ReceiptResponse = request.ReceiptResponse,
        });
        var journalITRepository = Mock.Of<IJournalITRepository>();
        var logger = Mock.Of<ILogger<ProtocolCommandProcessorIT>>();

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var processor = new ProtocolCommandProcessorIT(itSSCDProviderMock.Object, journalITRepository, queueItemRepositoryMock.Object, logger);

        var result = await processor.CopyReceiptPrintExistingReceipt0x3010Async(request);
        (result.receiptResponse.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == documentNumberSignature.Data);
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == documentZNumber.Data);
        result.receiptResponse.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == documentMoment.Data);

    }
}
