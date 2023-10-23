using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueME.Extensions
{
    public enum ErrorCode
    {
        Error = 3,
        CashDepositeOutstanding = 4,
        InvoiceAlreadyReceived = 5
    }
    public static class ReceiptResponseExtension
    {
        public static void SetStateToError(this ReceiptResponse receiptResponse, ErrorCode errorCode, string stateData)
        {
            receiptResponse.ftState = 0x4D45000000000000;
            receiptResponse.ftState += (int)errorCode;
            receiptResponse.ftStateData = stateData;
        }
    }
}
