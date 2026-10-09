using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// USB transport to a POSNET printer that the host application opens itself — on Android, where no
/// serial driver stands between the app and the device. The printer is the same CDC virtual COM
/// port <see cref="SerialPosNetTransport"/> drives through the operating system; here the bytes go
/// through a platform <see cref="IPosNetUsbHostLink"/> instead, and this class keeps what the
/// protocol requires of them.
/// The link is opened lazily and kept; after any failure it is dropped and opened again on the next
/// command. Failure semantics mirror the other transports: a link that cannot be opened — or
/// cleared of stale input before the frame goes out — is <see cref="PLDeviceUnreachableException"/>
/// (nothing was delivered, safe to resend); a failure while writing or waiting for the answer is
/// <see cref="PosNetAmbiguousResponseException"/> (the device may have executed the command — must
/// not resend).
/// </summary>
public sealed class UsbHostPosNetTransport : IPosNetTransport
{
    /// <summary>
    /// How long one read waits while clearing stale input. Short on purpose: it only has to collect
    /// what is already buffered, and it is paid on every command.
    /// </summary>
    private const int DrainReadTimeoutMs = 10;

    /// <summary>
    /// Bounds the clearing, so a device that keeps sending cannot hold the command back forever; what
    /// is left after that is dropped by <see cref="ResponseFrameBuffer"/> up to the next STX.
    /// </summary>
    private const int MaxDrainReads = 64;

    private readonly PosNetDeviceAddress.UsbHost _address;
    private readonly PosNetConfiguration _configuration;
    private readonly CdcLineCoding _lineCoding;
    private readonly IPosNetUsbHostLinkFactory _links;
    private IPosNetUsbHostLink? _link;

    public UsbHostPosNetTransport(PosNetDeviceAddress.UsbHost address, PosNetConfiguration configuration, IPosNetUsbHostLinkFactory links)
    {
        _address = address;
        _configuration = configuration;
        _lineCoding = CdcLineCoding.From(configuration);
        _links = links;
    }

    public PosNetDeviceAddress.UsbHost Address => _address;

    public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
        => Task.Run(() => SendReceive(frame, cancellationToken), cancellationToken);

    private byte[] SendReceive(byte[] frame, CancellationToken cancellationToken)
    {
        IPosNetUsbHostLink link;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            link = GetOpenLink();
            // Anything already on the line belongs to an earlier command — the late answer to one
            // that timed out. Unlike the serial port's DiscardInBuffer this is a real read, and it
            // can fail; it happens before the frame goes out, so such a failure delivered nothing.
            Drain(link);
        }
        catch (Exception ex) when (IsLinkFailure(ex))
        {
            DropConnection();
            throw new PLDeviceUnreachableException(DescribeOpenFailure(ex), ex);
        }

        try
        {
            link.Write(frame, _configuration.SendTimeoutMs);
        }
        catch (Exception ex) when (IsLinkFailure(ex))
        {
            // The frame may have been partially written; whether the device saw a complete command
            // is unknown, so this is already the ambiguous case.
            DropConnection();
            throw new PosNetAmbiguousResponseException($"Sending a command to the POSNET printer at {_address} was interrupted — the device state is unknown. Verify the device before retrying.", ex);
        }

        try
        {
            return ReadFrame(link, cancellationToken);
        }
        catch (Exception ex) when (IsLinkFailure(ex))
        {
            DropConnection();
            var what = ex is TimeoutException
                ? $"did not answer within {_configuration.ReceiveTimeoutMs} ms"
                : $"stopped answering ({ex.Message})";
            throw new PosNetAmbiguousResponseException($"The POSNET printer at {_address} {what} — the command may or may not have been executed. Verify the device before retrying.", ex);
        }
    }

    private IPosNetUsbHostLink GetOpenLink()
    {
        // A printer that was power-cycled or unplugged while the till was idle leaves the kept link
        // dead; writing to it would be reported as ambiguous although nothing was ever delivered.
        if (_link is { IsAttached: true })
        {
            return _link;
        }

        DropConnection();
        _link = _links.Open(_address, _lineCoding);
        return _link;
    }

    private static void Drain(IPosNetUsbHostLink link)
    {
        var chunk = new byte[256];
        for (var reads = 0; reads < MaxDrainReads && link.Read(chunk, DrainReadTimeoutMs) > 0; reads++)
        {
        }
    }

    private byte[] ReadFrame(IPosNetUsbHostLink link, CancellationToken cancellationToken)
    {
        var buffer = new ResponseFrameBuffer();
        var chunk = new byte[256];
        var deadline = Environment.TickCount64 + _configuration.ReceiveTimeoutMs;
        while (!buffer.IsComplete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
            {
                throw new TimeoutException($"No complete frame arrived within {_configuration.ReceiveTimeoutMs} ms.");
            }
            // The budget is for the whole frame, not per chunk; a read that times out returns 0 and
            // the deadline decides.
            var read = link.Read(chunk, (int)Math.Min(remaining, int.MaxValue));
            if (read > 0)
            {
                buffer.Append(chunk.AsSpan(0, read));
            }
            else if (!link.IsAttached)
            {
                // An unplugged printer answers nothing ever; waiting out the deadline would only
                // report it later, and as a slow printer.
                throw new IOException("The printer was detached while its answer was awaited.");
            }
        }
        return buffer.ToArray();
    }

    private string DescribeOpenFailure(Exception ex) => ex switch
    {
        UnauthorizedAccessException => $"Access to the POSNET printer at {_address} has not been granted — confirm the USB permission prompt on the device.",
        OperationCanceledException => $"Opening the POSNET printer at {_address} was cancelled.",
        _ => $"Could not open the POSNET printer at {_address} — is it connected (on a phone or tablet: through a USB-OTG adapter) and its PC interface set to USB?",
    };

    private static bool IsLinkFailure(Exception ex) =>
        ex is IOException or TimeoutException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException;

    private void DropConnection()
    {
        try
        {
            _link?.Dispose();
        }
        catch (Exception ex) when (IsLinkFailure(ex))
        {
            // A device that was unplugged can leave the link undisposable; nothing left to release.
        }
        _link = null;
    }

    public void Dispose() => DropConnection();
}
