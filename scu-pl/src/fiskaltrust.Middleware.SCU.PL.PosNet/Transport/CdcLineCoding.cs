using System;
using System.Buffers.Binary;
using System.IO.Ports;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// The CDC-ACM line coding (USB CDC PSTN 1.2, §6.3.10 SET_LINE_CODING) a host sends to a USB virtual
/// COM port before using it — what an operating-system serial driver such as the Windows usbser
/// sends on its own when the port is opened, and what a host that opens the device itself
/// (<see cref="UsbHostPosNetTransport"/>) has to send in its place.
/// </summary>
/// <param name="CharFormat">Stop bits: 0 = 1, 2 = 2.</param>
/// <param name="ParityType">0 = none, 1 = odd, 2 = even.</param>
public sealed record CdcLineCoding(int BaudRate, byte CharFormat, byte ParityType, byte DataBits)
{
    /// <summary>bmRequestType of a class request to an interface, host to device.</summary>
    public const int RequestTypeClassInterfaceOut = 0x21;

    public const int SetLineCodingRequest = 0x20;

    public const int SetControlLineStateRequest = 0x22;

    /// <summary>
    /// wValue of SET_CONTROL_LINE_STATE with DTR and RTS asserted — a CDC device may hold its answers
    /// back until the host signals that it is listening, as <see cref="SerialPosNetTransport"/> does
    /// with <c>DtrEnable</c>.
    /// </summary>
    public const int DtrAndRts = 0x0003;

    /// <summary>
    /// The configured serial settings as line coding. The handshake has no place in it: flow control
    /// on a USB link is the bus's business, which is why <c>None</c> is the configured default too.
    /// </summary>
    public static CdcLineCoding From(PosNetConfiguration configuration)
    {
        var settings = PosNetSerialSettings.From(configuration.DeviceUrl, configuration);
        return new CdcLineCoding(
            settings.BaudRate,
            settings.StopBits == StopBits.Two ? (byte)2 : (byte)0,
            settings.Parity switch
            {
                Parity.Odd => 1,
                Parity.Even => 2,
                _ => 0,
            },
            PosNetSerialSettings.DataBits);
    }

    /// <summary>The 7-byte data stage of SET_LINE_CODING: dwDTERate (little endian), bCharFormat, bParityType, bDataBits.</summary>
    public byte[] ToBytes()
    {
        var bytes = new byte[7];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), BaudRate);
        bytes[4] = CharFormat;
        bytes[5] = ParityType;
        bytes[6] = DataBits;
        return bytes;
    }
}
