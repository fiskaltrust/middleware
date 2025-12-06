using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.Test.Launcher.Helpers;

interface ICashBoxBuilder
{
    string Market { get; }

    void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId);
    void AddMarketQueue(PackageConfiguration configuration, Guid queueId, Guid scuId);
    IPOS CreateIPOS();
}

class CashBoxBuilder
{
    private readonly ICashBoxBuilder _cashBoxBuilder;
    public string Market { get => _cashBoxBuilder.Market; }
    public Guid CashBoxId { get; private init; }
    public ulong CountryCode { get => (((ulong) Market[0] << (4 * 2)) + Market[1]) << (8 * 14); }
    public Guid PosSystemId { get; private init; }
    public Guid QueueId { get; private init; }
    public Guid ScuId { get; private init; }

    public CashBoxBuilder(ICashBoxBuilder cashBoxBuilder, PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration)
    {
        _cashBoxBuilder = cashBoxBuilder;

        CashBoxId = Guid.NewGuid();
        PosSystemId = Guid.NewGuid();
        QueueId = Guid.NewGuid();
        ScuId = Guid.NewGuid();

        queueConfiguration.Configuration.Add(
            "init_ftCashBox",
            new ftCashBox
            {
                ftCashBoxId = CashBoxId
            }
        );
        queueConfiguration.Configuration.Add(
            "init_ftQueue",
            new List<ftQueue> {
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
        _cashBoxBuilder.AddMarketQueue(queueConfiguration, QueueId, ScuId);
    }

    public IPOS Build() => _cashBoxBuilder.CreateIPOS();
}