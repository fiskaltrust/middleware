using System;

namespace fiskaltrust.storage.V0
{
    public class ftJournalAT
    {
        public Guid ftJournalATId { get; set; }
        public Guid ftSignaturCreationUnitId { get; set; }
        public long Number { get; set; }
        public string JWSHeaderBase64url { get; set; }
        public string JWSPayloadBase64url { get; set; }
        public string JWSSignatureBase64url { get; set; }
        public Guid ftQueueId { get; set; }
        public long TimeStamp { get; set; }
    }
}