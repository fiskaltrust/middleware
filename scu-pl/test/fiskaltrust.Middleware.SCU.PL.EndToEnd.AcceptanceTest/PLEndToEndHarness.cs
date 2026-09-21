using System.Runtime.CompilerServices;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePL;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.TestSupport;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.PL.EndToEnd.AcceptanceTest;

/// <summary>
/// The whole Polish path in one object: a PL queue on in-memory storage, wired to the real PosNet SCU,
/// which talks to whatever <see cref="PosNetTestTarget"/> selects — the emulator replaying this
/// test's cassette, or the printer named by <c>SCU_PL_POSNET_DEVICE_URL</c>. Requests go in as the
/// JSON a POS would send and come back as the JSON the middleware answers, so serialization is part
/// of what is exercised.
/// </summary>
/// <remarks>
/// The register's fiscalization state is read for real: a PL queue only forwards receipts once it has
/// activated against a fiscalized register, and every committed cassette — like the office printer
/// they were recorded on — reports <c>fsT</c>, so the queue's own gate is what the suite exercises.
/// Lifting it with <see cref="AssumeFiscalizedPLSSCD"/> would make a regression in reading that state
/// invisible end to end. A run against a printer that is not fiscalized yet can still lift the gate
/// by setting <see cref="AssumeFiscalizedVariable"/>, the same switch the launcher offers.
/// The queue's market rows are the ones the launcher's CashBoxBuilderPL starts its PL queue with: a
/// queue row pointing at an SCU row that exists.
/// </remarks>
public sealed class PLEndToEndHarness : IDisposable
{
    private readonly Func<string, Task<string>> _sign;
    private readonly IStorageProvider _storage;
    private readonly ILoggerFactory _loggerFactory;

