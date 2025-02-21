using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.me
{
    [DataContract]
    public class RegisterTcrResponse
    {
        /// <summary>
        /// Unique code of the cash register, assigned by the central invoice register (CIS) service. Must be used in all subsequent requests to the CIS service.
        /// </summary>
        /// <remarks>
        /// Has the following format: [a-z]{2}[0-9]{3}[a-Z]{2}[0-9]{3} (e.g. ab123ab123)
        /// </remarks>
        [DataMember(Order = 10)]
        public string TcrCode { get; set; }
    }
}
