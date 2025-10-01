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
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Constants;
using fiskaltrust.Middleware.Localization.QueuePT.Models;

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
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            if (!request.ReceiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data) || data.PT is null || data.PT.Series is null || !data.PT.Number.HasValue)
            {
                request.ReceiptResponse.SetReceiptResponseError("When using Handwritten flag, ftReceiptCaseData with Series and Number must not be set.");
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }
        }


        ReceiptResponse receiptResponse = request.ReceiptResponse;
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException(ErrorMessagesPT.PreviousReceiptReferenceNotFound);
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
            }
            request.ReceiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = receiptReferences[0].Item2,
            };
        }

        NumberSeries series;

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
            {
                request.ReceiptResponse.SetReceiptResponseError("Handwritten refunds are not supported.");
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }
            else
            {
                series = StaticNumeratorStorage.CreditNoteSeries;
            }
        }
        else
        {
            if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
            {
                series = StaticNumeratorStorage.HandWrittenFSSeries;
            }
            else
            {
                series = StaticNumeratorStorage.SimplifiedInvoiceSeries;
            }
        }
        series.Numerator++;
        receiptResponse.ftReceiptIdentification += series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = receiptResponse,
        }, series.LastHash);
        var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));
        }
        else
        {
            var qrCode = PortugalReceiptCalculations.CreateSimplifiedInvoiceQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddProformaReference(receiptReferences));
            }
        }

        // Add manual document identification signature for handwritten receipts
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten))
        {
            if (request.ReceiptRequest.TryDeserializeftReceiptCaseData<ftReceiptCaseDataPayload>(out var data) && data.PT is not null && data.PT.Series is not null && data.PT.Number.HasValue)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddManualDocumentIdentification(data.PT.Series, data.PT.Number.Value));
            }
        }

        if (request.ReceiptRequest.cbCustomer is null)
        {
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "",
                Data = $"Consumidor final",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
            });
        }

        series.LastHash = hash;
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });

    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        ReceiptResponse receiptResponse = request.ReceiptResponse;
        List<(ReceiptRequest, ReceiptResponse)> receiptReferences = [];
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = await _receiptReferenceProvider.GetReceiptReferencesIfNecessaryAsync(request);
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException(ErrorMessagesPT.PreviousReceiptReferenceNotFound);
            }
            if (receiptReferences.Count > 1)
            {
                throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
            }
            request.ReceiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = receiptReferences[0].Item2,
            };
        }

        var series = StaticNumeratorStorage.PaymentSeries;
        series.Numerator++;
        receiptResponse.ftReceiptIdentification += series.Identifier + "/" + series.Numerator!.ToString()!.PadLeft(4, '0');
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = receiptResponse,
        }, series.LastHash);

        var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
        var qrCode = PortugalReceiptCalculations.CreateRGQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
        AddSignatures(series, response, hash, printHash, qrCode);
        series.LastHash = hash;
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            response.ReceiptResponse.AddSignatureItem(new SignatureItem
            {
                Caption = "",
                Data = $"Origem: {receiptReferences[0].Item2.ftReceiptIdentification.Split("#").Last()}",
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = SignatureTypePT.PTAdditional.As<SignatureType>(),
            });
        }

        return new ProcessCommandResponse(response.ReceiptResponse, []);
    });

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(qrCode));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddIvaIncluido());
    }
}
