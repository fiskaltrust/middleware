using System;

namespace fiskaltrust.Middleware.Storage.ES
{
    public class ftSignaturCreationUnitES
    {
        public Guid ftSignaturCreationUnitESId { get; set; }

        public long TimeStamp { get; set; }
        public string Url { get; set; }
        public string InfoJson { get; set; }
    }
}
