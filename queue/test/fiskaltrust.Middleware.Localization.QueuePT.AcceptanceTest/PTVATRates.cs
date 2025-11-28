using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.CertificationTool.Helpers;

public static class PTVATRates
{
    public const decimal Discounted1 = 6;
    public static ChargeItemCase Discounted1Case = (ChargeItemCase) 0x5054_2000_0000_0011;
    public const decimal Discounted2 = 13;
    public static ChargeItemCase Discounted2Case = (ChargeItemCase) 0x5054_2000_0000_0012;
    public const decimal Normal = 23;
    public static ChargeItemCase NormalCase = (ChargeItemCase) 0x5054_2000_0000_0013;
    public const decimal ParkingVatRate = 13;
    public static ChargeItemCase ParkingVatRateCase = (ChargeItemCase) 0x5054_2000_0000_0016;
    public const decimal NotTaxable = 0;
    public static ChargeItemCase NotTaxableCase = (ChargeItemCase) 0x5054_2000_0000_0018;
    public const decimal ZeroRate = 0;
    public static ChargeItemCase ZeroRateCase = (ChargeItemCase) 0x5054_2000_0000_0017;
}
