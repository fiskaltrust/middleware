using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public abstract class ProcessorPreparation
{
    public static class VATHelpers
    {
        public static decimal CalculateVAT(decimal amount, decimal rate) => decimal.Round(amount / (100M + rate) * rate, 6, MidpointRounding.ToEven);
    }

    protected abstract AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; }
    protected virtual bool IsTrainingModeEnabled => false;

    public async Task<ProcessCommandResponse> WithPreparations(ProcessCommandRequest request, Func<Task<ProcessCommandResponse>> process)
    {
        foreach (var chargeItem in request?.ReceiptRequest.cbChargeItems ?? Enumerable.Empty<ChargeItem>())
        {
            if (!chargeItem.VATAmount.HasValue)
            {
                chargeItem.VATAmount = VATHelpers.CalculateVAT(chargeItem.Amount, chargeItem.VATRate);
            }
        }
        //var series = isRefund ? staticNumberStorage.CreditNoteSeries : staticNumberStorage.InvoiceSeries;

        // Perform all validations using the new validator (returns one ValidationResult per error)
        // Now includes receipt moment order validation with the series
        var validator = new ReceiptValidator(request.ReceiptRequest, request.ReceiptResponse, _readOnlyQueueItemRepository, IsTrainingModeEnabled);
        var validationResults = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund),
            GeneratesSignature = true,
            IsHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten),
            //NumberSeries = series  // Include series for moment order validation
        });
        if (!validationResults.IsValid)
        {

            var error = validationResults.Results.SelectMany(r => r.Errors).FirstOrDefault();
            if (error != null)
            {
                request.ReceiptResponse.SetReceiptResponseError($"Validation error [{error.Code}]: {error.Message} (Field: {error.Field}, Index: {error.ItemIndex})");
            }
            else
            {
                request.ReceiptResponse.SetReceiptResponseError($"Validation error [UNKNOWN]: An unknown validation error has occurred.");
            }
            //foreach (var result in validationResults.Results)
            //{
            //    foreach (var error in result.Errors)
            //    {
            //        request.ReceiptResponse.SetReceiptResponseError($"Validation error [{error.Code}]: {error.Message} (Field: {error.Field}, Index: {error.ItemIndex})");
            //    }
            //}
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }



        return await process();
    }
}
