using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public class EpsonResult
    {
        public string Result { get; set; }

        public string Function { get; set; }

        public Dictionary<string, string> Error { get; }
    }
}
