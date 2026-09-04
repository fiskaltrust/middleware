using System.Text;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// Converts between a POSNET frame and its payload — the part between STX and the '#'-prefixed
/// checksum. In WINDOWS-1250 like the production codec, so Polish characters survive a round-trip
/// through a cassette intact.
/// </summary>
internal static class PosNetPayload
{
    private static readonly Encoding s_encoding;

    static PosNetPayload()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        s_encoding = Encoding.GetEncoding(1250);
    }

    public static string Of(byte[] frame)
    {
        var hashIndex = Array.LastIndexOf(frame, (byte)'#');
        var end = hashIndex > 0 ? hashIndex : frame.Length - 1;
        return s_encoding.GetString(frame, 1, end - 1);
    }

    /// <summary>Wraps a payload back into a frame, recomputing the checksum the way a device does.</summary>
    public static byte[] ToFrame(string payload)
    {
        var payloadBytes = s_encoding.GetBytes(payload);
        var crc = PosNetCrc16.Compute(payloadBytes);

        var frame = new byte[payloadBytes.Length + 7];
        frame[0] = PosNetFrame.Stx;
        payloadBytes.CopyTo(frame, 1);
        frame[payloadBytes.Length + 1] = (byte)'#';
        Encoding.ASCII.GetBytes(crc.ToString("X4"), 0, 4, frame, payloadBytes.Length + 2);
        frame[^1] = PosNetFrame.Etx;
        return frame;
    }
}
