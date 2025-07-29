using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public class ReceiptRequestValidatorPT
{
    public static void ValidateReceiptOrThrow(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            throw new Exception("Void is not supported");
        }
    }
}
