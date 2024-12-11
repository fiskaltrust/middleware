using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ReceiptCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT) : IReceiptCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT = signaturCreationUnitPT;

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase & 0xFFFF;
        switch (receiptCase)
        {
            case (int) ReceiptCases.UnknownReceipt0x0000:
                return await UnknownReceipt0x0000Async(request);
            case (int) ReceiptCases.PointOfSaleReceipt0x0001:
                return await PointOfSaleReceipt0x0001Async(request);
            case (int) ReceiptCases.PaymentTransfer0x0002:
                return await PaymentTransfer0x0002Async(request);
            case (int) ReceiptCases.PointOfSaleReceiptWithoutObligation0x0003:
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);
            case (int) ReceiptCases.ECommerce0x0004:
                return await ECommerce0x0004Async(request);
            case (int) ReceiptCases.Protocol0x0005:
                return await Protocol0x0005Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase(request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, _queuePT.LastHash);
        response.ReceiptResponse.ftReceiptIdentification = "FS " + _queuePT.SimplifiedInvoiceSeries + "/" + (++_queuePT.SimplifiedInvoiceSeriesNumerator).ToString().PadLeft(4, '0');
        var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCodeAnonymousCustomer(hash, _queuePT, _signaturCreationUnitPT, request.ReceiptRequest, response.ReceiptResponse);
        response.ReceiptResponse.AddSignatureItem(new Api.POS.Models.ifPOS.v2.SignatureItem
        {
            Caption = "ATCUD",
            Data = _queuePT.ATCUD,
            ftSignatureFormat = 0x0001,
            ftSignatureType = (long) SignatureTypesPT.ATCUD,
        });
        response.ReceiptResponse.AddSignatureItem(new Api.POS.Models.ifPOS.v2.SignatureItem
        {
            Caption = "Hash",
            Data = hash,
            ftSignatureFormat = 0x0000_0000_0001_0001,
            ftSignatureType = (long) SignatureTypesPT.Hash,
        });
        response.ReceiptResponse.AddSignatureItem(new Api.POS.Models.ifPOS.v2.SignatureItem
        {
            Caption = "Hash",
            Data = hash,
            ftSignatureFormat = 0x0001,
            ftSignatureType = (long) SignatureTypesPT.HashPrint,
        });
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
        _queuePT.LastHash = hash;
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
