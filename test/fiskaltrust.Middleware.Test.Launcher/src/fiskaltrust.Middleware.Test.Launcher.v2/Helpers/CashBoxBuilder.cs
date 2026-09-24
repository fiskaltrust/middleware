using System.Net.Mime;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using static fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ChargeItemFactory;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

interface ICashBoxBuilder
{
    string Market { get; }

    void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId);
    void AddMarketQueue(ref PackageConfiguration queueConfiguration, Guid queueId, Guid scuId);
    IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid queueId);
}

class CashBoxBuilder
{
    private readonly PackageConfiguration _queueConfiguration;
    private readonly PackageConfiguration _scuConfiguration;
    private readonly ICashBoxBuilder _cashBoxBuilder;
    public string Market { get => _cashBoxBuilder.Market; }
    public Guid CashBoxId { get; private init; }
    public Guid PosSystemId { get; private init; }
    public Guid QueueId { get; private init; }
    public Guid ScuId { get; private init; }

    /// <summary>
    /// The init tables the cashbox configuration brought, and therefore the ones this builder left
    /// alone. Reported by the host so a configured cashbox is distinguishable from a synthesized one
    /// — the two look identical from the outside, and only one of them is what the file says.
    /// </summary>
    public IReadOnlyCollection<string> ConfiguredTables { get; private init; }

    /// <summary>The init tables this builder had to invent because the configuration had none.</summary>
    public IReadOnlyCollection<string> SynthesizedTables { get; private init; }

    private readonly ChargeItemFactory _chargeItemFactory;
    public ChargeItemBuilder ChargeItem { get => _chargeItemFactory.Builder; }

    /// <param name="cashBoxId">From <c>ftCashBoxId</c>; the queue and SCU ids come from the packages.</param>
    /// <param name="posSystemId">
    /// From <c>ftPosSystemId</c>, the launcher's own addition to the cashbox configuration format —
    /// a pos system belongs to the caller, not to the cashbox.
    /// </param>
    public CashBoxBuilder(ICashBoxBuilder cashBoxBuilder, PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid cashBoxId, Guid posSystemId)
    {
        _cashBoxBuilder = cashBoxBuilder;

        CashBoxId = cashBoxId;
        PosSystemId = posSystemId;

        // An all-zero Id is a package file that pins nothing; a cashbox configuration names both.
        QueueId = queueConfiguration.Id != Guid.Empty ? queueConfiguration.Id : Guid.NewGuid();
        ScuId = scuConfiguration.Id != Guid.Empty ? scuConfiguration.Id : Guid.NewGuid();
        queueConfiguration.Id = QueueId;
        scuConfiguration.Id = ScuId;

        // Whatever the configuration already carries is what the cashbox is; everything below only
        // fills gaps. Taking the keys before the first fill is what makes the two distinguishable.
        ConfiguredTables = queueConfiguration.Configuration.Keys.ToHashSet(StringComparer.Ordinal);

        queueConfiguration.Configuration.AddUnlessConfigured(
            "init_ftCashBox",
            () => new ftCashBox
            {
                ftCashBoxId = CashBoxId
            }
        );
        queueConfiguration.Configuration.AddUnlessConfigured(
            "init_ftQueue",
            () => new List<ftQueue> {
                new ftQueue
                {
                    ftCashBoxId = CashBoxId,
                    ftQueueId = QueueId,
                    Timeout = 15_000,
                    CountryCode = Market
                }
            }
        );

        _cashBoxBuilder.AddSCU(ref queueConfiguration, scuConfiguration, ScuId);
        _cashBoxBuilder.AddMarketQueue(ref queueConfiguration, QueueId, ScuId);

        SynthesizedTables = queueConfiguration.Configuration.Keys.Except(ConfiguredTables, StringComparer.Ordinal).ToList();

        _queueConfiguration = queueConfiguration;
        _scuConfiguration = scuConfiguration;

        _chargeItemFactory = new ChargeItemFactory(Market switch
        {
            "ES" => new Dictionary<ChargeItemCase, decimal>
            {
                [ChargeItemCase.NormalVatRate] = 0.21m,
                [ChargeItemCase.DiscountedVatRate1] = 0.10m,
                [ChargeItemCase.DiscountedVatRate2] = 0.10m,
                [ChargeItemCase.SuperReducedVatRate1] = 0.04m,
                [ChargeItemCase.SuperReducedVatRate2] = 0.04m,
            },
            "PL" => new Dictionary<ChargeItemCase, decimal>
            {
                [ChargeItemCase.NormalVatRate] = 0.23m,
                [ChargeItemCase.DiscountedVatRate1] = 0.08m,
                [ChargeItemCase.DiscountedVatRate2] = 0.05m,
                [ChargeItemCase.ZeroVatRate] = 0.00m,
            },
            _ => throw new NotImplementedException()
        });
    }

    public MiddlewareMethods Build()
    {

        var bootstrapper = _cashBoxBuilder.CreateBootStrapper(new ftCashBoxConfiguration
        {
            ftQueues = [_queueConfiguration]
        }.NewtonsoftJsonWarp()!.ftQueues[0], _scuConfiguration, QueueId);

        return new MiddlewareMethods
        {
            Echo = bootstrapper.RegisterForEcho().JsonWarpingAsync<EchoRequest, EchoResponse>(),
            Sign = bootstrapper.RegisterForSign().JsonWarpingAsync<ReceiptRequest, ReceiptResponse>(),
            Journal = async (JournalRequest request) =>
            {
                var (contentType, pipeReader) = await bootstrapper.RegisterForJournal()(JsonSerializer.Serialize(request));
                return (contentType, pipeReader.AsStream());
            }
        };
    }
}

public record MiddlewareMethods
{
    public required Func<EchoRequest, Task<EchoResponse?>> Echo;
    public required Func<ReceiptRequest, Task<ReceiptResponse?>> Sign;
    public required Func<JournalRequest, Task<(ContentType contentType, Stream response)>> Journal;
}