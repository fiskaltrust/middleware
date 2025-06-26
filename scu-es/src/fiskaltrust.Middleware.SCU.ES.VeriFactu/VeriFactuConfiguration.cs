using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu
{
    public class VeriFactuSCUConfiguration
    {
        public string BaseUrl { get; set; } = "https://prewww10.aeat.es";
        public string QRCodeBaseUrl { get; set; } = "https://prewww2.aeat.es";

        public X509Certificate2 Certificate { get; set; } = null!;

        public static VeriFactuSCUConfiguration FromConfiguration(Dictionary<string, object> configuration)
        {
            return new VeriFactuSCUConfiguration
            {
                BaseUrl = configuration["BaseUrl"].ToString()!,
                QRCodeBaseUrl = configuration["QRCodeBaseUrl"].ToString()!,
                Certificate = (X509Certificate2)configuration["Certificate"]!
            };
        }
    }
}