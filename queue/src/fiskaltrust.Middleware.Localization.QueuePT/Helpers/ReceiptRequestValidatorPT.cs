using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Constants;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public class ReceiptRequestValidatorPT
{
    public static void ValidateReceiptOrThrow(ReceiptRequest receiptRequest)
    {
        if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void))
        {
            throw new Exception(ErrorMessagesPT.VoidNotSupported);
        }
    }
}
