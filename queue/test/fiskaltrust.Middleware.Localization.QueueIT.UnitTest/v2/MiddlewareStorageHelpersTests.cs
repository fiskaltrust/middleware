using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest.v2;

public class MiddlewareStorageHelpersTests
{
    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_EEEE_Tag_IfReceiptReference_IsNotAvailable()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> { }.ToAsyncEnumerable());
        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbTerminalID = "",
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse(), new ftQueueItem());
        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
        (result.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem()
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
                cbTerminalID = "",
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures.ToArray()
                })
            }
        }.ToAsyncEnumerable());
        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse
        {
            ftSignatures = signatures.ToArray()
        }, new ftQueueItem());
        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
        (result.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == documentNumberSignature.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == documentZNumber.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == documentMoment.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber) && x.Data == documentNumberSignature.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber) && x.Data == documentZNumber.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment) && x.Data == documentMoment.Data);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem_EvenIfTerminalIdIsSet()
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
                cbTerminalID = "",
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures.ToArray()
                })
            }
        }.ToAsyncEnumerable());
        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbTerminalID = "myterminalid",
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse
        {
            ftSignatures = signatures.ToArray()
        }, new ftQueueItem());
        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
        (result.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == documentNumberSignature.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == documentZNumber.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == documentMoment.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber) && x.Data == documentNumberSignature.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber) && x.Data == documentZNumber.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment) && x.Data == documentMoment.Data);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_ReferenceSignatures_IfLoadedReceipt_ContainsThem_AndUseRightReceipt_IfMatchesWithTerminalId()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var documentNumberSignature1 = new SignaturItem
        {
            Caption = "<doc-number>",
            Data = "1239",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber
        };
        var documentZNumber1 = new SignaturItem
        {
            Caption = "<z-number>",
            Data = "344",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber
        };
        var documentMoment1 = new SignaturItem
        {
            Caption = "<timestamp>",
            Data = "2024-23-01",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment
        };
        var signatures1 = new List<SignaturItem> {
            documentNumberSignature1,
            documentZNumber1,
            documentMoment1
        };

        var documentNumberSignature2 = new SignaturItem
        {
            Caption = "<doc-number>",
            Data = "11111",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber
        };
        var documentZNumber2 = new SignaturItem
        {
            Caption = "<z-number>",
            Data = "434",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber
        };
        var documentMoment2 = new SignaturItem
        {
            Caption = "<timestamp>",
            Data = "2024-01-01",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment
        };
        var signatures2 = new List<SignaturItem> {
            documentNumberSignature2,
            documentZNumber2,
            documentMoment2
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> {
            new ftQueueItem
            {
                cbTerminalID = "asdf",
                request = JsonConvert.SerializeObject(new ReceiptRequest
                {
                    cbTerminalID = "asdf", 
                }),
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures1.ToArray()
                })
            },
            new ftQueueItem
            {
                cbTerminalID = "myterminalid",
                request = JsonConvert.SerializeObject(new ReceiptRequest
                {
                    cbTerminalID = "myterminalid",
                }),
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures2.ToArray()
                })
            }
        }.ToAsyncEnumerable());

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbTerminalID = "myterminalid",
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse
        {
            ftSignatures = signatures2.ToArray()
        }, new ftQueueItem());
        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
        (result.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == documentNumberSignature2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == documentZNumber2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == documentMoment2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber) && x.Data == documentNumberSignature2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber) && x.Data == documentZNumber2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment) && x.Data == documentMoment2.Data);
    }

    [Fact]
    public async Task LoadReceiptReferencesToResponse_ShouldReturn_CorrectReceipt_IfLoadedReceipt_ContainsThem_AndUseRightReceipt_AndSkip_FailedReceipts_IfMatchesWithTerminalId()
    {
        var cbPreviousReceiptReference = Guid.NewGuid().ToString();
        var error = new SignaturItem
        {
            Caption = "FAILURE",
            Data = "Failed to process receiptcase 0x4954200008003010. with the following exception message: Object reference not set to an instance of an object.",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = 5283883447184535552
        };
        var signatures1 = new List<SignaturItem> {
            error
        };

        var documentNumberSignature2 = new SignaturItem
        {
            Caption = "<doc-number>",
            Data = "11111",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber
        };
        var documentZNumber2 = new SignaturItem
        {
            Caption = "<z-number>",
            Data = "434",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber
        };
        var documentMoment2 = new SignaturItem
        {
            Caption = "<timestamp>",
            Data = "2024-01-01",
            ftSignatureFormat = (long) SignaturItem.Formats.Text,
            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment
        };
        var signatures2 = new List<SignaturItem> {
            documentNumberSignature2,
            documentZNumber2,
            documentMoment2
        };

        var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItemRepositoryMock.Setup(x => x.GetByReceiptReferenceAsync(cbPreviousReceiptReference, null)).Returns(new List<ftQueueItem> 
        {
            new ftQueueItem
            {
                request = JsonConvert.SerializeObject(new ReceiptRequest
                {
                }),
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures1.ToArray(),
                    ftState =  Cases.BASE_STATE |0xEEEE_EEEE
                })
            },
            new ftQueueItem
            {
                request = JsonConvert.SerializeObject(new ReceiptRequest
                {
                }),
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures2.ToArray()
                })
            },
            new ftQueueItem
            {
                request = JsonConvert.SerializeObject(new ReceiptRequest
                {
                }),
                response  = JsonConvert.SerializeObject(new ReceiptResponse
                {
                    ftSignatures = signatures1.ToArray(),
                    ftState =  Cases.BASE_STATE |0xEEEE_EEEE
                })
            },
        }.ToAsyncEnumerable());

        var request = new ProcessCommandRequest(new ftQueue(), new ftQueueIT(), new ReceiptRequest
        {
            cbPreviousReceiptReference = cbPreviousReceiptReference
        }, new ReceiptResponse
        {
            ftSignatures = signatures2.ToArray()
        }, new ftQueueItem());
        var result = await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(queueItemRepositoryMock.Object, request.ReceiptRequest, request.QueueItem, request.ReceiptResponse);
        (result.ftState & 0xFFFF_FFFF).Should().NotBe(0xEEEE_EEEE);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber) && x.Data == documentNumberSignature2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber) && x.Data == documentZNumber2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentMoment) && x.Data == documentMoment2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentNumber) && x.Data == documentNumberSignature2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTZNumber) && x.Data == documentZNumber2.Data);
        result.ftSignatures.Should().Contain(x => x.ftSignatureType == (Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment) && x.Data == documentMoment2.Data);
    }
}