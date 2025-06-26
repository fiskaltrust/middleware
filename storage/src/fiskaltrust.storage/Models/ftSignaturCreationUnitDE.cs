using System;

namespace fiskaltrust.storage.V0
{
    public class ftSignaturCreationUnitDE
    {
        public Guid ftSignaturCreationUnitDEId { get; set; }

        public string Url { get; set; }

        public long TimeStamp { get; set; }

        public string TseInfoJson { get; set; }

        public int Mode { get; set; }

        public string ModeConfigurationJson { get; set; }
    }
}