namespace fiskaltrust.Middleware.Localization.QueueIT.Models;

public static class ErrorMessagesIT
{
    public static string NoSignaturCreationUnitAssigned(Guid queueId) => $"The queue {queueId} has no ftSignaturCreationUnitIT assigned. Please check the cashbox configuration.";

    public static string SignaturCreationUnitNotFound(Guid scuId) => $"The ftSignaturCreationUnitIT {scuId} assigned to the queue was not found in the configuration.";

    public static string SignaturCreationUnitWithoutUrl(Guid scuId) => $"The ftSignaturCreationUnitIT {scuId} has no Url configured, the RT device cannot be reached.";

    public static string ReferencedReceiptNotFound(string cbPreviousReceiptReference) => $"There is no item available with the given cbPreviousReceiptReference '{cbPreviousReceiptReference}'.";

    public const string PreviousReceiptReferenceRequired = "A copy of an existing receipt requires the cbPreviousReceiptReference of the receipt that should be reprinted.";

    public const string MissingRTDocumentIdentification = "The response of the SCU does not contain the RT document number (0x12) and the RT Z number (0x11) signatures.";

    public const string MissingRTZNumber = "The response of the SCU does not contain the RT Z number (0x11) signature.";
}
