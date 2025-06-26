using System;

namespace fiskaltrust.storage.V0
{
    public class ftSignaturCreationUnitAT
    {
        public Guid ftSignaturCreationUnitATId { get; set; }

        public string Url { get; set; }

        /// <summary>
        /// AT0 / AT1 / AT2
        /// </summary>
        public string ZDA { get; set; }

        /// <summary>
        /// HEX representation without 0x
        /// </summary>
        public string SN { get; set; }

        public string CertificateBase64 { get; set; }

        //public string[] IssuerBase64 { get; set; }
        //public string IssuerList

        /// <summary>
        /// Operation Mode = Flaged: 0x000000FF
        /// Timeout (s) = Flagged: 0x0000FF00 >>8
        /// </summary>
        /// <value>
        /// <list type="table">
        /// <item><term>0</term><description>Normal</description></item>
        /// <item><term>1</term><description>Backup, is only used if all normal fails</description></item>
        /// </list>
        /// </value>
        public int Mode { get; set; }

        public long TimeStamp { get; set; }
    }
}