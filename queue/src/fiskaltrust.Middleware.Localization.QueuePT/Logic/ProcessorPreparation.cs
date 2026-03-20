using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic;

public abstract class ProcessorPreparation
{
    public static class VATHelpers
    {
        public static decimal CalculateVAT(decimal amount, decimal rate) => decimal.Round(amount / (100M + rate) * rate, 6, MidpointRounding.ToEven);
    }

    protected abstract AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; }

    private MarketValidator? _shadowFvValidator;
    private ILogger? _shadowLogger;

    public void SetShadowValidation(MarketValidator fvValidator, ILogger shadowLogger)
    {
        _shadowFvValidator = fvValidator;
        _shadowLogger = shadowLogger;
    }

    public async Task<ProcessCommandResponse> WithPreparations(ProcessCommandRequest request, Func<Task<ProcessCommandResponse>> process)
    {
        foreach (var chargeItem in request?.ReceiptRequest.cbChargeItems ?? Enumerable.Empty<ChargeItem>())
        {
            if (!chargeItem.VATAmount.HasValue)
            {
                chargeItem.VATAmount = VATHelpers.CalculateVAT(chargeItem.Amount, chargeItem.VATRate);
            }
        }

        var validator = new ReceiptValidator(request.ReceiptRequest, request.ReceiptResponse, _readOnlyQueueItemRepository);
        var validationResults = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund),
            GeneratesSignature = true,
            IsHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten),
        });

        if (_shadowFvValidator != null && _shadowLogger != null)
        {
            var fvResult = await _shadowFvValidator.ValidateAsync(request.ReceiptRequest, request.queue);
            var oldSucceeded = validationResults.IsValid;
            var fvSucceeded = fvResult.IsValid;

            if (oldSucceeded != fvSucceeded)
            {
                _shadowLogger.LogError(
                    "Receipt validation mismatch " +
                    "cbReceiptReference={Ref} ftReceiptCase=0x{Case:X} " +
                    "OldSuccess={OldSuccess} FVSuccess={FVSuccess} " +
                    "FVErrors={FVErrors}",
                    request.ReceiptRequest.cbReceiptReference,
                    request.ReceiptRequest.ftReceiptCase,
                    oldSucceeded,
                    fvSucceeded,
                    string.Join("; ", fvResult.Errors.Select(e => $"[{e.ErrorCode}] {e.ErrorMessage}")));
            }
        }

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
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }

        return await process();
    }
}
