using System;
using System.Threading;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// Sends one encoded frame and returns the device's response frame. Implementations must throw
/// <see cref="Abstraction.Exceptions.PLDeviceUnreachableException"/> when the frame could not be
/// delivered, and <see cref="PosNetAmbiguousResponseException"/> when the frame was (possibly)
/// delivered but no response arrived — the two cases differ legally: an undelivered command is
/// safe to send again, an unanswered one is not.
/// </summary>
public interface IPosNetTransport : IDisposable
{
    Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default);
}
