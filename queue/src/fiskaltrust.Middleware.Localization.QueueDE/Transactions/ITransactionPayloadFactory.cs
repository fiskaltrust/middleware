using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.Transactions
{
    public interface ITransactionPayloadFactory
    {
        (string processType, string payload) CreateReceiptPayload(ReceiptRequest receiptRequest);
        (string processType, string payload) CreateAutomaticallyCanceledReceiptPayload();
    }
}