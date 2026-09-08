using System;
using System.Globalization;
using System.Text.RegularExpressions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet;

/// <summary>
/// Where the printer is, as parsed from <see cref="PosNetConfiguration.DeviceUrl"/>. The POSNET
/// protocol is the same on every carrier the device offers (Ethernet, USB, RS-232, Bluetooth), so
/// the address only decides which transport opens the byte stream:
/// <list type="bullet">
/// <item><see cref="Tcp"/> — <c>tcp://host:port</c> or plain <c>host:port</c>: the printer's PC
/// interface set to TCP/IP, reached over the network (or a tunnel).</item>
/// <item><see cref="Serial"/> — <c>serial://COM9</c>, <c>usb://COM9</c>, plain <c>COM9</c>, or a
/// device path such as <c>/dev/ttyACM0</c> (also <c>serial:///dev/ttyACM0</c>): the printer's PC
/// interface set to USB or COM. Over USB the device enumerates as a CDC virtual COM port, so both
/// look like a serial port to the host.</item>
/// </list>
/// </summary>
public abstract record PosNetDeviceAddress
{
    private static readonly Regex s_windowsComPort = new("^COM[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private PosNetDeviceAddress() { }

    public sealed record Tcp(string Host, int Port) : PosNetDeviceAddress
    {
        public override string ToString() => $"{Host}:{Port}";
    }

    public sealed record Serial(string PortName) : PosNetDeviceAddress
    {
        public override string ToString() => PortName;
    }

    public static PosNetDeviceAddress Parse(string deviceUrl)
    {
        var address = (deviceUrl ?? "").Trim();
        if (address.Length == 0)
        {
            throw new PLValidationException("The PosNet SCU requires a DeviceUrl (e.g. tcp://192.168.1.50:6666 or serial://COM9) in its configuration.");
        }

        if (TryStripScheme(address, "serial://", out var portName) || TryStripScheme(address, "usb://", out portName))
        {
            if (portName.Length == 0)
            {
                throw new PLValidationException($"The PosNet DeviceUrl '{deviceUrl}' names no serial port (expected e.g. serial://COM9 or serial:///dev/ttyACM0).");
            }
            return new Serial(portName);
        }

        if (s_windowsComPort.IsMatch(address) || address.StartsWith("/dev/", StringComparison.Ordinal))
        {
            return new Serial(address);
        }

        if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Port > 0 && !string.IsNullOrEmpty(uri.Host))
        {
            return new Tcp(uri.Host, uri.Port);
        }

        // Whether the port is one anything listens on is the transport's business: a closed or
        // nonsensical port fails at connect time as "unreachable", which is the truthful outcome.
        var separator = address.LastIndexOf(':');
        if (separator > 0 && int.TryParse(address[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
        {
            return new Tcp(address[..separator], port);
        }

        throw new PLValidationException($"The PosNet DeviceUrl '{deviceUrl}' is neither a tcp://host:port address nor a serial port (serial://COM9, /dev/ttyACM0).");
    }

    private static bool TryStripScheme(string address, string scheme, out string remainder)
    {
        if (address.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            remainder = address[scheme.Length..].Trim();
            return true;
        }
        remainder = "";
        return false;
    }
}
