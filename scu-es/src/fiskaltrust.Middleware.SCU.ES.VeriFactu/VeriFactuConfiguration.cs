using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu
{
    public class VeriFactuSCUConfiguration
    {
        public string BaseUrl { get; set; } = "https://prewww10.aeat.es/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP";
        public string QRCodeBaseUrl { get; set; } = "https://prewww2.aeat.es";
        public X509Certificate2 Certificate { get; set; } = null!;
        public string Nif { get; set; } = null!;
        public string NombreRazonEmisor { get; set; } = null!;

        public static VeriFactuSCUConfiguration FromConfiguration(Dictionary<string, object> configuration)
        {
            var config = new VeriFactuSCUConfiguration();

            if (configuration.ContainsKey("BaseUrl") && configuration["BaseUrl"] != null)
            {
                config.BaseUrl = configuration["BaseUrl"].ToString()!;
            }

            if (configuration.ContainsKey("QRCodeBaseUrl") && configuration["QRCodeBaseUrl"] != null)
            {
                config.QRCodeBaseUrl = configuration["QRCodeBaseUrl"].ToString()!;
            }

            if (configuration.ContainsKey("CertificateBase64") && configuration["CertificateBase64"] != null &&
                configuration.ContainsKey("CertificatePassword") && configuration["CertificatePassword"] != null)
            {
                config.Certificate = new X509Certificate2(
                    Convert.FromBase64String(configuration["CertificateBase64"].ToString()!),
                    configuration["CertificatePassword"].ToString()!);
            }

            if (configuration.ContainsKey("Nif") && configuration["Nif"] != null)
            {
                config.Nif = configuration["Nif"].ToString()!;
            }

            if (configuration.ContainsKey("NombreRazonEmisor") && configuration["NombreRazonEmisor"] != null)
            {
                config.NombreRazonEmisor = configuration["NombreRazonEmisor"].ToString()!;
            }

            return config;
        }
    }
}