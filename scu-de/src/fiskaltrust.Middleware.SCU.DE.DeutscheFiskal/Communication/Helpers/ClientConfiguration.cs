using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication
{
    public class ClientConfiguration
    {
        public Uri BaseAddress { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string GrantType { get; set; }
        public Dictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();
        public int Timeout { get; set; }
    }
}