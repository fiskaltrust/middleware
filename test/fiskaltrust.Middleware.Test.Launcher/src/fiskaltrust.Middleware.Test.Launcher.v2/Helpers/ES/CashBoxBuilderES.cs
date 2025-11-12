
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;
using fiskaltrust.Middleware.SCU.ES.TicketBAIBizkaia;
using fiskaltrust.Middleware.SCU.ES.TicketBAIGipuzkoa;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ES;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

enum SCUTypesES
{
    VeriFactu,
    TicketBAIAraba,
    TicketBAIBizkaia,
    TicketBAIGipuzkoa
}

class CashBoxBuilderES : ICashBoxBuilder
{
    public CashBoxBuilderES(SCUTypesES scuType)
    {
        SCUType = scuType;
    }

    public string Market { get => "ES"; }

    public SCUTypesES SCUType { get; init; }

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

    public IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid queueId)
    {
        var loggerFactory = new LoggerFactory();

        IESSSCD scu = SCUType switch
        {
            SCUTypesES.VeriFactu => new VeriFactuSCU(new VeriFactuInMemoryClient(), new VeriFactuSCUConfiguration
            {
                Nif = "M0123456Q",
                NombreRazonEmisor = "In Memory"
            }),
            SCUTypesES.TicketBAIAraba => new TicketBaiArabaSCU(loggerFactory.CreateLogger<TicketBaiArabaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration)),
            SCUTypesES.TicketBAIBizkaia => new TicketBaiBizkaiaSCU(loggerFactory.CreateLogger<TicketBaiBizkaiaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration)),
            SCUTypesES.TicketBAIGipuzkoa => new TicketBaiGipuzkoaSCU(loggerFactory.CreateLogger<TicketBaiGipuzkoaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration))
        };

        var clientFactory = new InMemoryClientFactory<IESSSCD>(new ESSSCDJsonWarper(scu));

        return new Localization.QueueES.QueueESBootstrapper(
            queueId,
            loggerFactory,
            clientFactory,
            queueConfiguration.Configuration,
            new InMemoryStorageProvider(loggerFactory, queueId, queueConfiguration.Configuration));
    }
}
