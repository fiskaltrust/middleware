using System;
using System.IO.Ports;
using System.Linq;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// The serial-link parameters of a <see cref="PosNetConfiguration"/> whose DeviceUrl names a COM
/// port, resolved into <see cref="System.IO.Ports"/> terms and validated against what the printer
/// can actually be set to (manual: speed, stop bits, parity and flow control are configurable, the
/// data bits are fixed at 8).
/// </summary>
public sealed record PosNetSerialSettings(string PortName, int BaudRate, Parity Parity, StopBits StopBits, Handshake Handshake)
{
    /// <summary>Fixed by the device; not configurable on the printer's COM menu.</summary>
    public const int DataBits = 8;

    /// <summary>
    /// Reads the settings for an address the caller has already recognized as serial, so the URL is
    /// parsed once — by whoever selects the transport — rather than again here.
    /// </summary>
    public static PosNetSerialSettings From(PosNetDeviceAddress.Serial serial, PosNetConfiguration configuration)
    {
        if (configuration.SerialBaudRate <= 0)
        {
            throw new PLValidationException($"The PosNet SerialBaudRate '{configuration.SerialBaudRate}' is not a valid line speed; the printer's default is 115200.");
        }
        var stopBits = configuration.SerialStopBits switch
        {
            1 => StopBits.One,
            2 => StopBits.Two,
            var other => throw new PLValidationException($"The PosNet SerialStopBits '{other}' is not supported; the printer offers 1 or 2."),
        };
        return new PosNetSerialSettings(serial.PortName, configuration.SerialBaudRate, ParseParity(configuration.SerialParity), stopBits, ParseHandshake(configuration.SerialHandshake));
    }

    private static Parity ParseParity(string? value)
    {
        var normalized = (value ?? "").Trim();
        // A parameter left blank is the device default, as it is for the handshake — the Portal
        // sends a cleared field as an empty string, and refusing it would keep the SCU from starting
        // over a setting the operator did not set.
        if (normalized.Length == 0)
        {
            return Parity.None;
        }
        if (Enum.TryParse<Parity>(normalized, ignoreCase: true, out var parity) && parity is Parity.None or Parity.Even or Parity.Odd)
        {
            return parity;
        }
        throw new PLValidationException($"The PosNet SerialParity '{value}' is not supported; use None, Even or Odd.");
    }

    private static Handshake ParseHandshake(string? value)
    {
        // The printer's own menu spells the choices XON/XOFF, RTS/CTS and Brak (none); accept those
        // next to the System.IO.Ports names.
        var normalized = (value ?? "").Trim().Replace("/", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (normalized.Equals("RtsCts", StringComparison.OrdinalIgnoreCase))
        {
            return Handshake.RequestToSend;
        }
        if (normalized.Equals("XonXoff", StringComparison.OrdinalIgnoreCase))
        {
            return Handshake.XOnXOff;
        }
        if (normalized.Equals("Brak", StringComparison.OrdinalIgnoreCase) || normalized.Length == 0)
        {
            return Handshake.None;
        }
        // Only the names the printer offers are accepted. Enum.TryParse would also take a number —
        // an undefined one passes validation here only to be refused by the SerialPort constructor
        // at the first receipt (reported as an unreachable printer), and a defined one would turn a
        // "1" the operator meant as a yes into XON/XOFF.
        if (!normalized.All(char.IsAsciiDigit)
            && Enum.TryParse<Handshake>(normalized, ignoreCase: true, out var handshake)
            && handshake is Handshake.None or Handshake.XOnXOff or Handshake.RequestToSend)
        {
            return handshake;
        }
        throw new PLValidationException($"The PosNet SerialHandshake '{value}' is not supported; use None, XOnXOff (XON/XOFF) or RequestToSend (RTS/CTS).");
    }
}
