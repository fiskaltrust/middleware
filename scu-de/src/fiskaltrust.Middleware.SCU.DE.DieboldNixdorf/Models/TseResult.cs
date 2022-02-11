using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class TseResult
    {
        public DieboldNixdorfCommand Command { get; set; }

        public List<List<byte>> Parameters { get; set; } = new List<List<byte>>();

        public Guid RequestId { get; set; }
    }
}

