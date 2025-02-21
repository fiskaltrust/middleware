using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class TseInfo
    {
        /// <summary>
        /// maximal number of clientId's supported
        /// </summary>
        [DataMember(Order = 10)]
        public long MaxNumberOfClients { get; set; }

        /// <summary>
        /// current number of clients registered
        /// </summary>
        [DataMember(Order = 20)]
        public long CurrentNumberOfClients { get; set; }

        /// <summary>
        /// current list of clientId's registered
        /// </summary>
        [DataMember(Order = 30)]
        public IEnumerable<string> CurrentClientIds { get; set; }

        /// <summary>
        /// maximal number of started transactions supported
        /// </summary>
        [DataMember(Order = 40)]
        public long MaxNumberOfStartedTransactions { get; set; }

        /// <summary>
        /// current number of started transactions
        /// </summary>
        [DataMember(Order = 50)]
        public long CurrentNumberOfStartedTransactions { get; set; }

        /// <summary>
        /// current list of started transactions
        /// </summary>
        [DataMember(Order = 60)]
        public IEnumerable<ulong> CurrentStartedTransactionNumbers { get; set; }

        /// <summary>
        /// maximum number of signatures which can be provided by the device over lifetime
        /// </summary>
        [DataMember(Order = 70)]
        public long MaxNumberOfSignatures { get; set; }

        /// <summary>
        /// current number of signatures created
        /// </summary>
        [DataMember(Order = 80)]
        public long CurrentNumberOfSignatures { get; set; }

        /// <summary>
        /// maximum size of log-memory in bytes
        /// </summary>
        [DataMember(Order = 90)]
        public long MaxLogMemorySize { get; set; }

        /// <summary>
        /// current size of used log memory in bytes
        /// </summary>
        [DataMember(Order = 100)]
        public long CurrentLogMemorySize { get; set; }

        /// <summary>
        /// current state in device lifecycle
        /// </summary>
        [DataMember(Order = 110)]
        public TseStates CurrentState { get; set; }

        /// <summary>
        /// identification of the device firmware
        /// </summary>
        [DataMember(Order = 120)]
        public string FirmwareIdentification { get; set; }

        /// <summary>
        /// certification identification of the device assigned by BSI
        /// </summary>
        [DataMember(Order = 130)]
        public string CertificationIdentification { get; set; }


        /// <summary>
        /// friendly name of the signature algorithm used by te device (e.g. 'ecdsa-plain-SHA256', 'ecdsa-plain-SHA384', 'ecdsa-plain-SHA512', aso.)
        /// </summary>
        [DataMember(Order = 140)]
        public string SignatureAlgorithm { get; set; }

        /// <summary>
        /// log time format used by the device. Values can be 'unixTime', 'utcTime', 'generalizedTime'.
        /// </summary>
        [DataMember(Order = 150)]
        public string LogTimeFormat { get; set; }

        /// <summary>
        /// serialnumber of the device related to the public key
        /// </summary>
        [DataMember(Order = 160)]
        public string SerialNumberOctet { get; set; }

        /// <summary>
        /// public key of the device related to the private key used for signing the transactions
        /// </summary>
        [DataMember(Order = 170)]
        public string PublicKeyBase64 { get; set; }

        /// <summary>
        /// chain of certificates starting with certificate related to the private/public keypair used for signing the transactions
        /// </summary>
        [DataMember(Order = 180)]
        public IEnumerable<string> CertificatesBase64 { get; set; }

        /// <summary>
        /// provided device specific additional informations
        /// </summary>
        [DataMember(Order = 1000)]
        public Dictionary<string, object> Info { get; set; }
    }
}
