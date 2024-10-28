using System;

namespace fiskaltrust.Middleware.Storage.ES
{
    public class ftSignaturCreationUnitES
    {
        public Guid ftSignaturCreationUnitESId { get; set; }

        public string StateData { get; set; }

        public long TimeStamp { get; set; }
    }
}
