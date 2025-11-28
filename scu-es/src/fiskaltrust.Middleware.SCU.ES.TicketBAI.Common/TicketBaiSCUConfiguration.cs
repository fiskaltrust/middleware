using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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

            if (configuration.TryGetValue("CertificateBase64", out var certificateBase64) && certificateBase64 != null &&
                configuration.TryGetValue("CertificatePassword", out var certificatePassword) && certificatePassword != null)
            {
                config.Certificate = new X509Certificate2(
                    Convert.FromBase64String(certificateBase64.ToString()!),
                    certificatePassword.ToString()!);
            }

            if (configuration.TryGetValue("EmisorNif", out var emisorNif) && emisorNif != null)
            {
                config.EmisorNif = emisorNif.ToString()!;
            }

            if (configuration.TryGetValue("EmisorApellidosNombreRazonSocial", out var emisorApellidosNombreRazonSocial) && emisorApellidosNombreRazonSocial != null)
            {
                config.EmisorApellidosNombreRazonSocial = emisorApellidosNombreRazonSocial.ToString()!;
            }

            if (configuration.TryGetValue("SoftwareVersion", out var softwareVersion) && softwareVersion != null)
            {
                config.SoftwareVersion = softwareVersion.ToString()!;
            }

            if (configuration.TryGetValue("SoftwareLicenciaTBAI", out var softwareLicenciaTBAI) && softwareLicenciaTBAI != null)
            {
                config.SoftwareLicenciaTBAI = softwareLicenciaTBAI.ToString()!;
            }

            if (configuration.TryGetValue("SoftwareName", out var softwareName) && softwareName != null)
            {
                config.SoftwareName = softwareName.ToString()!;
            }

            if (configuration.TryGetValue("SoftwareNif", out var softwareNif) && softwareNif != null)
            {
                config.SoftwareNif = softwareNif.ToString()!;
            }

            return config;
        }
    }
}