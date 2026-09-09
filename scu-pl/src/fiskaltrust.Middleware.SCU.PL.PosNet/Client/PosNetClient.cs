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
            RequireAnswerTo(command, response);
            return response;
        }
        finally
        {
            _commandLock.Release();
        }
    }

    /// <summary>
    /// A POSNET answer repeats the mnemonic of the command it answers, and that is the only thing
    /// tying the two together on the wire. It is checked because a frame can outlive the command it
    /// belongs to: over serial, the late answer to a command that timed out can arrive after the
    /// port was reopened and would otherwise be read as this command's confirmation — with every
    /// following answer shifted by one, so a device error would be attributed to the wrong command.
    /// The outcome is ambiguous rather than a device error: this command was written, and what the
    /// register did with it is exactly what the answer does not say.
    /// </summary>
    private static void RequireAnswerTo(PosNetCommand command, PosNetResponse response)
    {
        if (!string.Equals(response.CommandId, command.Mnemonic, StringComparison.Ordinal))
        {
            throw new PosNetAmbiguousResponseException(
                $"The POSNET printer answered '{response.CommandId}' to the command '{command.Mnemonic}' — the answer belongs to an earlier command, so the outcome of this one is unknown. Verify the device before retrying.");
        }
    }

    public void Dispose()
    {
        _commandLock.Dispose();
        _transport.Dispose();
        GC.SuppressFinalize(this);
    }
}
