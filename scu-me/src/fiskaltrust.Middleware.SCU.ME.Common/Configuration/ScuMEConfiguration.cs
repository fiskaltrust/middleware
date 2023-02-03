using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.SCU.ME.Common.Helpers;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.ME.Common.Configuration
{
    public class ScuMEConfiguration
    {
        /// <summary>
        /// DatetimeFormat sent with receipt LOCAL, UTC
        /// </summary>
        public string DatetimeFormat { get; set; } = "LOCAL";

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
        [JsonConverter(typeof(X509Certificate2Converter))]
        public X509Certificate2 Certificate { get; set; } = null!;

        /// <summary>
        /// Use test environment.
        /// </summary>
        public bool Sandbox { get; set; } = false;

        /// <summary>
        /// Proxy to use for external endpoints.
        /// </summary>
        [JsonConverter(typeof(WebProxyConverter))]
        public WebProxy Proxy { get; set; } = null!;
    }
}