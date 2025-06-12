using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface ISignProcessor
{
    Task<ReceiptResponse?> ProcessAsync(ReceiptRequest request);
}
