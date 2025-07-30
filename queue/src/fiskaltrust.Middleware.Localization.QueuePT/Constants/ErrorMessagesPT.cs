namespace fiskaltrust.Middleware.Localization.QueuePT.Constants;

public static class ErrorMessagesPT
{
    public static string UnknownReceiptCase(long caseCode) => $"The given ftReceiptCase 0x{caseCode:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
    
    public static string NotSupportedReceiptCase(long caseCode, string name) => $"The ftReceiptCase {name} - 0x{caseCode:x} is not supported in the QueuePT implementation.";
    
    public const string PreviousReceiptReferenceNotFound = "The given cbPreviousReceiptReference didn't match with any of the items in the Queue.";
    
    public const string MultipleReceiptReferencesNotSupported = "Multiple receipt references are currently not supported.";
    
    public const string VoidNotSupported = "Void is not supported";
}
