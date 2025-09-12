using System.IO.Pipelines;
using System.Net.Mime;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.Test.Launcher.v2.Extensions;
using fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ES;
using fiskaltrust.storage.serialization.V0;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using static fiskaltrust.Middleware.Test.Launcher.v2.Helpers.ChargeItemFactory;

namespace fiskaltrust.Middleware.Test.Launcher.v2.Helpers;

class CashBoxBuilder
{
    private PackageConfiguration _configuration { get; set; }
    public string Market { get; private init; }
    public Guid CashBoxId { get; private init; }
    public Guid QueueId { get; private init; }
    public Guid ScuId { get; private init; }

    private readonly ChargeItemFactory _chargeItemFactory;
    public ChargeItemBuilder ChargeItem { get => _chargeItemFactory.Builder; }

    public CashBoxBuilder(string market)
    {
        Market = market;
        CashBoxId = Guid.NewGuid();
        QueueId = Guid.NewGuid();
        ScuId = Guid.NewGuid();

        _configuration = new();
        _configuration.Id = QueueId;
        _configuration.Configuration = new()
        {
            {
                "init_ftCashBox",
                new ftCashBox
                {
                    ftCashBoxId = CashBoxId
                }
            },
            {
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
            },
            {
                $"init_ftQueue{Market}",
                Market switch
                {
                    "ES" => new List<ftQueueES> {
                        new ftQueueES
                        {
                            ftQueueESId = QueueId,
                            CashBoxIdentification = QueueId.ToString(),
                            ftSignaturCreationUnitESId = ScuId
                        }
                    },
                    _ => throw new NotImplementedException()
                }
            },
            {
                $"init_ftSignaturCreationUnit{Market}",
                Market switch
                {
                    "ES" => new List<ftSignaturCreationUnitES> {
                        new ftSignaturCreationUnitES
                        {
                            ftSignaturCreationUnitESId = ScuId,
                        }
                    },
                    _ => throw new NotImplementedException()
                }
            }
        };

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
            _ => throw new NotImplementedException()
        });
    }

    public (Func<EchoRequest, Task<EchoResponse?>> echo, Func<ReceiptRequest, Task<ReceiptResponse?>> sign, Func<JournalRequest, Task<(ContentType contentType, Stream response)>> journal) Build()
    {
        var loggerFactory = new LoggerFactory();

        var configuration = new ftCashBoxConfiguration(CashBoxId)
        {
            ftQueues = [
                _configuration
            ]
        }.NewtonsoftJsonWarp()!;

        var clientFactory = Market switch
        {
            "ES" => new InMemoryClientFactory<IESSSCD>(new ESSSCDJsonWarper(new VeriFactuSCU(new VeriFactuInMemoryClient(), new VeriFactuSCUConfiguration
            {
                Nif = "M0123456Q",
                NombreRazonEmisor = "In Memory"
            }))),
            _ => throw new NotImplementedException()
        };

        var bootstrapper = Market switch
        {
            "ES" => new Localization.QueueES.QueueESBootstrapper(
                configuration.ftQueues![0].Id,
                loggerFactory,
                clientFactory,
                configuration.ftQueues[0].Configuration!,
                new InMemoryStorageProvider(loggerFactory, QueueId, configuration.ftQueues![0].Configuration!)),
            _ => throw new NotImplementedException()
        };

        return (
            bootstrapper.RegisterForEcho().JsonWarpingAsync<EchoRequest, EchoResponse>(),
            bootstrapper.RegisterForSign().JsonWarpingAsync<ReceiptRequest, ReceiptResponse>(),
            async (JournalRequest request) =>
            {
                var (contentType, pipeReader) = await bootstrapper.RegisterForJournal()(JsonSerializer.Serialize(request));
                return (contentType, pipeReader.AsStream());
            }
        );
    }
}