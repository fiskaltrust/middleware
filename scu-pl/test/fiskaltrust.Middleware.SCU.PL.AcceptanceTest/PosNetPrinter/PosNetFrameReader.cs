using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// Reassembles POSNET frames from a TCP stream: TCP delivers bytes, not messages, so a read can
/// carry half a frame or several. Frames end with ETX and payload characters start at 0x20, so
/// scanning for the terminator cannot hit content — the same reasoning the production transport uses.
/// </summary>
internal sealed class PosNetFrameReader
{
    private readonly List<byte> _buffer = [];

    /// <summary>Adds freshly read bytes and returns the frames they completed, in order.</summary>
    /// <param name="chunk">The read buffer.</param>
    /// <param name="count">How many bytes of <paramref name="chunk"/> were actually read.</param>
    public List<byte[]> Append(byte[] chunk, int count)
    {
        _buffer.AddRange(chunk.Take(count));

        var frames = new List<byte[]>();
        int etxIndex;
        while ((etxIndex = _buffer.IndexOf(PosNetFrame.Etx)) >= 0)
        {
            frames.Add(_buffer.Take(etxIndex + 1).ToArray());
            _buffer.RemoveRange(0, etxIndex + 1);
        }
        return frames;
    }
}
