using System.Collections.Concurrent;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;

/// <summary>
/// The commands the SCU sent during a test, in order. Both places that can see them fill one:
/// the emulator on the receiving end of the socket, and the recording transport on a hardware run.
/// A test therefore asserts the same protocol flow either way.
/// </summary>
/// <remarks>
/// A test may read the device back through a <see cref="Verification.PosNetDeviceProbe"/>; those
/// commands travel over the same connection and are recorded alongside, but they are not what the
/// SCU sent. They are tagged while a <see cref="Probing"/> scope is open and kept out of
/// <see cref="Commands"/>, so an assertion on the SCU's command sequence is unaffected by how much a
/// test chose to read back. The tagging is sound because a probe awaits its answer inside the scope
/// and the SCU never runs concurrently with it in a test.
/// </remarks>
public sealed class CommandTranscript
{
    private readonly ConcurrentQueue<Entry> _entries = new();
    private int _probeDepth;

    /// <summary>What the SCU sent — without the probe's own read-backs.</summary>
    public IEnumerable<PosNetResponse> Commands => _entries.Where(e => !e.IsProbe).Select(e => e.Command);

    public IEnumerable<string> Mnemonics => Commands.Select(c => c.CommandId);

    /// <summary>Marks the commands added until disposal as read-backs of the test rather than of the SCU.</summary>
    public IDisposable Probing()
    {
        Interlocked.Increment(ref _probeDepth);
        return new ProbeScope(this);
    }

    /// <summary>
    /// Decodes a command frame with the production codec and appends it. Decoding here rather than
    /// storing raw bytes is what makes a CRC or framing defect in what the SCU sent visible;
    /// the emulator turns the exception into a replay fault, which fails the test with the reason.
    /// </summary>
    /// <exception cref="PosNetProtocolException">The frame is not decodable.</exception>
    public PosNetResponse Add(byte[] frame)
    {
        var command = PosNetFrame.Decode(frame);
        _entries.Enqueue(new Entry(command, Volatile.Read(ref _probeDepth) > 0));
        return command;
    }

    private sealed record Entry(PosNetResponse Command, bool IsProbe);

    private sealed class ProbeScope(CommandTranscript transcript) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Interlocked.Decrement(ref transcript._probeDepth);
            }
        }
    }
}
