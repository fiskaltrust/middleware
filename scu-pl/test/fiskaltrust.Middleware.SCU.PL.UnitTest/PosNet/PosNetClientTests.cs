using System.Text;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

/// <summary>
/// What the client guarantees about a single command: the answer it returns belongs to the command it
/// sent, a <c>?nnnn</c> answer is a device error, and nothing is ever resent.
/// </summary>
public class PosNetClientTests
{
    [Fact]
    public async Task Execute_ReturnsTheAnswerToTheCommandItSent()
    {
        using var client = new PosNetClient(new ScriptedTransport("scnt\tbt85\t"));

        var response = await client.ExecuteAsync(PosNetCommands.Scnt());

        response.Parameters["bt"].Should().Be("85");
    }

    /// <summary>
    /// A POSNET answer repeats the mnemonic it answers, and that is the only thing tying the two
    /// together: over serial the late answer to a command that timed out can arrive after the port
    /// was reopened, and reading it as this command's confirmation would shift every answer that
    /// follows — a device error would then be attributed to the wrong command.
    /// </summary>
    [Fact]
    public async Task Execute_WhenTheAnswerBelongsToAnEarlierCommand_IsAmbiguous_NotSuccess()
    {
        // The register is still answering the previous trend when trinit is sent.
        using var client = new PosNetClient(new ScriptedTransport("trend\t"));

        var act = () => client.ExecuteAsync(PosNetCommands.Trinit());

        var failure = (await act.Should().ThrowAsync<PosNetAmbiguousResponseException>()).Which;
        failure.Message.Should().Contain("answered 'trend' to the command 'trinit'");
    }

    /// <summary>An error answer names the command it rejects, so it must not be mistaken for a shift.</summary>
    [Fact]
    public async Task Execute_WhenTheDeviceRejectsTheCommand_IsADeviceError()
    {
        using var client = new PosNetClient(new ScriptedTransport("trline\t?2000\t"));

        var act = () => client.ExecuteAsync(PosNetCommands.Trinit());

        (await act.Should().ThrowAsync<PLDeviceErrorException>()).Which.ErrorCode.Should().Be(2000);
    }

    private sealed class ScriptedTransport(string responsePayload) : IPosNetTransport
    {
        public Task<byte[]> SendReceiveAsync(byte[] frame, CancellationToken cancellationToken = default)
            => Task.FromResult(PosNetProtocolTests.EncodeResponse(responsePayload));

        public void Dispose() { }
    }
}
