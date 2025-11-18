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
        // Validate supported VAT rates
        var vatRateError = PortugalReceiptValidation.ValidateSupportedVatRates(request.ReceiptRequest);
        if (vatRateError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(vatRateError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate VAT rate category matches percentage and VAT amount calculation
        var vatRateAndAmountError = PortugalReceiptValidation.ValidateVatRateAndAmount(request.ReceiptRequest);
        if (vatRateAndAmountError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(vatRateAndAmountError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate that non-refund receipts don't have negative amounts/quantities (except discounts)
        var negativeValuesError = PortugalReceiptValidation.ValidateNegativeAmountsAndQuantities(
            request.ReceiptRequest, 
            request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund));
        if (negativeValuesError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(negativeValuesError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate that charge items sum matches pay items sum (receipt balance)
        var balanceError = PortugalReceiptValidation.ValidateReceiptBalance(request.ReceiptRequest);
        if (balanceError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(balanceError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate cbUser presence for signature-generating receipts
        var userPresenceError = PortugalReceiptValidation.ValidateUserPresenceForSignatures(request.ReceiptRequest, generatesSignature: true);
        if (userPresenceError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(userPresenceError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate cbUser structure (must follow PTUserObject format)
        var userStructureError = PortugalReceiptValidation.ValidateUserStructure(request.ReceiptRequest);
        if (userStructureError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(userStructureError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var cashPaymentError = PortugalReceiptValidation.ValidateCashPaymentLimit(request.ReceiptRequest);
        if (cashPaymentError != null)
        {
            request.ReceiptResponse.SetReceiptResponseError(cashPaymentError);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        // Validate that refunds have a cbPreviousReceiptReference
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) &&
            request.ReceiptRequest.cbPreviousReceiptReference is null)
        {
            request.ReceiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_RefundMissingPreviousReceiptReference);
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var staticNumberStorage = await StaticNumeratorStorage.GetStaticNumeratorStorageAsync(queuePT, await _readOnlyQueueItemRepository);
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
            if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
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

        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund))
        {
            var series = staticNumberStorage.CreditNoteSeries;
            if (!ValidateReceiptMomentOrder(request, series))
            {
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }

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
            var series = staticNumberStorage.InvoiceSeries;
            if (!ValidateReceiptMomentOrder(request, series))
            {
                return new ProcessCommandResponse(request.ReceiptResponse, []);
            }

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

    private static bool ValidateReceiptMomentOrder(ProcessCommandRequest request, NumberSeries series)
    {
        if (series.LastCbReceiptMoment.HasValue &&
            !request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) &&
            request.ReceiptRequest.cbReceiptMoment < series.LastCbReceiptMoment.Value)
        {
            request.ReceiptResponse.SetReceiptResponseError(ErrorMessagesPT.EEEE_CbReceiptMomentBeforeLastMoment(series.Identifier, series.LastCbReceiptMoment.Value));
            return false;
        }

        return true;
    }
}