using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Serial transport to a POSNET printer over its USB or COM interface. Over USB the printer
/// enumerates as a CDC virtual COM port (on Windows the inbox usbser driver: "USB Serial Device
/// (COMn)"), so both interfaces are one <see cref="SerialPort"/> here. The port is opened lazily and
/// kept open; after any failure it is closed and reopened on the next command, which also discards
/// whatever a late answer left in the driver's buffer. The transaction state lives in the device,
/// not in the port, so reopening between commands is safe.
/// Failure semantics mirror <see cref="TcpPosNetTransport"/>: a port that cannot be opened is
/// <see cref="PLDeviceUnreachableException"/> (nothing was delivered — safe to resend); a failure
/// while writing or waiting for the answer is <see cref="PosNetAmbiguousResponseException"/> (the
/// device may have executed the command — must not resend).
/// The blocking <see cref="SerialPort"/> API is used deliberately: its timeouts are enforced by the
/// driver on every platform, whereas the base stream's asynchronous reads do not honour cancellation
/// on Windows. The blocking work runs on the thread pool, one command at a time.
/// </summary>
public sealed class SerialPosNetTransport : IPosNetTransport
{
    private readonly PosNetConfiguration _configuration;
    private readonly PosNetSerialSettings _settings;
    private SerialPort? _port;

    public SerialPosNetTransport(PosNetDeviceAddress.Serial address, PosNetConfiguration configuration)
    {
        _configuration = configuration;
        _settings = PosNetSerialSettings.From(address, configuration);
    }

    public string PortName => _settings.PortName;

    public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
        => Task.Run(() => SendReceive(frame, cancellationToken), cancellationToken);

    private byte[] SendReceive(byte[] frame, CancellationToken cancellationToken)
    {
        SerialPort port;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            port = GetOpenPort();
        }
        catch (Exception ex)
        {
            // Whatever kept the port from opening — busy, missing, unplugged, a driver that is not
            // there — nothing has reached the device.
            DropConnection();
            throw new PLDeviceUnreachableException(DescribeOpenFailure(ex), ex);
        }

        try
        {
            // Anything already on the line belongs to an earlier command — the late answer to one
            // that timed out, or a duplicate after a USB hiccup. It is dropped here rather than only
            // when the port is reopened, because a frame that arrives after that reopen would
            // otherwise be read as the answer to this command and shift every answer that follows.
            port.DiscardInBuffer();
            port.Write(frame, 0, frame.Length);
        }
        catch (Exception ex) when (IsSerialFailure(ex))
        {
            // The frame may have been partially written; whether the device saw a complete
            // command is unknown, so this is already the ambiguous case.
            DropConnection();
            throw new PosNetAmbiguousResponseException($"Sending a command to the POSNET printer on {PortName} was interrupted — the device state is unknown. Verify the device before retrying.", ex);
        }

        try
        {
            return ReadFrame(port, cancellationToken);
        }
        catch (Exception ex) when (IsSerialFailure(ex))
        {
            DropConnection();
            throw new PosNetAmbiguousResponseException($"The POSNET printer on {PortName} did not answer within {_configuration.ReceiveTimeoutMs} ms — the command may or may not have been executed. Verify the device before retrying.", ex);
        }
    }

    private SerialPort GetOpenPort()
    {
        if (IsUsable(_port))
        {
            return _port!;
        }

        DropConnection();
        var port = new SerialPort(_settings.PortName, _settings.BaudRate, _settings.Parity, PosNetSerialSettings.DataBits, _settings.StopBits)
        {
            Handshake = _settings.Handshake,
            // A CDC device may hold its answers back until the host signals that it is listening.
            DtrEnable = true,
            // The write budget never changes; the read budget is set per chunk in ReadFrame, which
            // owns the deadline for the whole frame.
            WriteTimeout = _configuration.SendTimeoutMs,
        };
        if (_settings.Handshake is not (Handshake.RequestToSend or Handshake.RequestToSendXOnXOff))
        {
            // Only settable while RTS is not driven by the handshake itself.
            port.RtsEnable = true;
        }

        try
        {
            port.Open();
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }
        catch
        {
            port.Dispose();
            throw;
        }
        _port = port;
        return port;
    }

    /// <summary>
    /// Whether the kept port can still carry a command, the counterpart of
    /// <c>TcpPosNetTransport.IsUsable</c>. <see cref="SerialPort.IsOpen"/> only says that the
    /// managed handle was opened: a USB printer that was power-cycled while the till was idle
    /// re-enumerates and leaves the old handle dead but open-looking. Writing to it fails, and that
    /// failure would be reported as an ambiguous outcome ("verify the device") although nothing was
    /// ever delivered. Touching the driver — reading the buffered byte count — is what surfaces it.
    /// </summary>
    private static bool IsUsable(SerialPort? port)
    {
        if (port is not { IsOpen: true })
        {
            return false;
        }

        try
        {
            _ = port.BytesToRead;
            return true;
        }
        catch (Exception ex) when (IsSerialFailure(ex) || ex is ObjectDisposedException)
        {
            return false;
        }
    }

    private byte[] ReadFrame(SerialPort port, CancellationToken cancellationToken)
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
            // Read returns as soon as at least one byte is there, or throws TimeoutException; the
            // budget is for the whole frame, not per chunk.
            port.ReadTimeout = (int)Math.Min(remaining, int.MaxValue);
            var read = port.Read(chunk, 0, chunk.Length);
            if (read == 0)
            {
                throw new IOException("The serial port was closed before a complete frame was received.");
            }
            buffer.Append(chunk.AsSpan(0, read));
        }
        return buffer.ToArray();
    }

    private string DescribeOpenFailure(Exception ex) => ex switch
    {
        UnauthorizedAccessException => $"Could not open {PortName} for the POSNET printer: the port is held by another program (e.g. POSNET OPS) or access was denied.",
        OperationCanceledException => $"Opening {PortName} for the POSNET printer was cancelled.",
        _ => $"Could not open the serial port {PortName} for the POSNET printer — is the printer connected and its PC interface set to USB/COM?",
    };

    private static bool IsSerialFailure(Exception ex) =>
        ex is IOException or TimeoutException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or OperationCanceledException;

    private void DropConnection()
    {
        try
        {
            _port?.Dispose();
        }
        catch (Exception ex) when (IsSerialFailure(ex))
        {
            // A USB device that was unplugged can leave the port undisposable; nothing left to release.
        }
        _port = null;
    }

    public void Dispose() => DropConnection();
}
