using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Text;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ReceiptCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IReceiptCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
#pragma warning disable
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => WithPreparations(request, () => PointOfSaleReceipt0x0001Async(request));

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException("Refund receipt must reference a previous receipt.");
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException("Grouping of refund receipts is not supported.");
            }

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
                Caption = $"Referencia {receiptReferences[0].Item2.ftReceiptIdentification}",
                Data = $"Razão: Devolução",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            series.LastHash = hash;
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
    });

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

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        if (request.ReceiptRequest.cbPreviousReceiptReference is null)
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
        else
        {


            var receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException("Refund receipt must reference a previous receipt.");
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException("Grouping of refund receipts is not supported.");
            }
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
                Caption = $"Origem: Fattura {receiptReferences[0].Item2.ftReceiptIdentification}",
                Data = $"",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.ReferenceForCreditNote.As<SignatureType>(),
            });
            series.LastHash = hash;
            return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
        }
    });

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);
}
