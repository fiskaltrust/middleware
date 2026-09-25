using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;
using Moq;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest;

public static class TestHelpers
{
    public const ulong BaseState = 0x4954_2000_0000_0000;
    public const string CashBoxIdentification = "00040005";
    public const string RTSerialNumber = "96SRT001239";

    public static ftQueue CreateQueue(Guid? queueId = null) => new()
    {
        ftQueueId = queueId ?? Guid.NewGuid(),
        CountryCode = "IT",
        ftReceiptNumerator = 1,
    };

    public static ftQueueItem CreateQueueItem(ftQueue queue) => new()
    {
        ftQueueId = queue.ftQueueId,
        ftQueueItemId = Guid.NewGuid(),
    };

    public static ftQueueIT CreateQueueIT(ftQueue queue, Guid? scuId = null) => new()
    {
        ftQueueITId = queue.ftQueueId,
        ftSignaturCreationUnitITId = scuId ?? Guid.NewGuid(),
        CashBoxIdentification = CashBoxIdentification,
    };

    public static ftSignaturCreationUnitIT CreateSignaturCreationUnitIT(ftQueueIT queueIT, string url = "grpc://localhost:1400", string? infoJson = null) => new()
    {
        ftSignaturCreationUnitITId = queueIT.ftSignaturCreationUnitITId!.Value,
        Url = url,
        InfoJson = infoJson!,
    };

    public static ReceiptRequest CreateRequest(ReceiptCase receiptCase, ulong flags = 0, string? cbPreviousReceiptReference = null)
    {
        var request = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftPosSystemId = Guid.NewGuid(),
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [],
            cbPayItems = [],
            ftReceiptCase = (ReceiptCase) (BaseState | (ulong) receiptCase | flags),
        };
        if (cbPreviousReceiptReference is not null)
        {
            request.cbPreviousReceiptReference = cbPreviousReceiptReference;
        }
        return request;
    }

    public static ReceiptResponse CreateResponse(ftQueue queue, ftQueueItem queueItem, ReceiptRequest request) => new()
    {
        ftCashBoxID = request.ftCashBoxID,
        ftQueueID = queue.ftQueueId,
        ftQueueItemID = queueItem.ftQueueItemId,
        ftQueueRow = 1,
        cbTerminalID = request.cbTerminalID,
        cbReceiptReference = request.cbReceiptReference,
        ftCashBoxIdentification = CashBoxIdentification,
        ftReceiptMoment = DateTime.UtcNow,
        ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#",
        ftState = (State) BaseState,
    };

    public static SignatureItem RTSignature(SignatureTypeIT type, string caption, string data) => new()
    {
        Caption = caption,
        Data = data,
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = type.As<SignatureType>(),
    };

    /// <summary>
    /// The signatures an Italian SCU puts on a processed document (see SCU.IT.Abstraction SignatureFactory.CreateDocumentoCommercialeSignatures).
    /// </summary>
    public static List<SignatureItem> CreateRTSignatures(long zNumber, long documentNumber, DateTime documentMoment, string documentType = "POSRECEIPT", string? lotteryCode = null, string? customerId = null, string? shaMetadata = null)
    {
        var signatures = new List<SignatureItem>
        {
            RTSignature(SignatureTypeIT.RTSerialNumber, "<rt-serialnumber>", RTSerialNumber),
            RTSignature(SignatureTypeIT.RTZNumber, "<rt-z-number>", zNumber.ToString().PadLeft(4, '0')),
            RTSignature(SignatureTypeIT.RTDocumentNumber, "<rt-doc-number>", documentNumber.ToString().PadLeft(4, '0')),
            RTSignature(SignatureTypeIT.RTDocumentMoment, "<rt-doc-moment>", documentMoment.ToString("yyyy-MM-dd HH:mm:ss")),
            RTSignature(SignatureTypeIT.RTDocumentType, "<rt-document-type>", documentType),
        };
        if (shaMetadata is not null)
        {
            signatures.Add(RTSignature(SignatureTypeIT.RTServerShaMetadata, "<rt-server-shametadata>", shaMetadata));
        }
        if (lotteryCode is not null)
        {
            signatures.Add(RTSignature(SignatureTypeIT.RTLotteryID, "<rt-lottery-id>", lotteryCode));
        }
        if (customerId is not null)
        {
            signatures.Add(RTSignature(SignatureTypeIT.RTCustomerID, "<rt-customer-id>", customerId));
        }
        return signatures;
    }

    public static AsyncLazy<T> Lazy<T>(T value) => new(() => Task.FromResult(value));

    public static Mock<IConfigurationRepository> CreateConfigurationRepository(ftQueueIT queueIT, ftSignaturCreationUnitIT? signaturCreationUnitIT = null)
    {
        var repository = new Mock<IConfigurationRepository>();
        repository.Setup(x => x.GetQueueITAsync(queueIT.ftQueueITId)).ReturnsAsync(queueIT);
        repository.Setup(x => x.InsertOrUpdateQueueITAsync(It.IsAny<ftQueueIT>())).Returns(Task.CompletedTask);
        if (signaturCreationUnitIT is not null)
        {
            repository.Setup(x => x.GetSignaturCreationUnitITAsync(signaturCreationUnitIT.ftSignaturCreationUnitITId)).ReturnsAsync(signaturCreationUnitIT);
        }
        repository.Setup(x => x.InsertOrUpdateSignaturCreationUnitITAsync(It.IsAny<ftSignaturCreationUnitIT>())).Returns(Task.CompletedTask);
        return repository;
    }

    public static Mock<IMiddlewareJournalITRepository> CreateJournalITRepository()
    {
        var repository = new Mock<IMiddlewareJournalITRepository>();
        repository.Setup(x => x.InsertAsync(It.IsAny<ftJournalIT>())).Returns(Task.CompletedTask);
        return repository;
    }

    /// <summary>
    /// An SCU that answers with the response it was given, optionally changed by <paramref name="handler"/>.
    /// </summary>
    public static Mock<IITSSCDProvider> CreateSscd(Action<ProcessRequest>? handler = null, string? serialNumber = RTSerialNumber)
    {
        var sscd = new Mock<IITSSCDProvider>(MockBehavior.Strict);
        sscd.Setup(x => x.GetRTInfoAsync()).ReturnsAsync(new ifPOS.v1.it.RTInfo { SerialNumber = serialNumber, InfoData = "{}" });
        sscd.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>())).ReturnsAsync((ProcessRequest request) =>
        {
            handler?.Invoke(request);
            return new ProcessResponse { ReceiptResponse = request.ReceiptResponse };
        });
        return sscd;
    }

    /// <summary>
    /// What the v2 SignProcessor leaves in the response when the request references a previous receipt.
    /// </summary>
    public static void SetPreviousReceipt(ReceiptResponse response, params (ReceiptRequest request, ReceiptResponse response)[] referencedReceipts)
    {
        response.ftStateData = new MiddlewareStateData
        {
            PreviousReceiptReference = referencedReceipts.Select(x => new Receipt { Request = x.request, Response = x.response }).ToList()
        };
    }

    public static ulong State(this ReceiptResponse response) => (ulong) response.ftState;
}
