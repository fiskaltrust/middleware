using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueES.Logic;

public abstract class ProcessorPreparation
{
    protected abstract AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; }

    private IMarketValidator? _shadowFvValidator;
    private ILogger? _shadowLogger;

    public void SetShadowValidation(IMarketValidator fvValidator, ILogger shadowLogger)
    {
        _shadowFvValidator = fvValidator;
        _shadowLogger = shadowLogger;
    }

    public async Task<ProcessCommandResponse> WithPreparations(ProcessCommandRequest request, Func<Task<ProcessCommandResponse>> process)
    {
        var validator = new ReceiptValidator(request.ReceiptRequest, request.ReceiptResponse, _readOnlyQueueItemRepository);
        var validationResults = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund),
            GeneratesSignature = true,
            IsHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten),
        });

        if (_shadowFvValidator != null && _shadowLogger != null)
        {
            var fvResult = _shadowFvValidator.LastResult;
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
            var error = validationResults.AllErrors.FirstOrDefault();
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
