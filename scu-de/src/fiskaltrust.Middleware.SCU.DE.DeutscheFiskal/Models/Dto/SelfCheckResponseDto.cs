using System;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{

    public class SelfCheckResponseDto
    {
        public string remoteCspVersion { get; set; }
        public Keyinfo[] keyInfos { get; set; }
    }

    public class Keyinfo
    {
        public KeyState state { get; set; }
        public long lastSignatureCounter { get; set; }
    }
}
