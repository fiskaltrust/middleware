using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest.PosNetPrinter;

/// <summary>
/// Wraps the real transport on a hardware run: it keeps the transcript a test asserts on — a
/// printer offers no introspection — and writes down what the device answered, so the same
/// conversation can be replayed later without the device. Every exchange passes through untouched.
/// </summary>
public sealed class RecordingPosNetTransport(IPosNetTransport inner, string printerDeviceUrl) : IPosNetTransport
{
    public CommandTranscript Transcript { get; } = new();

    public Cassette Cassette { get; } = new() { RecordedAgainst = printerDeviceUrl, RecordedAtUtc = DateTime.UtcNow };

    public async Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
    {
        Transcript.Add(frame);
        try
        {
            var response = await inner.SendReceiveAsync(frame, cancellationToken);
            Cassette.Exchanges.Add(new CassetteExchange(PosNetPayload.Of(frame), PosNetPayload.Of(response)));
            return response;
        }
        catch (PosNetAmbiguousResponseException)
        {
            // The device stayed silent. Recorded as such: a replay must reproduce the silence, not
            // invent an answer the printer never gave.
            Cassette.Exchanges.Add(new CassetteExchange(PosNetPayload.Of(frame), null));
            throw;
        }
    }


    public void Dispose() => inner.Dispose();
}
