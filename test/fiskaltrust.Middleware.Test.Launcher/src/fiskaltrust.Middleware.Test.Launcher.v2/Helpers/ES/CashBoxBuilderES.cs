
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


class CashBoxBuilderES : ICashBoxBuilder
{
    public string Market { get => "ES"; }

    public PackageConfiguration? _scuConfiguration { get; set; }

    public void AddSCU(ref PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
                $"init_ftSignaturCreationUnitES",
                new List<ftSignaturCreationUnitES> {
                    new ftSignaturCreationUnitES
                    {
                        ftSignaturCreationUnitESId = scuId,
                    }
                }
        );
        _scuConfiguration = scuConfiguration;
    }
    public void AddMarketQueue(ref PackageConfiguration queueConfiguration, Guid queueId, Guid scuId)
    {
        queueConfiguration.Configuration.Add(
                $"init_ftQueueES",
                new List<ftQueueES> {
                    new ftQueueES
                    {
                        ftQueueESId = queueId,
                        CashBoxIdentification = queueId.ToString().Substring(0, 18),
                        ftSignaturCreationUnitESId = scuId
                    }
                }
        );
    }

    public IV2QueueBootstrapper CreateBootStrapper(PackageConfiguration queueConfiguration, PackageConfiguration scuConfiguration, Guid queueId)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        IESSSCD scu = _scuConfiguration!.Package switch
        {
            "fiskaltrust.Middleware.SCU.ES.VeriFactu" => new VeriFactuSCU(new VeriFactuInMemoryClient(), new VeriFactuSCUConfiguration
            {
                Nif = "M0123456Q",
                NombreRazonEmisor = "In Memory"
            }),
            "fiskaltrust.Middleware.SCU.ES.TicketBaiAraba" => new TicketBaiArabaSCU(loggerFactory.CreateLogger<TicketBaiArabaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration)),
            "fiskaltrust.Middleware.SCU.ES.TicketBaiBizkaia" => new TicketBaiBizkaiaSCU(loggerFactory.CreateLogger<TicketBaiBizkaiaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration)),
            "fiskaltrust.Middleware.SCU.ES.TicketBaiGipuzkoa" => new TicketBaiGipuzkoaSCU(loggerFactory.CreateLogger<TicketBaiGipuzkoaSCU>(), TicketBaiSCUConfiguration.FromConfiguration(scuConfiguration.Configuration)),
            _ => throw new NotImplementedException("SCU Type not implemented")
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
