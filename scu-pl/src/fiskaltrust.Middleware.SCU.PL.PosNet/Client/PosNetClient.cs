using System;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Client;

/// <summary>
/// Executes single POSNET commands over the transport, implementing the three-outcome model:
/// a confirmed response is returned, a confirmed error (<c>?nnnn</c>) throws
/// <see cref="PLDeviceErrorException"/>, and an ambiguous outcome propagates as
/// <see cref="PosNetAmbiguousResponseException"/>. There is deliberately no retry at any level —
/// resending after an ambiguous outcome can duplicate fiscal printouts.
/// </summary>
public class PosNetClient : IDisposable
{
    private readonly IPosNetTransport _transport;
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public PosNetClient(IPosNetTransport transport)
    {
        _transport = transport;
    }

    public async Task<PosNetResponse> ExecuteAsync(PosNetCommand command, CancellationToken cancellationToken = default)
    {
        var frame = PosNetFrame.Encode(command);
        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            var responseFrame = await _transport.SendReceiveAsync(frame, cancellationToken);
            var response = PosNetFrame.Decode(responseFrame);
            if (response.IsError)
            {
                var code = response.ErrorCode ?? -1;
                throw new PLDeviceErrorException(code, $"The POSNET printer rejected '{command.Mnemonic}' with error {code}.");
            }
            return response;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public void Dispose()
    {
        _commandLock.Dispose();
        _transport.Dispose();
        GC.SuppressFinalize(this);
    }
}
