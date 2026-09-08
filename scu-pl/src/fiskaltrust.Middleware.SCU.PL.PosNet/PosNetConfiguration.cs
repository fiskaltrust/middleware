using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

public class PosNetConfiguration
{
    /// <summary>
    /// The printer address — see <see cref="PosNetDeviceAddress"/> for the accepted forms:
    /// <c>tcp://192.168.1.50:6666</c> (or plain <c>host:port</c>) for the network interface,
    /// <c>serial://COM9</c> / <c>usb://COM9</c> / <c>/dev/ttyACM0</c> for the USB or COM interface.
    /// </summary>
    public string DeviceUrl { get; set; } = "";

    public int ConnectTimeoutMs { get; set; } = 5_000;

    public int SendTimeoutMs { get; set; } = 5_000;

    public int ReceiveTimeoutMs { get; set; } = 15_000;

    /// <summary>
    /// Line speed of a serial <see cref="DeviceUrl"/>. The printer's own default for its COM port is
    /// 115200; over USB (a CDC virtual COM port) the value is handed to the driver but does not
    /// limit the link. Data bits are fixed at 8 by the device.
    /// </summary>
    public int SerialBaudRate { get; set; } = 115_200;

    /// <summary><c>None</c> (device default), <c>Even</c> or <c>Odd</c>.</summary>
    public string SerialParity { get; set; } = "None";

    /// <summary>1 (device default) or 2.</summary>
    public int SerialStopBits { get; set; } = 1;

    /// <summary>
    /// Flow control on the host side: <c>None</c>, <c>XOnXOff</c> (also <c>XON/XOFF</c>) or
    /// <c>RequestToSend</c> (also <c>RTS/CTS</c>). <c>None</c> is the default because it is what a
    /// USB virtual COM port needs — a hardware handshake on a link that never asserts CTS blocks
    /// every write until it times out. A physical RS-232 cable must match what the printer's COM
    /// menu is set to (its factory default is XON/XOFF).
    /// </summary>
    public string SerialHandshake { get; set; } = "None";

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
        // Cashbox configuration parameters arrive from the Portal as strings, so numbers are read
        // from strings as well.
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        var result = JsonSerializer.Deserialize<PosNetConfiguration>(serialized, options) ?? new PosNetConfiguration();
        result.Validate();
        return result;
    }

    /// <summary>
    /// Fails fast on a configuration no transport could be built from, so a mistyped address or
    /// serial setting surfaces when the SCU is configured rather than at the first receipt.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceUrl))
        {
            throw new PLValidationException("The PosNet SCU requires a DeviceUrl (e.g. tcp://192.168.1.50:6666 or serial://COM9) in its configuration.");
        }
        if (ParseDeviceAddress() is PosNetDeviceAddress.Serial)
        {
            PosNetSerialSettings.From(this);
        }
    }

    public PosNetDeviceAddress ParseDeviceAddress() => PosNetDeviceAddress.Parse(DeviceUrl);

    /// <summary>The TCP endpoint of a network <see cref="DeviceUrl"/>; a serial address has none.</summary>
    public (string Host, int Port) ParseDeviceEndpoint() =>
        ParseDeviceAddress() is PosNetDeviceAddress.Tcp tcp
            ? (tcp.Host, tcp.Port)
            : throw new PLValidationException($"The PosNet DeviceUrl '{DeviceUrl}' is a serial port, not a tcp://host:port address.");
}
