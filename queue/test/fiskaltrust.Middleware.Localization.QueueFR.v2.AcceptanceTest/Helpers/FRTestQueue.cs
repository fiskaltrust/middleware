using System.Text.Json;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;

/// <summary>A real QueueFR.v2 on the in-memory storage, in front of the given SCU.</summary>
public static class FRTestQueue
{
    public const string CashBoxIdentification = "cashbox-fr";

    public static (Func<string, Task<string>> SignProcessor, Guid CashBoxId) Create(IFRSSCD scu)
    {
        var queueId = Guid.NewGuid();
        var cashBoxId = Guid.NewGuid();

        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue>
                {
                    new() { ftQueueId = queueId, ftCashBoxId = cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" },
                })
            },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR>
                {
                    new() { ftQueueFRId = queueId, CashBoxIdentification = CashBoxIdentification, Siret = "12345678901234" },
                })
            },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration
                {
                    Account = new AccountMasterData
                    {
                        AccountId = Guid.NewGuid(),
                        AccountName = "fiskaltrust SARL",
                        Street = "1 rue de la Paix",
                        Zip = "75002",
                        City = "Paris",
                        Country = "FR",
                    },
                })
            },
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(queueId, configuration, loggerFactory);
        var bootstrapper = new QueueFRBootstrapper(queueId, loggerFactory, configuration, scu, storageProvider);
        return (bootstrapper.RegisterForSign(), cashBoxId);
    }
}
