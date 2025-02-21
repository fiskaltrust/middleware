using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// Request from the cash register to extract a journal from the local database. 
    /// Type and Timerange can be specified with the properties Journaltype,from and to. 
    /// </summary>
    [DataContract]
    public class JournalRequest
    {
        [DataMember(Order = 1)]
        public long ftJournalType { get; set; }

        [DataMember(Order = 2)]
        public long From { get; set; }

        [DataMember(Order = 3)]
        public long To { get; set; }

        [DataMember(Order = 4)]
        public int MaxChunkSize { get; set; } = 4096;
    }
}
