using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class EndExportSessionRequest
    {
        [DataMember(Order = 10)]
        public string TokenId { get; set; }

        /// <summary>
        /// SHA-256 (SHA-2) over all received chunks.
        /// Verifies data integrity. EndSession.
        /// </summary>
        [DataMember(Order = 20)]
        public string Sha256ChecksumBase64 { get; set; }

        /// <summary>
        /// Request data deletion after successfull verification.
        /// This works only for sessions without filter.
        /// The session needs to be started also with the erase flag.
        /// </summary>
        [DataMember(Order = 30)]
        public bool Erase { get; set; }
    }
}
