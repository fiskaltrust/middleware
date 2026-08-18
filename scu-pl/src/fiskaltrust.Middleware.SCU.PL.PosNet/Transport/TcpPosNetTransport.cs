using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// TCP transport to a POSNET printer with a persistent connection (reconnected lazily after a
/// failure, and whenever the kept socket is no longer usable — see <see cref="IsUsable"/>) and
/// explicit connect/send/receive timeouts. The transaction state lives in the
/// device, not in the connection, so reconnecting between commands is safe. Failures before the
/// frame is fully written surface as <see cref="PLDeviceUnreachableException"/> (safe to resend);
/// failures after it surface as <see cref="PosNetAmbiguousResponseException"/> (must not resend).
/// </summary>
public sealed class TcpPosNetTransport : IPosNetTransport
{
    private readonly string _host;
    private readonly int _port;
    private readonly PosNetConfiguration _configuration;
    private TcpClient? _client;

    public TcpPosNetTransport(PosNetConfiguration configuration)
    {
        _configuration = configuration;
        (_host, _port) = configuration.ParseDeviceEndpoint();
    }

    public async Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        NetworkStream stream;
        try
        {
            stream = await GetConnectedStreamAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            DropConnection();
            throw new PLDeviceUnreachableException($"Could not connect to the POSNET printer at {_host}:{_port}.", ex);
        }

        try
        {
            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sendCts.CancelAfter(_configuration.SendTimeoutMs);
            await stream.WriteAsync(frame, sendCts.Token);
            await stream.FlushAsync(sendCts.Token);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            // The frame may have been partially written; whether the device saw a complete
            // command is unknown, so this is already the ambiguous case.
            DropConnection();
            throw new PosNetAmbiguousResponseException($"Sending a command to the POSNET printer at {_host}:{_port} was interrupted — the device state is unknown. Verify the device before retrying.", ex);
        }

        try
        {
            using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            receiveCts.CancelAfter(_configuration.ReceiveTimeoutMs);
            return await ReadFrameAsync(stream, receiveCts.Token);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            DropConnection();
            throw new PosNetAmbiguousResponseException($"The POSNET printer at {_host}:{_port} did not answer within {_configuration.ReceiveTimeoutMs} ms — the command may or may not have been executed. Verify the device before retrying.", ex);
        }
    }

    private async Task<NetworkStream> GetConnectedStreamAsync(CancellationToken cancellationToken)
    {
        if (IsUsable(_client))
        {
            return _client!.GetStream();
        }

        DropConnection();
        var client = new TcpClient();
        try
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(_configuration.ConnectTimeoutMs);
            await client.ConnectAsync(_host, _port, connectCts.Token);
        }
        catch
        {
            client.Dispose();
            throw;
        }
        _client = client;
        return client.GetStream();
    }

    /// <summary>
    /// Whether the kept connection can still carry a command. <see cref="TcpClient.Connected"/>
    /// only reflects the last I/O operation, so a printer that closed the connection while the till
    /// was idle still looks connected — writing to it fails, and that failure would be reported as
    /// an ambiguous outcome ("verify the device") although nothing was ever delivered. A socket the
    /// peer has closed becomes readable with nothing to read, which is what this checks for.
    /// </summary>
    private static bool IsUsable(TcpClient? client)
    {
        if (client is not { Connected: true })
        {
            return false;
        }

        try
        {
            return !(client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            return false;
        }
    }

    private static async Task<byte[]> ReadFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        // Frames end with ETX (0x03); payload characters start at 0x20, so scanning for the
        // terminator cannot hit content bytes.
        using var buffer = new MemoryStream();
        var chunk = new byte[256];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                throw new IOException("The connection was closed before a complete frame was received.");
            }
            buffer.Write(chunk, 0, read);
            if (Array.IndexOf(chunk, PosNetFrame.Etx, 0, read) >= 0)
            {
                return buffer.ToArray();
            }
        }
    }

    private void DropConnection()
    {
        _client?.Dispose();
        _client = null;
    }

    public void Dispose() => DropConnection();
}
