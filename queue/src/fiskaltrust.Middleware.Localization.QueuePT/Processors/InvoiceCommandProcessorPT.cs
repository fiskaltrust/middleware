using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class InvoiceCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IInvoiceCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);
    private readonly RefundValidator _refundValidator = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => InvoiceB2C0x1001Async(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        var staticNumberStorage = await StaticNumeratorStorage.GetStaticNumeratorStorageAsync(queuePT, await _readOnlyQueueItemRepository);
        var isRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
        var series = isRefund ? staticNumberStorage.CreditNoteSeries : staticNumberStorage.InvoiceSeries;
        var receiptRequest = request.ReceiptRequest;
        var receiptResponse = request.ReceiptResponse;

        List<Receipt> receiptReferences = [];
        if (receiptRequest.cbPreviousReceiptReference is not null)
        {
            receiptReferences = request.ReceiptResponse.GetPreviousReceiptReference() ?? [];
            if (receiptReferences.Count == 0)
            {
                throw new InvalidOperationException(ErrorMessagesPT.PreviousReceiptReferenceNotFound);
            }

            if (isRefund)
            {
                if (receiptReferences.Count > 1)
                {
                    throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
                }

                var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
                var hasExistingRefund = await _receiptReferenceProvider.HasExistingRefundAsync(previousReceiptRef);
                if (hasExistingRefund)
                {
                    request.ReceiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_RefundAlreadyExists(previousReceiptRef));
                    return new ProcessCommandResponse(request.ReceiptResponse, []);
                }

                // Validate full refund: check if all articles from original invoice are properly refunded
                var originalRequest = receiptReferences[0].Request;
                var validationError = await _refundValidator.ValidateFullRefundAsync(
                    receiptRequest,
                    originalRequest,
                    previousReceiptRef);
                
                if (validationError != null)
                {
                    request.ReceiptResponse.SetReceiptResponseError(validationError);
                    return new ProcessCommandResponse(request.ReceiptResponse, []);
                }
            }
            else
            {
                // Check for partial refund: items have refund flag but receipt case does not
                if (receiptReferences.Count > 1)
                {
                    throw new NotSupportedException(ErrorMessagesPT.MultipleReceiptReferencesNotSupported);
                }

                var previousReceiptRef = receiptRequest.cbPreviousReceiptReference.SingleValue!;
                var originalRequest = receiptReferences[0].Request;

                // Validate partial refund: check for mixed items and quantity/amount limits
                var validationError = await _refundValidator.ValidatePartialRefundAsync(
                    receiptRequest,
                    originalRequest,
                    previousReceiptRef);
                
                if (validationError != null)
                {
                    request.ReceiptResponse.SetReceiptResponseError(validationError);
                    return new ProcessCommandResponse(request.ReceiptResponse, []);
                }
            }
        }
        else
        {
            // If no cbPreviousReceiptReference but items have refund flags, this is invalid
            if (!isRefund && receiptRequest.cbChargeItems?.Any(item => item.IsRefund()) == true)
            {
                request.ReceiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_MixedRefundItemsNotAllowed);
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }
        }

        series.Numerator++;
        ReceiptIdentificationHelper.AppendSeriesIdentification(receiptResponse, series);
        var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse,
        }, series.LastHash);
        var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();

        if (isRefund)
        {
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, receiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));

            if (receiptRequest.cbCustomer is null)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddConsumidorFinal());
            }
            series.LastHash = hash;
            series.LastCbReceiptMoment = receiptRequest.cbReceiptMoment;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            var qrCode = PortugalReceiptCalculations.CreateQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, receiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            if (receiptRequest.cbPreviousReceiptReference is not null)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddProformaReference(receiptReferences));
            }

            if (receiptRequest.cbCustomer is null)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddConsumidorFinal());
            }

            series.LastHash = hash;
            series.LastCbReceiptMoment = receiptRequest.cbReceiptMoment;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
    });

    private static void AddSignatures(NumberSeries series, ProcessResponse response, string hash, string printHash, string qrCode)
    {
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddHash(hash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddCertificateSignature(printHash));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddATCUD(series));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.CreatePTQRCode(qrCode));
        response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddIvaIncluido());
    }
}