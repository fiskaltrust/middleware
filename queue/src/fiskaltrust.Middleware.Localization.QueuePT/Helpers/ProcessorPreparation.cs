using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public abstract class ProcessorPreparation
{
    public static class VATHelpers
    {
        public static decimal CalculateVAT(decimal amount, decimal rate) => decimal.Round(amount / (100M + rate) * rate, 6, MidpointRounding.ToEven);
    }

    protected abstract AsyncLazy<IMiddlewareQueueItemRepository> _readOnlyQueueItemRepository { get; init; }

    public async Task<T> WithPreparations<T>(ProcessCommandRequest request, Func<Task<T>> process)
    {
        foreach (var chargeItem in request?.ReceiptRequest.cbChargeItems ?? Enumerable.Empty<ChargeItem>())
        {
            if (!chargeItem.VATAmount.HasValue)
            {
                chargeItem.VATAmount = VATHelpers.CalculateVAT(chargeItem.Amount, chargeItem.VATRate);
            }
        }

        await StaticNumeratorStorage.LoadStorageNumbers(await _readOnlyQueueItemRepository);
        ReceiptRequestValidatorPT.ValidateReceiptOrThrow(request.ReceiptRequest);
        return await process();
    }
}