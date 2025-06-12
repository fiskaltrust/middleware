using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;

public static class ReceiptRequestExtensions
{
    public static bool HasOnlyServiceItems(this ReceiptRequest receiptRequest) => receiptRequest.cbChargeItems.All(x => x.ftChargeItemCase.IsTypeOfService(ChargeItemCaseTypeOfService.OtherService));

    public static bool HasEUCountryCode(this ReceiptRequest receiptRequest)
    {
        return EU_CountryCodes.Contains((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000);
    }

    public static List<ulong> EU_CountryCodes = new List<ulong> { 0x4555_0000_0000_0000, 0x4752_0000_0000_0000, 0x4154_0000_0000_0000 };
}
