using Org.BouncyCastle.Asn1.Ocsp;

namespace fiskaltrust.Middleware.Localization.QueuePT.Interface;

public class ErrorMessages
{
    public static string UnknownReceiptCase(long caseCode) => $"The given ftReceiptCase 0x{caseCode:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
}