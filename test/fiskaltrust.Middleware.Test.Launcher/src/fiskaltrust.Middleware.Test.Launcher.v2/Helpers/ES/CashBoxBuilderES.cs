
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ES;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

class CashBoxBuilderES : ICashBoxBuilder
{
    public string Market { get => "ES"; }
    public void AddSCU(ref PackageConfiguration configuration, Guid scuId)
    {
        configuration.Configuration.Add(
                $"init_ftSignaturCreationUnitES",
                new List<ftSignaturCreationUnitES> {
                    new ftSignaturCreationUnitES
                    {
                        ftSignaturCreationUnitESId = scuId,
                    }
                }
        );
    }
    public void AddMarketQueue(ref PackageConfiguration configuration, Guid queueId, Guid scuId)
    {
        configuration.Configuration.Add(
                $"init_ftQueueES",
                new List<ftQueueES> {
                    new ftQueueES
                    {
                        ftQueueESId = queueId,
                        CashBoxIdentification = queueId.ToString(),
                        ftSignaturCreationUnitESId = scuId
                    }
                }
        );
    }

    public IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration configuration, Guid queueId)
    {
        var loggerFactory = new LoggerFactory();

        var clientFactory = new InMemoryClientFactory<IESSSCD>(new ESSSCDJsonWarper(new VeriFactuSCU(new VeriFactuInMemoryClient(), new VeriFactuSCUConfiguration
        {
            Nif = "M0123456Q",
            NombreRazonEmisor = "In Memory"
        })));

        return new Localization.QueueES.QueueESBootstrapper(
            queueId,
            loggerFactory,
            clientFactory,
            configuration.Configuration,
            new InMemoryStorageProvider(loggerFactory, queueId, configuration.Configuration));
    }
}
