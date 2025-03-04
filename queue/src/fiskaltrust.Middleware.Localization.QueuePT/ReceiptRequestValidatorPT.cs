using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueuePT;

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
