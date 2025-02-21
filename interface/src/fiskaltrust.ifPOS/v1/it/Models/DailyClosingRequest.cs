using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.it
{
    /// <summary>
    /// A class to specify a daily closing request that will be performed on the printer or server
    /// </summary>
    [DataContract]
    public class DailyClosingRequest
    {
        /// <summary>
        /// Operator
        /// </summary>
        [DataMember(Order = 10)]
        public string Operator { get; set; }

        /// <summary>
        /// Sends text messages to the customer display. You cannot insert carriage returns or line feeds so use 
        /// spaces to pad out line 1 and begin line 2. This sub-element has two attributes; one to indicate the
        /// operator and one for the text itself.The maximum number of characters is 40. This reduces to 20 if 
        /// used with printerTicket files.
        /// </summary>
        [DataMember(Order = 30)]
        public string DisplayText { get; set; }
    }
}
