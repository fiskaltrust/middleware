using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2;

public interface ISignProcessor
{
    Task<ReceiptResponse?> ProcessAsync(ReceiptRequest request);
}