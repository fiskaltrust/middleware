using Android.Content;
using Android.Hardware.Usb;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Android;

/// <summary>
/// Opens a POSNET printer attached to the Android device over USB (on a phone or tablet through an
/// OTG adapter). The printer's USB interface is a CDC-ACM virtual COM port: a communication
/// interface that takes the line coding and the DTR/RTS state, and a data interface with one bulk
/// endpoint each way that carries the POSNET frames.
/// </summary>
/// <remarks>
/// Access to a USB device is the user's to grant on Android. This factory only checks it — asking
/// needs an activity in front of the user, see <see cref="PosNetUsbPermission"/>, or the app's
/// <c>USB_DEVICE_ATTACHED</c> intent filter with a device filter for the printer.
/// </remarks>
public sealed class AndroidUsbHostLinkFactory : IPosNetUsbHostLinkFactory
{
    private const int ControlTimeoutMs = 1_000;

    private readonly Context _context;

    public AndroidUsbHostLinkFactory(Context context)
    {
        // The application context outlives any activity the SCU happened to be built from.
        _context = context.ApplicationContext ?? context;
    }

    /// <remarks>
    /// The device can go away, or the permission be revoked, between any two calls here; Android then
    /// throws its own exceptions. They leave as the two the transport expects of a link, so a printer
    /// that was never reached is reported as unreachable — safe to resend — and not as a crash.
    /// </remarks>
    public IPosNetUsbHostLink Open(PosNetDeviceAddress.UsbHost address, CdcLineCoding lineCoding)
    {
        try
        {
            return OpenDevice(address, lineCoding);
        }
        catch (Java.Lang.SecurityException ex)
        {
            throw new UnauthorizedAccessException($"The app has no access to the USB device {address}: {ex.Message}", ex);
        }
        catch (Java.Lang.Exception ex)
        {
            throw new IOException($"The USB device {address} could not be opened: {ex.Message}", ex);
        }
    }

    private IPosNetUsbHostLink OpenDevice(PosNetDeviceAddress.UsbHost address, CdcLineCoding lineCoding)
    {
        var manager = UsbDevices.Manager(_context);
        var device = UsbDevices.Find(manager, address)
            ?? throw new IOException($"No USB device {address} is attached.");
        if (!manager.HasPermission(device))
        {
            throw new UnauthorizedAccessException($"The app has not been granted access to the USB device {address}.");
        }

        var (control, data, bulkIn, bulkOut) = SelectInterfaces(device, address);
        var connection = manager.OpenDevice(device)
            ?? throw new IOException($"The USB device {address} could not be opened.");
        var claimed = new List<UsbInterface>();
        try
        {
            foreach (var usbInterface in control is null ? [data] : new[] { control, data })
            {
                // force: detach whatever kernel driver bound the interface (cdc_acm on some devices).
                if (!connection.ClaimInterface(usbInterface, true))
                {
                    throw new IOException($"Interface {usbInterface.Id} of the USB device {address} could not be claimed.");
                }
                claimed.Add(usbInterface);
            }

            if (control is not null)
            {
                var coding = lineCoding.ToBytes();
                Control(connection, CdcLineCoding.SetLineCodingRequest, 0, control.Id, coding, address, "SET_LINE_CODING");
                Control(connection, CdcLineCoding.SetControlLineStateRequest, CdcLineCoding.DtrAndRts, control.Id, null, address, "SET_CONTROL_LINE_STATE");
            }

            return new AndroidUsbHostLink(manager, device.DeviceName, connection, claimed, bulkIn, bulkOut);
        }
        catch
        {
            Release(connection, claimed);
            throw;
        }
    }

    /// <summary>Best effort: a failure here must not hide the one that made the open fail.</summary>
    private static void Release(UsbDeviceConnection connection, IEnumerable<UsbInterface> claimed)
    {
        try
        {
            foreach (var usbInterface in claimed)
            {
                connection.ReleaseInterface(usbInterface);
            }
            connection.Close();
        }
        catch (Java.Lang.Exception)
        {
            // An unplugged device can leave nothing to release.
        }
    }

    /// <summary>
    /// The CDC data interface and its bulk endpoints, with the communication interface that takes the
    /// control requests. A device without a CDC data interface is driven through its first interface
    /// with a bulk endpoint each way, and no control requests — the frames are all the protocol needs.
    /// </summary>
    private static (UsbInterface? Control, UsbInterface Data, UsbEndpoint BulkIn, UsbEndpoint BulkOut) SelectInterfaces(UsbDevice device, PosNetDeviceAddress.UsbHost address)
    {
        var interfaces = Enumerable.Range(0, device.InterfaceCount).Select(device.GetInterface).ToList();
        var control = interfaces.FirstOrDefault(i => i.InterfaceClass == UsbClass.Comm);
        var candidates = interfaces.Where(i => i.InterfaceClass == UsbClass.CdcData).Concat(interfaces);
        foreach (var data in candidates)
        {
            var endpoints = Enumerable.Range(0, data.EndpointCount).Select(data.GetEndpoint).Where(e => e?.Type == UsbAddressing.XferBulk).ToList();
            var bulkIn = endpoints.FirstOrDefault(e => e!.Direction == UsbAddressing.In);
            var bulkOut = endpoints.FirstOrDefault(e => e!.Direction == UsbAddressing.Out);
            if (bulkIn is not null && bulkOut is not null)
            {
                return (data.InterfaceClass == UsbClass.CdcData ? control : null, data, bulkIn, bulkOut);
            }
        }
        throw new IOException($"The USB device {address} has no interface with a bulk endpoint each way — is its PC interface set to USB?");
    }

    private static void Control(UsbDeviceConnection connection, int request, int value, int interfaceId, byte[]? data, PosNetDeviceAddress.UsbHost address, string name)
    {
        var result = connection.ControlTransfer((UsbAddressing)CdcLineCoding.RequestTypeClassInterfaceOut, request, value, interfaceId, data, data?.Length ?? 0, ControlTimeoutMs);
        if (result < 0)
        {
            throw new IOException($"The USB device {address} refused {name}.");
        }
    }
}
