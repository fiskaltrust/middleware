using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

public class PosNetConfiguration
{
    /// <summary>The printer address, e.g. <c>tcp://192.168.1.50:6666</c> (or plain <c>host:port</c>).</summary>
    public string DeviceUrl { get; set; } = "";

    public int ConnectTimeoutMs { get; set; } = 5_000;

    public int SendTimeoutMs { get; set; } = 5_000;

    public int ReceiveTimeoutMs { get; set; } = 15_000;

    /// <summary>
    /// The PTU rate table as programmed on the printer, used to resolve the trline vt slot index.
    /// Reading it live from the device (vatget) is not part of the first milestone, so the table
    /// is configuration with the customary Polish layout as default.
    /// </summary>
    public List<PLVatRateTableEntry> VatRateTable { get; set; } = DefaultVatRateTable();

    public static List<PLVatRateTableEntry> DefaultVatRateTable() =>
    [
        new() { PtuSlot = "A", VatRatePercent = 23m },
        new() { PtuSlot = "B", VatRatePercent = 8m },
        new() { PtuSlot = "C", VatRatePercent = 5m },
        new() { PtuSlot = "D", VatRatePercent = 0m },
        new() { PtuSlot = "G", IsExempt = true },
    ];

    public static PosNetConfiguration FromConfiguration(Dictionary<string, object> configuration)
    {
        var serialized = JsonSerializer.Serialize(configuration);
        var result = JsonSerializer.Deserialize<PosNetConfiguration>(serialized, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new PosNetConfiguration();
        if (string.IsNullOrWhiteSpace(result.DeviceUrl))
        {
            throw new PLValidationException("The PosNet SCU requires a DeviceUrl (e.g. tcp://192.168.1.50:6666) in its configuration.");
        }
        return result;
    }

    public (string Host, int Port) ParseDeviceEndpoint()
    {
        var address = DeviceUrl.Trim();
        if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Port > 0 && !string.IsNullOrEmpty(uri.Host))
        {
            return (uri.Host, uri.Port);
        }

        var separator = address.LastIndexOf(':');
        if (separator > 0 && int.TryParse(address[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
        {
            return (address[..separator], port);
        }

        throw new PLValidationException($"The PosNet DeviceUrl '{DeviceUrl}' is not a valid tcp://host:port address.");
    }
}
