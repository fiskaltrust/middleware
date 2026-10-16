using Android.Hardware.Usb;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Android;

/// <summary>
/// An open, claimed USB connection to the printer, moving bytes over its two bulk endpoints.
/// </summary>
/// <remarks>
/// Android's bulk transfer reports a timeout and a failure alike, as -1. A read tells them apart by
/// the clock: -1 after the whole wait is "nothing arrived", -1 well before it is a broken transfer
/// (an unplugged printer, a stalled endpoint) and fails — otherwise the transport would poll a dead
/// endpoint as fast as it answers until its deadline. A write takes -1 as a failure either way: a
/// frame that was not taken in time is not delivered.
/// </remarks>
internal sealed class AndroidUsbHostLink : IPosNetUsbHostLink
{
    private readonly UsbManager _manager;
    private readonly string _deviceName;
    private readonly UsbDeviceConnection _connection;
    private readonly IReadOnlyList<UsbInterface> _interfaces;
    private readonly UsbEndpoint _bulkIn;
    private readonly UsbEndpoint _bulkOut;

    /// <summary>
    /// Exactly one packet. A read must take whole packets — a smaller buffer overflows — and must not
    /// ask for more than one: a transfer longer than a packet only ends early on a short packet, so
    /// an answer that fills its last packet exactly (with no zero-length packet after it) would hold
    /// the read until its timeout, and Android drops what a timed-out transfer received. What the
    /// caller had no room for waits here for the next read.
    /// </summary>
    private readonly byte[] _received;
    private int _receivedOffset;
    private int _receivedCount;

    public AndroidUsbHostLink(UsbManager manager, string deviceName, UsbDeviceConnection connection, IReadOnlyList<UsbInterface> interfaces, UsbEndpoint bulkIn, UsbEndpoint bulkOut)
    {
        _manager = manager;
        _deviceName = deviceName;
        _connection = connection;
        _interfaces = interfaces;
        _bulkIn = bulkIn;
        _bulkOut = bulkOut;
        // 64 is the full-speed bulk packet size, should a device report none.
        _received = new byte[bulkIn.MaxPacketSize > 0 ? bulkIn.MaxPacketSize : 64];
    }

    /// <summary>
    /// How much earlier than its timeout a -1 must come to count as a failure rather than a timeout —
    /// slack for the clock and for the transfer's own setup.
    /// </summary>
    private const int TimeoutSlackMs = 10;

    public bool IsAttached
    {
        get
        {
            try
            {
                return _manager.DeviceList?.ContainsKey(_deviceName) == true;
            }
            catch (Java.Lang.Exception)
            {
                return false;
            }
        }
    }

    public void Write(byte[] buffer, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        var written = 0;
        while (written < buffer.Length)
        {
            var remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
            {
                throw new IOException($"The printer took only {written} of {buffer.Length} bytes within {timeoutMs} ms.");
            }
            var sent = Transfer(() => _connection.BulkTransfer(_bulkOut, buffer, written, buffer.Length - written, (int)remaining));
            if (sent < 0)
            {
                throw new IOException($"Writing to the printer failed after {written} of {buffer.Length} bytes.");
            }
            written += sent;
        }
    }

    public int Read(byte[] buffer, int timeoutMs)
    {
        if (_receivedCount == 0)
        {
            var started = Environment.TickCount64;
            var read = Transfer(() => _connection.BulkTransfer(_bulkIn, _received, 0, _received.Length, timeoutMs));
            if (read < 0 && Environment.TickCount64 - started < timeoutMs - TimeoutSlackMs)
            {
                throw new IOException(IsAttached
                    ? "Reading from the printer failed."
                    : "The printer was detached.");
            }
            if (read <= 0)
            {
                return 0;
            }
            _receivedOffset = 0;
            _receivedCount = read;
        }

        var count = Math.Min(buffer.Length, _receivedCount);
        Array.Copy(_received, _receivedOffset, buffer, 0, count);
        _receivedOffset += count;
        _receivedCount -= count;
        return count;
    }

    /// <summary>Java failures surface as <see cref="IOException"/>, the one failure type the transport expects of a link.</summary>
    private static int Transfer(Func<int> transfer)
    {
        try
        {
            return transfer();
        }
        catch (Java.Lang.Exception ex)
        {
            throw new IOException($"The USB transfer failed: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        try
        {
            foreach (var usbInterface in _interfaces)
            {
                _connection.ReleaseInterface(usbInterface);
            }
            _connection.Close();
        }
        catch (Java.Lang.Exception)
        {
            // An unplugged device can leave nothing to release.
        }
    }
}
