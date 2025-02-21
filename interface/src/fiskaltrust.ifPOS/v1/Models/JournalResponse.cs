using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1
{
    /// <summary>
    /// The fiskaltrust.Middleware returns the requested JournalType as a FileStream.
    /// </summary>
    [DataContract]
    public class JournalResponse
    {
        [DataMember(Order = 1)]
        public List<byte> Chunk { get; set; }
    }
}
