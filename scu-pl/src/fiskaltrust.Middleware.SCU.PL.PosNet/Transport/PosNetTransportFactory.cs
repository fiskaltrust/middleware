using System;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Picks the transport the configured DeviceUrl calls for: TCP for a network address, serial for a
/// USB or COM port, USB host for a device the application opens itself. Everything above the
/// transport is the same for all of them.
/// </summary>
public static class PosNetTransportFactory
{
    /// <summary>
    /// The DeviceUrl is parsed once, here, and the recognized address is handed to the transport —
    /// so no transport re-derives from the string what selecting it already established.
    /// </summary>
    /// <param name="usbHostLinks">
    /// The platform's way to open a USB device itself, needed only for a <c>usbhost://</c> address.
    /// The SCU cannot bring one of its own: the USB host API is the platform's (on Android:
    /// <c>fiskaltrust.Middleware.SCU.PL.PosNet.Android</c>), so the host registers it.
    /// </param>
    public static IPosNetTransport Create(PosNetConfiguration configuration, IPosNetUsbHostLinkFactory? usbHostLinks = null) => configuration.ParseDeviceAddress() switch
    {
        PosNetDeviceAddress.Tcp tcp => new TcpPosNetTransport(tcp, configuration),
        PosNetDeviceAddress.Serial serial => new SerialPosNetTransport(serial, configuration),
        PosNetDeviceAddress.UsbHost usb when usbHostLinks is not null => new UsbHostPosNetTransport(usb, configuration, usbHostLinks),
        PosNetDeviceAddress.UsbHost usb => throw new NotSupportedException(
            $"The device address '{usb}' is opened by the host application over USB, and this host registers no {nameof(IPosNetUsbHostLinkFactory)} — on Android, add fiskaltrust.Middleware.SCU.PL.PosNet.Android; elsewhere, use serial:// with the printer's COM port."),
        var other => throw new NotSupportedException($"No transport for the device address '{other}'."),
    };
}