    public PLEndToEndHarness(bool startActive = true, [CallerMemberName] string cassetteName = "")
    {
        CashBoxId = Guid.NewGuid();
        QueueId = Guid.NewGuid();
        ScuId = Guid.NewGuid();
        PosSystemId = Guid.NewGuid();
        Target = PosNetTestTarget.Open(TestProject.Cassettes, cassetteName);
        PtuSlots = new PtuSlotResolver(Target.Configuration.VatRateTable);

        var configuration = new Dictionary<string, object>
        {
            ["cashboxid"] = CashBoxId,
            ["init_ftCashBox"] = JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = CashBoxId, TimeStamp = DateTime.UtcNow.Ticks }),
            ["init_ftQueue"] = JsonSerializer.Serialize(new List<ftQueue>
            {
                new()
                {
                    ftQueueId = QueueId,
                    ftCashBoxId = CashBoxId,
                    // An active queue forwards receipts right away; an inactive one needs the initial operation first.
                    StartMoment = startActive ? DateTime.UtcNow : null,
                    CountryCode = "PL",
                },
            }),
            ["init_ftQueuePL"] = JsonSerializer.Serialize(new List<ftQueuePL>
            {
                new()
                {
                    ftQueuePLId = QueueId,
                    CashBoxIdentification = QueueId.ToString().Substring(0, 18),
                    ftSignaturCreationUnitPLId = ScuId,
                },
            }),
            ["init_ftSignaturCreationUnitPL"] = JsonSerializer.Serialize(new List<ftSignaturCreationUnitPL>
            {
                new() { ftSignaturCreationUnitPLId = ScuId },
            }),
            ["init_masterData"] = JsonSerializer.Serialize(new MasterDataConfiguration
            {
                Account = new AccountMasterData
                {
                    AccountId = Guid.NewGuid(),
                    AccountName = "fiskaltrust sp. z o.o.",
                    VatId = "5260250274",
                    Street = "ul. Przykładowa 1",
                    Zip = "00-001",
                    City = "Warszawa",
                    Country = "PL",
                    TaxId = "5260250274",
                },
            }),
        };

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _storage = new InMemoryStorageProvider(_loggerFactory, QueueId, configuration);
        var sut = AssumeFiscalized ? new AssumeFiscalizedPLSSCD(Target.Sut) : Target.Sut;
        var bootstrapper = new QueuePLBootstrapper(QueueId, _loggerFactory, configuration, sut, _storage);
        _sign = bootstrapper.RegisterForSign();
    }

    /// <summary>Lifts the queue's fiscalization gate, for a run against a register that is not fiscalized yet.</summary>
    public const string AssumeFiscalizedVariable = "MW_PL_ASSUME_FISCALIZED";

    private static bool AssumeFiscalized =>
        Environment.GetEnvironmentVariable(AssumeFiscalizedVariable) is { Length: > 0 } value
            && !value.Equals("0", StringComparison.Ordinal)
            && !value.Equals("false", StringComparison.OrdinalIgnoreCase);

    public Guid CashBoxId { get; }

    public Guid QueueId { get; }

    public Guid ScuId { get; }

    public Guid PosSystemId { get; }

    /// <summary>The device end: emulator or printer, the SCU's transcript and the read-back probe.</summary>
    public PosNetTestTarget Target { get; }

    public PosNetDeviceProbe Probe => Target.Probe;

    /// <summary>The commands the SCU sent to the register, without the probe's read-backs.</summary>
    public IEnumerable<string> SentMnemonics => Target.SentMnemonics;

    public IEnumerable<PosNetResponse> SentCommands => Target.SentCommands;

    /// <summary>The PTU slots as the SCU resolves them — the footprint has to use the same table.</summary>
    public PtuSlotResolver PtuSlots { get; }

    /// <summary>
    /// The committed business case with this harness's cashbox and pos system filled in — resolved by
    /// <see cref="BusinessCaseSample"/>, which the launcher uses as well, so a case that passes here
    /// is served unchanged there.
    /// </summary>
    public string Prepare(string rawJson) => BusinessCaseSample.Resolve(rawJson, CashBoxId, PosSystemId);

    /// <summary>Signs the request the way a POS would: JSON in, JSON out.</summary>
    public async Task<ReceiptResponse> SignAsync(string rawJson) => await SignPreparedAsync(Prepare(rawJson));

    /// <summary>
    /// Signs the request and reads the register back around it: counters and totalizers before,
    /// the transaction status and the counters after. The read-backs are explicit and in a fixed
    /// place so that a recorded cassette holds them in a reproducible order.
    /// </summary>
    public async Task<VerifiedReceipt> SignAndVerifyAsync(string rawJson)
    {
        var preparedJson = Prepare(rawJson);
        var request = JsonSerializer.Deserialize<ReceiptRequest>(preparedJson)
            ?? throw new InvalidOperationException("The business case is not a ReceiptRequest.");

        var before = await Probe.SnapshotAsync();
        var response = await SignPreparedAsync(preparedJson);
        var transaction = await Probe.ReadTransactionAsync();
        var after = await Probe.SnapshotAsync();

        var expected = FiscalFootprint.Of(request, PtuSlots);
        var discrepancies = FootprintComparer.Compare(expected, transaction, before, after, FiscalDocumentNumber.Of(response));
        return new VerifiedReceipt(request, response, expected, discrepancies);
    }

    private async Task<ReceiptResponse> SignPreparedAsync(string preparedJson)
    {
        var responseJson = await _sign(preparedJson);
        return JsonSerializer.Deserialize<ReceiptResponse>(responseJson)
            ?? throw new InvalidOperationException("The queue answered with JSON that is not a ReceiptResponse.");
    }

    /// <summary>Everything the queue persisted — request and response of each receipt, in order.</summary>
    public async Task<IReadOnlyList<ftQueueItem>> QueueItemsAsync()
    {
        IMiddlewareQueueItemRepository repository = await _storage.CreateMiddlewareQueueItemRepository();
        return (await repository.GetAsync()).OrderBy(x => x.ftQueueRow).ToList();
    }

    public void Dispose()
    {
        Target.Dispose();
        _loggerFactory.Dispose();
    }
}

/// <summary>A signed receipt together with what the register recorded for it.</summary>
/// <param name="Request">The request as it went into the queue.</param>
/// <param name="Response">The middleware's answer.</param>
/// <param name="Expected">What the request should have left on the register.</param>
/// <param name="Discrepancies">Where the register's account differs from that — empty when it does not.</param>
public sealed record VerifiedReceipt(ReceiptRequest Request, ReceiptResponse Response, FiscalFootprint Expected, IReadOnlyList<string> Discrepancies);
