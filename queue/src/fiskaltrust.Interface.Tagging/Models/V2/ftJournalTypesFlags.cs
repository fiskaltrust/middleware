using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V2
{
    [FlagExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType))]
    public enum ftJournalTypesFlags : long
    {
        Zip = 0x0000_0000_0001_0000,
    }
}