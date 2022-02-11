using System;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models
{

    public class SelfCheckResponseDto
    {
        public string description { get; set; }
        public string localClientVersion { get; set; }
        public Configparameter[] configParameters { get; set; }
        public int lastTransactionCounter { get; set; }
        public string remoteCspVersion { get; set; }
        public DateTime cspSystemTime { get; set; }
        public string keyReference { get; set; }
        public Keyinfo[] keyInfos { get; set; }
        public Failure[] failures { get; set; }
    }

    public class Configparameter
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class Keyinfo
    {
        public string serialNumber { get; set; }
        public KeyState state { get; set; }
        public Certificate[] certificates { get; set; }
        public string signatureAlgorithm { get; set; }
        public long lastSignatureCounter { get; set; }
    }

    public class Certificate
    {
        public string pemCertificate { get; set; }
        public string publicKey { get; set; }
        public string type { get; set; }
        public DateTime? endDate { get; set; }
        public DateTime? revocationDate { get; set; }
    }

    public class Failure
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
