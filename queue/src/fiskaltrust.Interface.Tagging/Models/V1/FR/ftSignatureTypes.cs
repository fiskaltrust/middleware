using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [CaseExtensions(OnType = typeof(SignaturItem), OnField = nameof(SignaturItem.ftSignatureType), Mask = 0x0FFF, Prefix = "V1", CaseName = "SignatureType")]
    public enum ftSignatureTypes : long
    {
        Unknown0x000 = 0x000,
        JWT0x001 = 0x001,
        ShiftClosingSum0x002 = 0x002,
        DayClosingSum0x003 = 0x003,
        MonthClosingSum0x004 = 0x004,
        YearClosingSum0x005 = 0x005,
        ArchiveTotalsSum0x006 = 0x006,
        PerpetualTotalsSum0x007 = 0x007,
    }
}