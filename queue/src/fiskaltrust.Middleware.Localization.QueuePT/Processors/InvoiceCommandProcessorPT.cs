using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using System.Text;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class InvoiceCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, ftSignaturCreationUnitPT signaturCreationUnitPT, IMiddlewareQueueItemRepository readOnlyQueueItemRepository) : ProcessorPreparation, IInvoiceCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
#pragma warning disable
    private readonly ftSignaturCreationUnitPT _signaturCreationUnitPT = signaturCreationUnitPT;
    protected override IMiddlewareQueueItemRepository _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => WithPreparations(request, async () => new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var series = StaticNumeratorStorage.InvoiceSeries;
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
                Data = $"Rasão: Devolução",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            _queuePT.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
        else if (request.ReceiptRequest.cbPreviousReceiptReference != null)
        {
            var receiptReference = await LoadReceiptReferencesToResponse(request.ReceiptRequest, request.ReceiptResponse);
            var series = StaticNumeratorStorage.InvoiceSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = $"Referencia: Proforma {receiptReference.ftReceiptIdentification}",
                Data = $"",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            _queuePT.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
        else
        {
            var series = StaticNumeratorStorage.InvoiceSeries;
            series.Numerator++;
            var invoiceNo = series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
            ;
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, invoiceNo, series.LastHash);
            response.ReceiptResponse.ftReceiptIdentification = invoiceNo;
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(printHash, _queuePT.IssuerTIN, _queuePT.TaxRegion, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            series.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    });

    private async Task<ReceiptResponse> LoadReceiptReferencesToResponse(ReceiptRequest request, ReceiptResponse receiptResponse)
    {
        var data = request.GetPreviousReceiptReferenceStringOrArray();
        if (data.cbPreviousReceiptReferenceArray != null)
        {
            throw new Exception($"cbPreviousReceiptReferenceArray is not supported in PT. Please use cbPreviousReceiptReference as a string.");
        }

        var queueItems = _readOnlyQueueItemRepository.GetByReceiptReferenceAsync(data.cbPreviousReceiptReferenceString, request.cbTerminalID);
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
}