﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI
{
    public class TicketBaiSCUConfiguration
    {
        public X509Certificate2 Certificate { get; set; } = null!;

        public TicketBaiTerritory TicketBaiTerritory { get; set; }

        public string EmisorNif { get; set; } = null!;

        public string EmisorApellidosNombreRazonSocial { get; set; } = null!;

        public static TicketBaiSCUConfiguration FromConfiguration(Dictionary<string, object> configuration)
            => JsonSerializer.Deserialize<TicketBaiSCUConfiguration>(JsonSerializer.Serialize(configuration)) ?? new TicketBaiSCUConfiguration();
    }

    public enum TicketBaiTerritory
    {
        Araba,
        Bizkaia,
        Gipuzkoa
    }
}