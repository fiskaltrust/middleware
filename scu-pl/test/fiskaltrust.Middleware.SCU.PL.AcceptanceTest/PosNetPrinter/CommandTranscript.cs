using System.Collections.Concurrent;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// The commands the SCU sent during a test, in order. Both places that can see them fill one:
/// the emulator on the receiving end of the socket, and the recording transport on a hardware run.
/// A test therefore asserts the same protocol flow either way.
/// </summary>
public sealed class CommandTranscript
{
    private readonly ConcurrentQueue<PosNetResponse> _commands = new();

    public IEnumerable<PosNetResponse> Commands => _commands;

    public IEnumerable<string> Mnemonics => _commands.Select(c => c.CommandId);

    /// <summary>
    /// Decodes a command frame with the production codec and appends it. Decoding here rather than
    /// storing raw bytes is what makes a CRC or framing defect in what the SCU sent visible;
    /// the emulator turns the exception into a replay fault, which fails the test with the reason.
    /// </summary>
    /// <exception cref="PosNetProtocolException">The frame is not decodable.</exception>
    public PosNetResponse Add(byte[] frame)
    {
        var command = PosNetFrame.Decode(frame);
        _commands.Enqueue(command);
        return command;
    }
}
