using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.Emulator;

/// <summary>
/// An in-process POSNET Online printer speaking the real protocol over real TCP: it decodes
/// frames (CRC-checked by the production codec), tracks the device-side transaction state like a
/// printer does (no line without an open transaction, trend/prncancel close it) and answers with
/// standard confirmations or scripted failures. This lets the acceptance tests exercise the full
/// SCU stack — transport, codec, command flow — without hardware.
/// </summary>
public sealed class PosNetPrinterEmulator : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentDictionary<string, int> _errorOn = new();
    private readonly ConcurrentDictionary<string, byte> _swallowOn = new();
    private Task? _serveLoop;
    private bool _transactionOpen;

    public PosNetPrinterEmulator()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    public ConcurrentQueue<PosNetResponse> ReceivedCommands { get; } = new();

    public IEnumerable<string> ReceivedMnemonics => ReceivedCommands.Select(c => c.CommandId);

    public string DeviceUrl => $"tcp://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    public bool TransactionOpen => _transactionOpen;

    /// <summary>Rejects the given command with a device error code (a confirmed error, ?nnnn).</summary>
    public void ErrorOn(string mnemonic, int errorCode) => _errorOn[mnemonic] = errorCode;

    /// <summary>Reads the given command but never answers — produces the ambiguous outcome.</summary>
    public void SwallowOn(string mnemonic) => _swallowOn[mnemonic] = 1;

    public PosNetPrinterEmulator Start()
    {
        _listener.Start();
        _serveLoop = Task.Run(ServeAsync);
        return this;
    }

    /// <summary>Starts and immediately stops listening — the port then refuses connections.</summary>
    public PosNetPrinterEmulator StartUnreachable()
    {
        _listener.Start();
        _listener.Stop();
        return this;
    }

    private async Task ServeAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using (client)
            {
                try
                {
                    await ServeClientAsync(client);
                }
                catch (IOException)
                {
                    // The SCU dropped the connection (e.g. after a receive timeout) — accept the next one.
                }
                catch (SocketException)
                {
                }
            }
        }
    }

    private async Task ServeClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new List<byte>();
        var chunk = new byte[256];
        while (!_shutdown.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(chunk, _shutdown.Token);
            if (read == 0)
            {
                return;
            }
            buffer.AddRange(chunk.Take(read));

            int etxIndex;
            while ((etxIndex = buffer.IndexOf(PosNetFrame.Etx)) >= 0)
            {
                var frame = buffer.Take(etxIndex + 1).ToArray();
                buffer.RemoveRange(0, etxIndex + 1);
                var response = HandleFrame(frame);
                if (response is not null)
                {
                    await stream.WriteAsync(response, _shutdown.Token);
                }
            }
        }
    }

    private byte[]? HandleFrame(byte[] frame)
    {
        var command = PosNetFrame.Decode(frame);
        ReceivedCommands.Enqueue(command);
        var mnemonic = command.CommandId;

        if (_swallowOn.ContainsKey(mnemonic))
        {
            return null;
        }

        if (_errorOn.TryGetValue(mnemonic, out var scriptedError))
        {
            return EncodeResponse($"{mnemonic}\t?{scriptedError}\t");
        }

        return mnemonic switch
        {
            "scomm" => EncodeResponse($"scomm\tfs1\ttz1\tts{(_transactionOpen ? 16 : 0)}\thr1\t"),
            "trinit" => _transactionOpen
                ? EncodeResponse($"trinit\t?382\t")
                : Confirm(mnemonic, open: true),
            "trline" or "trpayment" => _transactionOpen
                ? Confirm(mnemonic, open: true)
                : EncodeResponse($"{mnemonic}\t?380\t"),
            "trend" => _transactionOpen
                ? Confirm(mnemonic, open: false)
                : EncodeResponse($"trend\t?380\t"),
            "prncancel" => _transactionOpen
                ? Confirm(mnemonic, open: false)
                : EncodeResponse($"prncancel\t?381\t"),
            _ => EncodeResponse($"{mnemonic}\t"),
        };
    }

    private byte[] Confirm(string mnemonic, bool open)
    {
        _transactionOpen = open;
        return EncodeResponse($"{mnemonic}\t");
    }

    private static byte[] EncodeResponse(string payload)
    {
        var payloadBytes = Encoding.ASCII.GetBytes(payload);
        var crc = PosNetCrc16.Compute(payloadBytes);
        return Encoding.ASCII.GetBytes($"\x02{payload}#{crc:X4}\x03");
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _listener.Stop();
        try
        {
            _serveLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
        _shutdown.Dispose();
    }
}
