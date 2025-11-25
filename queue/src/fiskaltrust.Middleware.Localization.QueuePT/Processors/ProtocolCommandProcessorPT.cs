using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.pt;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Factories;
using fiskaltrust.Middleware.Localization.QueuePT.Helpers;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class ProtocolCommandProcessorPT(IPTSSCD sscd, ftQueuePT queuePT, AsyncLazy<IMiddlewareQueueItemRepository> readOnlyQueueItemRepository) : ProcessorPreparation, IProtocolCommandProcessor
{
    private readonly ReceiptReferenceProvider _receiptReferenceProvider = new(readOnlyQueueItemRepository);

    protected override AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; } = readOnlyQueueItemRepository;

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await ProcessLogMessageAsync(request);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await ProcessLogMessageAsync(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await ProcessLogMessageAsync(request);

    private async Task<ProcessCommandResponse> ProcessLogMessageAsync(ProcessCommandRequest request)
    {
        var validator = new ReceiptValidator(request.ReceiptRequest, request.ReceiptResponse, readOnlyQueueItemRepository);
        var validationResults = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund),
            GeneratesSignature = true,
            IsHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten),
            //NumberSeries = series  // Include series for moment order validation
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
        await Task.CompletedTask;
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => await PTFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        if (request.ReceiptRequest.cbPreviousReceiptReference is null)
        {
            var validationResult = ValidationResult.Failed(new ValidationError(
                   ErrorMessagesPT.EEEE_PreviousReceiptReference,
                   "EEEE_PreviousReceiptReference",
                   "cbPreviousReceiptReference"
               ));
            request.ReceiptResponse.SetReceiptResponseError($"Validation error [{validationResult.Errors[0].Code}]: {validationResult.Errors[0].Message} (Field: {validationResult.Errors[0].Field}, Index: {validationResult.Errors[0].ItemIndex})");
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        var receiptReferences = request.ReceiptResponse.GetRequiredPreviousReceiptReference();

        // Get the original receipt request to check its type
        var originalRequest = receiptReferences[0].Request;
        var originalResponse = receiptReferences[0].Response;
        var originalReceiptCase = originalRequest.ftReceiptCase.Case();

        // Validate that CopyReceipt is only supported for PosReceipt (0x0001) and Invoice types (0x100x)
        var isPosReceipt = originalReceiptCase == ReceiptCase.PointOfSaleReceipt0x0001;
        var isInvoice = originalReceiptCase == ReceiptCase.InvoiceUnknown0x1000 ||
                        originalReceiptCase == ReceiptCase.InvoiceB2C0x1001 ||
                        originalReceiptCase == ReceiptCase.InvoiceB2B0x1002 ||
                        originalReceiptCase == ReceiptCase.InvoiceB2G0x1003 ||
                        originalReceiptCase == ReceiptCase.UnknownReceipt0x0000 ||
                        originalReceiptCase == ReceiptCase.DeliveryNote0x0005 ||
                        originalReceiptCase == (ReceiptCase) 0x0006;

        if (!isPosReceipt && !isInvoice)
        {
            throw new NotSupportedException(ErrorMessagesPT.CopyReceiptNotSupportedForType(originalReceiptCase));
        }

        //// Compare the incoming ReceiptRequest with the referenced ReceiptRequest

        //var (areEqual, differences) = ReceiptRequestComparer.Compare(originalRequest, request.ReceiptRequest);

        //if (!areEqual)
        //{
        //    throw new InvalidOperationException(ErrorMessagesPT.CopyReceiptRequestMismatchDetails(differences));
        //}

        ////request.ReceiptResponse.ftSignatures.AddRange(originalResponse.ftSignatures);
        //request.ReceiptResponse.ftStateData = new
        //{
        //    Original = new
        //    {
        //        ReceiptRequest = receiptReferences[0].Item1,
        //        ReceiptResponse = receiptReferences[0].Item2
        //    }
        //};
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }
}