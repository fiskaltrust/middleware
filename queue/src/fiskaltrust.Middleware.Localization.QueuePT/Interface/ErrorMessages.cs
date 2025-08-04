using fiskaltrust.Middleware.Localization.QueuePT.Constants;

namespace fiskaltrust.Middleware.Localization.QueuePT.Interface;

public class ErrorMessages
{
    public static string UnknownReceiptCase(long caseCode) => ErrorMessagesPT.UnknownReceiptCase(caseCode);
}