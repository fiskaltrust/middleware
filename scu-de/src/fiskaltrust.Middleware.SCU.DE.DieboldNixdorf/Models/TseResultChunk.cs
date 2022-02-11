using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class TseResultChunk
    {
        public int Position { get; set; }

        public byte Marker { get; set; }

        public List<byte> Values { get; set; } = new List<byte>();
    }
}

