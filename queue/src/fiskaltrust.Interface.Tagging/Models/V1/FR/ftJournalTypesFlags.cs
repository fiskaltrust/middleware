using fiskaltrust.ifPOS.v1;
using fiskaltrust.Interface.Tagging.Generator;

namespace fiskaltrust.Interface.Tagging.Models.V1.FR
{
    [FlagExtensions(OnType = typeof(JournalRequest), OnField = nameof(JournalRequest.ftJournalType), Prefix = "V1", CaseName = "JournalTypeFlag")]
    public enum ftJournalTypesFlags : long
    {
        Zip0x0001 = 0x0000_0000_0001_0000,
    }
}