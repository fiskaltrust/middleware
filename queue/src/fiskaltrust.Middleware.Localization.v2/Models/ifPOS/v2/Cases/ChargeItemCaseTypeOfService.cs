using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

public enum ChargeItemCaseTypeOfService
{
    UnknownService = 0x00,
    Delivery = 0x10,
    OtherService = 0x20,
    Tip = 0x30,
    Voucher = 0x40,
    CatalogService = 0x50,
    NotOwnSales = 0x60,
    OwnConsumption = 0x70,
    Grant = 0x80,
    Receivable = 0x90,
    CashTransfer = 0xA0,
}

public static class ChargeItemCaseTypeOfServiceExt
{
    public static ChargeItemCase WithTypeOfService(this ChargeItemCase self, ChargeItemCaseTypeOfService typeOfService) => (ChargeItemCase) (((ulong) self & 0xFFFF_FFFF_FFFF_FF0F) | (ulong) typeOfService);
    public static bool IsTypeOfService(this ChargeItemCase self, ChargeItemCaseTypeOfService typeOfService) => ((long) self & 0xF0) == (long) typeOfService;
    public static ChargeItemCaseTypeOfService TypeOfService(this ChargeItemCase self) => (ChargeItemCaseTypeOfService) ((long) self & 0xF0);
}