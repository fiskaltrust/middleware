using System;

namespace fiskaltrust.Middleware.Storage.ES;

public class ftJournalES
{
    public Guid ftJournalESId { get; set; }

    public Guid ftSignaturCreationUnitId { get; set; }

    public Guid ftQueueId { get; set; }

    public string JournalType { get; set; }

    public byte[] JournalData { get; set; }

    public long TimeStamp { get; set; }
}