using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// market-fr#221: sandbox and production queues sign with a real FR SCU, the in-memory one runs only locally or in
/// development; and a French queue always validates at Error (decision 24b).
/// </summary>
public class StartupGuardTests
{
    private sealed class ScuReporting : IFRSSCD
    {
        private readonly string _body;

        public ScuReporting(string body) => _body = body;

        public int Signed { get; private set; }

        public bool Unreachable { get; set; }

        public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = echoRequest.Message });

        public Task<FRSSCDInfo> GetInfoAsync() => Unreachable
            ? throw new InvalidOperationException("not reachable")
            : Task.FromResult(new FRSSCDInfo { InfoData = $"{{\"CertificationBody\":\"{_body}\"}}" });

        public Task<(ProcessResponse response, string signature)> ProcessReceiptAsync(ProcessRequest request, string? previousSignature)
        {
            Signed++;
            return Task.FromResult((new ProcessResponse { ReceiptResponse = request.ReceiptResponse }, "sig"));
        }

        public Task<FRHashSignature> SignHashAsync(byte[] sha256) => Task.FromResult(new FRHashSignature());
    }

    private static ProcessRequest Request() => new()
    {
        ReceiptRequest = new ReceiptRequest { cbChargeItems = [], cbPayItems = [] },
        ReceiptResponse = new ReceiptResponse { ftState = (State) 0x4652_2000_0000_0000, ftSignatures = [] },
    };

    private static async Task<(ProcessResponse response, string signature)> SignAsync(IFRSSCD scu) => await scu.ProcessReceiptAsync(Request(), null);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TheInMemoryScu_IsRefusedOutsideLocalDevelopment(bool sandbox)
    {
        var inner = new ScuReporting("InMemory");
        var guard = new FRInMemoryScuGuard(inner, FRInMemoryScuGuard.IsLocalDevelopment(new Dictionary<string, object>(), sandbox), NullLogger.Instance);

        var (response, signature) = await SignAsync(guard);

        signature.Should().BeEmpty();
        ((long) response.ReceiptResponse.ftState & 0xFFFF_FFFF).Should().Be(0xEEEE_EEEE, "the receipt comes back unsigned");
        inner.Signed.Should().Be(0);
        (await guard.SignHashAsync([1, 2, 3])).Failure.Should().Contain("in-memory SCU");
        (await guard.GetSigningCertificateAsync()).Failure.Should().Contain("in-memory SCU");
        (await guard.EchoAsync(new EchoRequest { Message = "ping" })).Message.Should().Be("ping", "echo passes through");
    }

    [Fact]
    public async Task TheInMemoryScu_RunsLocally()
    {
        var inner = new ScuReporting("InMemory");
        var guard = new FRInMemoryScuGuard(inner, FRInMemoryScuGuard.IsLocalDevelopment(new Dictionary<string, object> { ["FRLocalDevelopment"] = true }, isSandbox: false), NullLogger.Instance);

        (await SignAsync(guard)).signature.Should().Be("sig");
        inner.Signed.Should().Be(1);
    }

    [Theory]
    [InlineData("true", false, true)]
    [InlineData("True", false, true)]
    [InlineData("true", true, false)]
    [InlineData("false", false, false)]
    [InlineData("yes", false, false)]
    public void LocalDevelopment_IsTheKeySetToTrue_OnAQueueThatIsNotASandbox(string value, bool sandbox, bool expected)
        => FRInMemoryScuGuard.IsLocalDevelopment(new Dictionary<string, object> { ["FRLocalDevelopment"] = value }, sandbox).Should().Be(expected);

    [Theory]
    [InlineData("LNE")]
    [InlineData("Infocert")]
    [InlineData("SelfCertified")]
    public async Task ARealScu_SignsOnAnyQueue(string body)
    {
        var inner = new ScuReporting(body);
        var guard = new FRInMemoryScuGuard(inner, localDevelopment: false, NullLogger.Instance);

        (await SignAsync(guard)).signature.Should().Be("sig");
    }

    [Fact]
    public async Task AnScuNotReachableYet_IsAskedAgain()
    {
        var inner = new ScuReporting("InMemory") { Unreachable = true };
        var guard = new FRInMemoryScuGuard(inner, localDevelopment: false, NullLogger.Instance);

        (await SignAsync(guard)).signature.Should().Be("sig", "the inner SCU fails or signs on its own while it cannot be asked");
        inner.Unreachable = false;
        (await SignAsync(guard)).signature.Should().BeEmpty();
    }

    // ValidationLevel (decision 24b).

    private sealed class CountingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(ValidationLevel.Error, false)]
    [InlineData(ValidationLevel.Warning, true)]
    [InlineData(ValidationLevel.Info, true)]
    public void AFrenchQueue_AlwaysValidatesAtError(ValidationLevel? configured, bool logged)
    {
        var logger = new CountingLogger();

        var effective = QueueFRBootstrapper.ForcedErrorLevel(new ValidationConfiguration { ValidationLevel = configured, ValidationsInSignatures = true }, logger);

        effective.ValidationLevel.Should().Be(ValidationLevel.Error);
        effective.ValidationsInSignatures.Should().BeTrue();
        logger.Messages.Should().HaveCount(logged ? 1 : 0);
    }
}
