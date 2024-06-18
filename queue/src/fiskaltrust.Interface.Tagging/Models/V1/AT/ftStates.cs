using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.AT
{
    [FlagExtensions(OnType = typeof(ReceiptResponse), OnField = nameof(ReceiptResponse.ftState))]
    public enum ftStates : long
    {
        OutOfService0x0001 = 0x0001,
        SSCDTemporaryOutOfService0x0002 = 0x0002,
        SSCDPermanentlyOutOfService0x0004 = 0x0004,
        SubsequentEntryActivated0x0008 = 0x0008,
        MonthlyReportDue0x0010 = 0x0010,
        AnnualReportDue0x0020 = 0x0020,
        MessageNotificationPending0x0040 = 0x0040,
        BackupSSCDInUse0x0080 = 0x0080,
    }
}