using System.Linq;
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
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class InvoiceCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IInvoiceCommandProcessor
{
    private readonly IPTSSCD _sscd = sscd;
    private readonly ftQueuePT _queuePT = queuePT;
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => WithPreparations(request, async () =>
    {
        var staticNumberStorage = await StaticNumeratorStorage.GetStaticNumeratorStorageAsync(queuePT, await _readOnlyQueueItemRepository);
        
        // Determine the series to use (needed for validation)
        var isRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund);
        var series = isRefund ? staticNumberStorage.CreditNoteSeries : staticNumberStorage.InvoiceSeries;

        // Perform all validations using the new validator (returns one ValidationResult per error)
        // Now includes receipt moment order validation with the series
        var validator = new ReceiptValidator(request.ReceiptRequest);
        var validationResults = validator.ValidateAndCollect(new ReceiptValidationContext
        {
            IsRefund = isRefund,
            GeneratesSignature = true,
            IsHandwritten = false,
            NumberSeries = series  // Include series for moment order validation
        });

        if (!validationResults.IsValid)
        {
            foreach (var result in validationResults.Results)
            {
                foreach (var error in result.Errors)
                {
                    request.ReceiptResponse.SetReceiptResponseError($"Validation error [{error.Code}]: {error.Message} (Field: {error.Field}, Index: {error.ItemIndex})");
                }
            }
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var receiptResponse = request.ReceiptResponse;
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
            
            // Check for duplicate refund if this is a refund receipt
            if (isRefund)
            {
                var previousReceiptRef = request.ReceiptRequest.cbPreviousReceiptReference.ToString();
                var hasExistingRefund = await _receiptReferenceProvider.HasExistingRefundAsync(previousReceiptRef, request.ReceiptRequest.cbTerminalID);
                if (hasExistingRefund)
                {
                    request.ReceiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_RefundAlreadyExists(previousReceiptRef));
                    return new ProcessCommandResponse(request.ReceiptResponse, []);
                }
            }
            
            receiptResponse.ftStateData = new
            {
                ReferencedReceiptResponse = receiptReferences[0].Item2,
            };
        }

        if (isRefund)
        {
            series.Numerator++;
            ReceiptIdentificationHelper.AppendSeriesIdentification(receiptResponse, series);
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = receiptResponse,
            }, series.LastHash);
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateCreditNoteQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddReferenceSignature(receiptReferences));

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
            series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }
        else
        {
            series.Numerator++;
            ReceiptIdentificationHelper.AppendSeriesIdentification(receiptResponse, series);
            var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            }, series.LastHash);
            var printHash = new StringBuilder().Append(hash[0]).Append(hash[10]).Append(hash[20]).Append(hash[30]).ToString();
            var qrCode = PortugalReceiptCalculations.CreateInvoiceQRCode(printHash, _queuePT.IssuerTIN, series.ATCUD + "-" + series.Numerator, request.ReceiptRequest, response.ReceiptResponse);
            AddSignatures(series, response, hash, printHash, qrCode);
            if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
            {
                response.ReceiptResponse.AddSignatureItem(SignatureItemFactoryPT.AddProformaReference(receiptReferences));
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
            series.LastCbReceiptMoment = request.ReceiptRequest.cbReceiptMoment;
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