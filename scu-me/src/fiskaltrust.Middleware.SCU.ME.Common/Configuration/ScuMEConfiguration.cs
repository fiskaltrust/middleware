using System;
using System.Security.Cryptography.X509Certificates;

[assembly:CLSCompliant(true)]
namespace fiskaltrust.Middleware.SCU.ME.Common.Configuration
{
    public class ScuMEConfiguration
    {
        /// <summary>
        /// Tax identification number (TIN) of the PosOperator.
        /// </summary>
        public string TIN { get; set; } = null!;

        /// <summary>
        /// VAT number of the PosOperator.
        /// </summary>
        public string VatNumber { get; set; } = null!;

        /// <summary>
        /// Name of the PosOperator.
        /// </summary>
        public string PosOperatorName { get; set; } = null!;

        /// <summary>
        /// Address of the PosOperator.
        /// </summary>
        public string PosOperatorAddress { get; set; } = null!;

        /// <summary>
        /// Town of the PosOperator.
        /// </summary>
        public string PosOperatorTown { get; set; } = null!;

        /// <summary>
        /// Country of the PosOperator.
        /// </summary>
        public string PosOperatorCountry { get; set; } = null!;

        /// <summary>
        /// Certificate used for signing.
        /// </summary>
        public X509Certificate2 Certificate { get; set; } = null!;
    }
}