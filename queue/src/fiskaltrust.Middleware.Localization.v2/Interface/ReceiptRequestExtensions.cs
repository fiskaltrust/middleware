using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public static class ReceiptRequestExtensions
{
    // public static long GetCasePart(this ReceiptRequest receiptRequest) => receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF;

    // public static bool IsVoid(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0004_0000) > 0;

    // public static bool IsRefund(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0100_0000) > 0;

    // public static bool IsInitialOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_FFFF) == 0x4001;

    // public static bool IsLateSigning(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0001_0000) == 0x0000_0000_0001_0000;

    // public static bool IsReceiptOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x0000;

    // public static bool IsInvoiceOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x1000;

    // public static bool IsInvoiceB2COperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F00F) == 0x1001;

    // public static bool IsDailyOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x2000;

    // public static bool IsSelfPricingOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0F00_0000_0000) == 0x0000_0100_0000_0000;

    // public static bool IsProtocolOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x3000;

    // public static bool IsLifeCycleOperation(this ReceiptRequest receiptRequest) => (receiptRequest.ftReceiptCase & 0x0000_0000_0000_F000) == 0x4000;

    // public static string GetCountry(this ReceiptRequest data)
    // {
    //     return (0xFFFF000000000000 & (ulong) data.ftReceiptCase) switch
    //     {
    //         0x4445000000000000 => "DE",
    //         0x4652000000000000 => "FR",
    //         0x4D45000000000000 => "ME",
    //         0x4954000000000000 => "IT",
    //         _ => "AT",
    //     };
    // }
}
