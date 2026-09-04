using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.Emulator;

/// <summary>
/// An in-process POSNET Online printer speaking the real protocol over real TCP, so the acceptance
/// tests exercise the full SCU stack — transport, framing, codec, command flow — without hardware.
/// It answers either from a <see cref="Cassette"/> recorded off a real device, or, where no
/// recording can exist, from a small hand-written model of the device: it tracks the transaction
/// state like a printer does (no line without an open transaction, trend/prncancel close it) and
/// can be scripted to fail.
/// </summary>
public sealed class PosNetPrinterEmulator : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentDictionary<string, int> _errorOn = new();
    private readonly ConcurrentDictionary<string, byte> _swallowOn = new();
    private readonly ConcurrentQueue<CassetteExchange> _replay = new();
    private readonly ConcurrentQueue<string> _replayFaults = new();
    private readonly bool _replaysACassette;
    private int _replayPosition;
    private Task? _serveLoop;
    private bool _transactionOpen;
    private int _completedReceipts = 84;
    private string? _eDocumentBufferRecord;

    /// <summary>
    /// The unique eDokument id (<c>ha</c>) the model assigns to the next eparagonidznext binding —
    /// configurable so a test can assert the value all the way through to the ReceiptResponse.
    /// </summary>
    public uint NextEDocumentId { get; set; } = 3054;

    public PosNetPrinterEmulator() { }

    private PosNetPrinterEmulator(Cassette cassette)
    {
        _replaysACassette = true;
        foreach (var exchange in cassette.Exchanges)
        {
            _replay.Enqueue(exchange);
        }
    }

    /// <summary>Replays a conversation recorded off a real printer.</summary>
    public static PosNetPrinterEmulator Replaying(Cassette cassette) => new(cassette);

    public CommandTranscript Transcript { get; } = new();

    /// <summary>
    /// Where the conversation left the recording: a command the cassette did not record, or one it
    /// recorded differently. Replay is only evidence for as long as the commands still match, so a
    /// mismatch is a test failure rather than something to improvise around.
    /// </summary>
    public IEnumerable<string> ReplayFaults => _replayFaults;

    public string DeviceUrl => $"tcp://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    public bool TransactionOpen => _transactionOpen;

    /// <summary>Rejects the given command with a device error code (a confirmed error, ?nnnn).</summary>
    public PosNetPrinterEmulator ErrorOn(string mnemonic, int errorCode)
    {
        _errorOn[mnemonic] = errorCode;
        return this;
    }

    /// <summary>Reads the given command but never answers — produces the ambiguous outcome.</summary>
    public PosNetPrinterEmulator SwallowOn(string mnemonic)
    {
        _swallowOn[mnemonic] = 1;
        return this;
    }

    /// <summary>
    /// Scripts the eDokument buffer record eparagonbufferget answers with (the parameters after the
    /// mnemonic, e.g. <c>"hd3054\tprN\tst1\t"</c>) — real delivery states can only be scripted, the
    /// model has no hub to deliver to.
    /// </summary>
    public PosNetPrinterEmulator WithEDocumentBufferRecord(string parameters)
    {
        _eDocumentBufferRecord = parameters;
        return this;
    }

    /// <summary>
    /// Confirms eparagonidznext WITHOUT the promised <c>ha</c> — the degenerate answer behind
    /// middleware#766's cleanup path: the binding is armed on the device but untrackable.
    /// </summary>
    public PosNetPrinterEmulator OmittingEDocumentIdOnBind()
    {
        _omitEDocumentIdOnBind = true;
        return this;
    }

    private bool _omitEDocumentIdOnBind;

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
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
            {
                // The listener was shut down — stop accepting.
                return;
            }

            using (client)
            {
                try
                {
                    await ServeClientAsync(client);
                }
                catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
                {
                    // The SCU dropped the connection (e.g. after a receive timeout) — accept the next one.
                }
            }
        }
    }

    private async Task ServeClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var reader = new PosNetFrameReader();
        var chunk = new byte[256];
        while (!_shutdown.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(chunk, _shutdown.Token);
            if (read == 0)
            {
                return;
            }

            foreach (var frame in reader.Append(chunk, read))
            {
                var response = Answer(frame);
                if (response is not null)
                {
                    await stream.WriteAsync(response, _shutdown.Token);
                }
            }
        }
    }

    /// <summary>Answers a command the way a printer would, or returns null to stay silent.</summary>
    private byte[]? Answer(byte[] frame)
    {
        string mnemonic;
        try
        {
            mnemonic = Transcript.Add(frame).CommandId;
        }
        catch (PosNetProtocolException ex)
        {
            // A frame the production codec cannot decode is a defect in what the SCU sent, and the
            // test has to see it. Letting it escape would not achieve that: this runs in a
            // fire-and-forget accept task, so the emulator would stop serving, the exception would
            // go unobserved, and the test would fail on a receive timeout instead. Noting it and
            // answering with a device error fails the test on the spot, with the reason.
            return Fault($"the SCU sent a frame the protocol codec rejected: {ex.Message}");
        }

        // Scripted failures win over anything recorded: they exist precisely for the outcomes a
        // real device cannot be asked to produce.
        if (_swallowOn.ContainsKey(mnemonic))
        {
            return null;
        }

        if (_errorOn.TryGetValue(mnemonic, out var scriptedError))
        {
            return PosNetPayload.ToFrame($"{mnemonic}\t?{scriptedError}\t");
        }

        if (_replay.TryDequeue(out var recorded))
        {
            return Replay(recorded, PosNetPayload.Of(frame));
        }

        if (_replaysACassette)
        {
            // Improvising past the end of a recording would answer for a device that was never
            // asked — the remaining flow would be asserted against the hand-written model instead.
            return Fault($"the cassette holds {_replayPosition} exchange(s) and the SCU sent a further command '{PosNetPayload.Of(frame)}'. Re-record the cassette against a printer.");
        }

        return Improvise(mnemonic);
    }

    /// <summary>
    /// Answers from the recording, but only for the command that was recorded: replay is evidence
    /// of what a real printer did in reply to <em>that</em> command.
    /// </summary>
    private byte[]? Replay(CassetteExchange exchange, string sentPayload)
    {
        _replayPosition++;
        if (!string.Equals(exchange.Command, sentPayload, StringComparison.Ordinal))
        {
            return Fault($"exchange #{_replayPosition} of the cassette recorded the command '{exchange.Command}' but the SCU sent '{sentPayload}'.");
        }

        // A recorded silence stays silent — that is what the device did.
        return exchange.Response is null ? null : PosNetPayload.ToFrame(exchange.Response);
    }

    /// <summary>
    /// Notes a replay fault and answers with a device error, so the test fails on the spot instead
    /// of waiting out a receive timeout. <see cref="PosNetPrinter.PosNetTestTarget"/> surfaces the
    /// note itself — the error code alone would not say what went wrong.
    /// </summary>
    private byte[] Fault(string description)
    {
        _replayFaults.Enqueue(description);
        return PosNetPayload.ToFrame("ERR\t?9999\t");
    }

    /// <summary>
    /// The hand-written device model, used when no cassette covers the command. It is a model, not
    /// a printer — it can only ever confirm what the protocol demands, never what a specific device
    /// really answers.
    /// </summary>
    private byte[] Improvise(string mnemonic) => mnemonic switch
    {
        // T/N flags and the numer unikatowy in the shape a POSNET Online printer answers them (see
        // the recorded cassettes) — but fiscalized, which a test device is not.
        "scomm" => PosNetPayload.ToFrame($"scomm\tfsT\ttzN\tts{(_transactionOpen ? 16 : 0)}\thrT\tnuZBF 2101002392\ttdN\t"),
        "scnt" => PosNetPayload.ToFrame($"scnt\trd12\tbn{_completedReceipts}\tbt{_completedReceipts}\tfn3\t"),
        "trinit" => _transactionOpen
            ? PosNetPayload.ToFrame($"trinit\t?382\t")
            : Confirm(mnemonic, open: true),
        "trline" or "trpayment" or "trnipset" => _transactionOpen
            ? Confirm(mnemonic, open: true)
            : PosNetPayload.ToFrame($"{mnemonic}\t?380\t"),
        "trend" => _transactionOpen
            ? CompleteReceipt()
            : PosNetPayload.ToFrame($"trend\t?380\t"),
        "prncancel" => _transactionOpen
            ? Confirm(mnemonic, open: false)
            : PosNetPayload.ToFrame($"prncancel\t?381\t"),
        // The e-paragon binding answers with the unique eDokument id (ha); the buffer readback
        // answers a scripted record, defaulting to a delivered electronic document (prN = no
        // paper). The model is fiscalized (fsT above), so the binding succeeds unless a test
        // scripts ?2034 explicitly.
        "eparagonidznext" => _omitEDocumentIdOnBind
            ? PosNetPayload.ToFrame("eparagonidznext\t")
            : PosNetPayload.ToFrame($"eparagonidznext\tha{NextEDocumentId}\t"),
        "eparagonbufferget" => PosNetPayload.ToFrame($"eparagonbufferget\t{_eDocumentBufferRecord ?? $"hd{NextEDocumentId}\tprN\tst1\t"}"),
        _ => PosNetPayload.ToFrame($"{mnemonic}\t"),
    };

    private byte[] Confirm(string mnemonic, bool open)
    {
        _transactionOpen = open;
        return PosNetPayload.ToFrame($"{mnemonic}\t");
    }

    private byte[] CompleteReceipt()
    {
        _completedReceipts++;
        return Confirm("trend", open: false);
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
