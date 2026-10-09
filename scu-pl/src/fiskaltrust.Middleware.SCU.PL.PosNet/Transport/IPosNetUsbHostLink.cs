using System;
using System.IO;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// The raw byte pipe to a POSNET printer that the host application opened itself over USB — the
/// platform-specific half of <see cref="UsbHostPosNetTransport"/>. The transport owns everything the
/// protocol cares about (framing, deadlines, what counts as delivered); a link only moves bytes, so
/// a platform needs nothing but this to drive the printer (on Android:
/// <c>fiskaltrust.Middleware.SCU.PL.PosNet.Android</c>).
/// </summary>
/// <remarks>
/// Failures are reported as <see cref="IOException"/> — or <see cref="UnauthorizedAccessException"/>
/// when the platform has not granted access to the device — so the transport can tell them from its
/// own bugs.
/// </remarks>
public interface IPosNetUsbHostLink : IDisposable
{
    /// <summary>Whether the device behind the link is still attached, so a kept link can be reused.</summary>
    bool IsAttached { get; }

    /// <summary>Writes the whole buffer or throws; a partial write is a failure.</summary>
    void Write(byte[] buffer, int timeoutMs);

    /// <summary>
    /// Reads what the device has sent, up to the buffer's length, waiting at most
    /// <paramref name="timeoutMs"/>. Returns 0 when nothing arrived in that time.
    /// </summary>
    int Read(byte[] buffer, int timeoutMs);
}

/// <summary>Opens a <see cref="IPosNetUsbHostLink"/> to the device a <c>usbhost://</c> DeviceUrl names.</summary>
public interface IPosNetUsbHostLinkFactory
{
    /// <summary>
    /// Finds the device, claims its interfaces and sends the line coding, ready for the first frame.
    /// Throws <see cref="IOException"/> when the device is not attached or cannot be opened, and
    /// <see cref="UnauthorizedAccessException"/> when access to it has not been granted.
    /// </summary>
    IPosNetUsbHostLink Open(PosNetDeviceAddress.UsbHost address, CdcLineCoding lineCoding);
}
