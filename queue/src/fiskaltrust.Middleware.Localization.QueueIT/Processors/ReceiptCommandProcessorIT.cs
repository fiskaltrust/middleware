using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// The "documento commerciale": the RT device numbers and prints the document, the queue derives the receipt
/// identification (Z number and document number) from the RT signatures and journals it.
/// </summary>
public class ReceiptCommandProcessorIT(IITSSCDProvider sscd, AsyncLazy<IMiddlewareJournalITRepository> journalITRepository, AsyncLazy<IConfigurationRepository> configurationRepository) : IReceiptCommandProcessor
{
    private readonly IITSSCDProvider _sscd = sscd;
    private readonly AsyncLazy<IMiddlewareJournalITRepository> _journalITRepository = journalITRepository;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var isRefundOrVoid = receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) || receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
        if (isRefundOrVoid && !ReceiptReferences.TryAddReferenceSignatures(receiptRequest, receiptResponse))
        {
            return new ProcessCommandResponse(receiptResponse, []);
        }

        var result = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }).ConfigureAwait(false);
        if (result.ReceiptResponse.HasFailed())
        {
            return new ProcessCommandResponse(result.ReceiptResponse, []);
        }

        var documentNumber = result.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTDocumentNumber)?.Data;
        var zNumber = result.ReceiptResponse.GetSignatureItem(SignatureTypeIT.RTZNumber)?.Data;
        if (documentNumber is null || zNumber is null)
        {
            receiptResponse.SetReceiptResponseError(ErrorMessagesIT.MissingRTDocumentIdentification);
            return new ProcessCommandResponse(receiptResponse, []);
        }

        // An SCU that numbers the document itself replaces the identification; otherwise the RT numbering is appended to the queue's "ft...#".
        if (result.ReceiptResponse.ftReceiptIdentification is { } scuReceiptIdentification && !scuReceiptIdentification.EndsWith('#'))
        {
            receiptResponse.ftReceiptIdentification = scuReceiptIdentification;
        }
        else
        {
            receiptResponse.ftReceiptIdentification += $"{zNumber.PadLeft(4, '0')}-{documentNumber.PadLeft(4, '0')}";
        }

        receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
        receiptResponse.InsertSignatureItems(SignaturItemFactory.CreatePOSReceiptFormatSignatures(receiptResponse));

        var queueIT = await (await _configurationRepository).GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
        await (await _journalITRepository).InsertAsync(ftJournalITFactory.CreateFrom(receiptResponse, queueIT, new ScuResponse
        {
            ftReceiptCase = receiptRequest.ftReceiptCase,
            ReceiptNumber = long.Parse(documentNumber),
            ZRepNumber = long.Parse(zNumber)
        })).ConfigureAwait(false);
        return new ProcessCommandResponse(receiptResponse, []);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => ITFallBackOperations.NotSupported(request, "TableCheck");

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => ITFallBackOperations.NotSupported(request, "ProForma");
}
