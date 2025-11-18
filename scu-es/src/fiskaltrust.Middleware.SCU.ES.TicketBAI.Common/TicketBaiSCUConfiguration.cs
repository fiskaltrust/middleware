using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Common
{
    public class TicketBaiSCUConfiguration
    {
        public X509Certificate2 Certificate { get; set; } = null!;
        public string EmisorNif { get; set; } = null!;
        public string EmisorApellidosNombreRazonSocial { get; set; } = null!;

        public string SoftwareVersion { get; set; } = null!;
        public string SoftwareName { get; set; } = null!;
        public string SoftwareLicenciaTBAI { get; set; } = null!;
        public string SoftwareNif { get; set; } = null!;

        public static TicketBaiSCUConfiguration FromConfiguration(Dictionary<string, object> configuration)
        {
            var config = new TicketBaiSCUConfiguration();

            if (configuration.ContainsKey("CertificateBase64") && configuration["CertificateBase64"] != null &&
                configuration.ContainsKey("CertificatePassword") && configuration["CertificatePassword"] != null)
            {
                config.Certificate = new X509Certificate2(
                    Convert.FromBase64String(configuration["CertificateBase64"].ToString()!),
                    configuration["CertificatePassword"].ToString()!);
            }

            if (configuration.ContainsKey("EmisorNif") && configuration["EmisorNif"] != null)
            {
                config.EmisorNif = configuration["EmisorNif"].ToString()!;
            }

            if (configuration.ContainsKey("EmisorApellidosNombreRazonSocial") && configuration["EmisorApellidosNombreRazonSocial"] != null)
            {
                config.EmisorApellidosNombreRazonSocial = configuration["EmisorApellidosNombreRazonSocial"].ToString()!;
            }

            if (configuration.ContainsKey("SoftwareVersion") && configuration["SoftwareVersion"] != null)
            {
                config.SoftwareVersion = configuration["SoftwareVersion"].ToString()!;
            }

            if (configuration.ContainsKey("SoftwareLicenciaTBAI") && configuration["SoftwareLicenciaTBAI"] != null)
            {
                config.SoftwareLicenciaTBAI = configuration["SoftwareLicenciaTBAI"].ToString()!;
            }

            if (configuration.ContainsKey("SoftwareName") && configuration["SoftwareName"] != null)
            {
                config.SoftwareName = configuration["SoftwareName"].ToString()!;
            }

            if (configuration.ContainsKey("SoftwareNif") && configuration["SoftwareNif"] != null)
            {
                config.SoftwareNif = configuration["SoftwareNif"].ToString()!;
            }

            return config;
        }
    }
}