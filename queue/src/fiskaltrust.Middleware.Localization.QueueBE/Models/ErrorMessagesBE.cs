namespace fiskaltrust.Middleware.Localization.QueueBE.Models;

public class ErrorMessagesBE
{
    public static string UnknownReceiptCase(long caseCode) => $"The given ftReceiptCase 0x{caseCode:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
}