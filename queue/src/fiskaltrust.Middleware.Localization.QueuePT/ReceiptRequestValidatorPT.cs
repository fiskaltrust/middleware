using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class ReceiptRequestValidatorPT
{
    public static void ValidateReceiptOrThrow(ReceiptRequest receiptRequest)
    {
        if (ReceiptCaseFlagsExt.IsFlag(receiptRequest.ftReceiptCase, ReceiptCaseFlags.Void))
        {
            throw new Exception("Void is not supported");
        }
    }
}
