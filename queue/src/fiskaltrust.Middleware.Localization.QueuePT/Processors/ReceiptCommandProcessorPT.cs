using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Text;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ReceiptCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT, IMiddlewareQueueItemRepository readOnlyQueueItemRepository) : IReceiptCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
#pragma warning disable
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT = signaturCreationUnitPT;
    private readonly IMiddlewareQueueItemRepository _readOnlyQueueItemRepository = readOnlyQueueItemRepository;

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        await StaticNumeratorStorage.LoadStorageNumbers(_readOnlyQueueItemRepository);
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(request.ReceiptRequest);
        var receiptCase = request.ReceiptRequest.ftReceiptCase.Case();
        switch (receiptCase)
        {
            case ReceiptCase.UnknownReceipt0x0000:
                return await UnknownReceipt0x0000Async(request);
            case ReceiptCase.PointOfSaleReceipt0x0001:
                return await PointOfSaleReceipt0x0001Async(request);
            case ReceiptCase.PaymentTransfer0x0002:
                return await PaymentTransfer0x0002Async(request);
            case ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003:
                return await PointOfSaleReceiptWithoutObligation0x0003Async(request);
            case ReceiptCase.ECommerce0x0004:
                return await ECommerce0x0004Async(request);
            case ReceiptCase.Protocol0x0005:
                return await Protocol0x0005Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase((long) request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var series = StaticNumeratorStorage.CreditNoteSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = $"Referencia {receiptReference.ftReceiptIdentification}",
                Data = $"Razão: Devolução",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            _queuePT.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
        else
        {
            var series = StaticNumeratorStorage.SimplifiedInvoiceSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            series.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    }

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
       response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "Hash",
            Data = hash,
            ftSignatureFormat = SignatureFormat.Text.WithPosition(SignatureFormatPosition.AfterHeader),
            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = $"{printHash} - Processado por programa certificado",
            Data = $"No {CertificationPosSystem.SoftwareCertificateNumber}/AT",
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.CertificationNo.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(new SignatureItem
        {
            Caption = "",
            Data = "ATCUD: " + series.ATCUD + "-" + series.Numerator,
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
        });
        response.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePTQRCode(qrCode));
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request)
    {
        if (!string.IsNullOrEmpty(request.ReceiptRequest.cbPreviousReceiptReference))
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var series = StaticNumeratorStorage.PaymentSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateRGQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = $"Origem: Fattura {receiptReference.ftReceiptIdentification}",
                Data = $"",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            series.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
        else
        {
            var series = StaticNumeratorStorage.PaymentSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateRGQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            series.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    }

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> Protocol0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    private async Task<ReceiptResponse> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var queueItems = _readOnlyQueueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
        await foreach (var existingQueueItem in queueItems)
        {
            if (string.IsNullOrEmpty(existingQueueItem.response))
            {
                continue;
            }

            var referencedResponse = JsonSerializer.Deserialize<ReceiptResponse>(existingQueueItem.response);
            receiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = referencedResponse
            };
            return referencedResponse;
        }
        throw new Exception("No referenced receipt found");
    }
}
