using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2.FR
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x0FFF, Prefix = "V2", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        JWT0x001 = 0x001,
        ShiftClosingSum0x010 = 0x010,
        DayClosingSum0x011 = 0x011,
        MonthClosingSum0x012 = 0x012,
        YearClosingSum0x013 = 0x013,
        ArchiveTotalsSum0x014 = 0x014,
        PerpetualTotalsSum0x015 = 0x015,        
    }
}