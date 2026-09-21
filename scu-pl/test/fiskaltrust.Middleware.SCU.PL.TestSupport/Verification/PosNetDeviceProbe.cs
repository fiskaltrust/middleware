using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;

/// <summary>
/// Reads the register's own account of what it did — its status and counter commands — so a test
/// can hold what the printer recorded against what the SCU sent. It uses the SCU's
/// <see cref="PosNetClient"/>, i.e. the same TCP connection and the same one-command-at-a-time
/// discipline, which is what makes the read-backs part of the recorded conversation and lets the
/// emulator replay them in order. All commands it sends are available in read-only mode and change
/// nothing on the device.
/// </summary>
public sealed class PosNetDeviceProbe(PosNetClient client, CommandTranscript transcript)
{
    /// <summary>Executes a read-only status command and returns the decoded answer.</summary>
    public async Task<PosNetResponse> ExecuteAsync(string mnemonic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mnemonic);
        using (transcript.Probing())
        {
            return await client.ExecuteAsync(new PosNetCommand(mnemonic));
        }
    }

    /// <summary>The counters and totalizers right now (<c>stot</c>, then <c>scnt</c>).</summary>
    public async Task<FiscalSnapshot> SnapshotAsync()
    {
        var totalizers = await ExecuteAsync("stot");
        var counters = await ExecuteAsync("scnt");
        return FiscalSnapshot.From(totalizers, counters);
    }

    /// <summary>The transaction status (<c>strns</c>) — after a receipt, that receipt's values.</summary>
    public async Task<TransactionReading> ReadTransactionAsync()
        => TransactionReading.From(await ExecuteAsync("strns"));
}
