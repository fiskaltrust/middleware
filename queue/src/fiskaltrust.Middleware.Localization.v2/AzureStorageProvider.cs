using System.Collections.ObjectModel;
using System.Text;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public class AzureStorageProvider : BaseStorageBootStrapper, IStorageProvider
{
    private readonly QueueConfiguration _queueConfiguration;
    private readonly ILogger<IMiddlewareBootstrapper> _logger;
    private readonly Dictionary<string, object> _configuration;
    private readonly AzureTableStorageConfiguration _tableStorageConfiguration;
    private readonly TableServiceClient _tableServiceClient;
    private readonly BlobServiceClient _blobServiceClient;

    private readonly TaskCompletionSource _initializedCompletionSource;
    public Task Initialized => _initializedCompletionSource.Task;

    public AzureStorageProvider(ILoggerFactory loggerFactory, Guid id, Dictionary<string, object> configuration)
    {
        _configuration = configuration;
        _initializedCompletionSource = new TaskCompletionSource();
        _tableStorageConfiguration = AzureTableStorageConfiguration.FromConfigurationDictionary(configuration);
        _queueConfiguration = new QueueConfiguration { QueueId = id };
        _logger = loggerFactory.CreateLogger<IMiddlewareBootstrapper>();

        if (!string.IsNullOrEmpty(_tableStorageConfiguration.StorageAccountName))
        {
            Uri tableUri;
            Uri blobUri;
            try
            {
                tableUri = new Uri($"https://{_tableStorageConfiguration.StorageAccountName}.table.core.windows.net/");
                blobUri = new Uri($"https://{_tableStorageConfiguration.StorageAccountName}.blob.core.windows.net/");
            }
            catch (Exception e)
            {
                throw new Exception($"The value for the queue parameter storageaccountname '{_tableStorageConfiguration.StorageAccountName}' is not valid.", e);
            }
#if DEBUG
            _tableServiceClient = new TableServiceClient(tableUri, new ChainedTokenCredential(new VisualStudioCredential(), new AzureCliCredential(), new DefaultAzureCredential()));
            _blobServiceClient = new BlobServiceClient(blobUri, new ChainedTokenCredential(new VisualStudioCredential(), new AzureCliCredential(), new DefaultAzureCredential()));
#else
            _tableServiceClient = new TableServiceClient(tableUri, new DefaultAzureCredential());
            _blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
#endif
        }
        else if (!string.IsNullOrEmpty(_tableStorageConfiguration.ConnectionString))
        {
            string connectionString;
            if (_tableStorageConfiguration.ConnectionString.StartsWith("raw:"))
            {
                connectionString = _tableStorageConfiguration.ConnectionString.Substring("raw:".Length);
            }
            else
            {
                connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(_tableStorageConfiguration.ConnectionString), _queueConfiguration.QueueId.ToByteArray()));
            }
            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        else if (!string.IsNullOrEmpty(_tableStorageConfiguration.StorageConnectionString))
        {
            _logger.LogWarning("The queue parameter 'storageconnectionstring' is deprecated. Please use 'storageaccountname' or 'connectionstring' instead.");
            _tableServiceClient = new TableServiceClient(_tableStorageConfiguration.StorageConnectionString);
            _blobServiceClient = new BlobServiceClient(_tableStorageConfiguration.StorageConnectionString);
        }
        else
        {
            throw new Exception("Either the parameter 'storageaccountname' or 'storageconnectionstring' needs to be defined.");
        }

        Task.Run(() => InitAsync());
    }

    public AsyncLazy<IConfigurationRepository> CreateConfigurationRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageConfigurationRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageActionJournalRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageQueueItemRepository(_queueConfiguration, _tableServiceClient, new AzureTableStorageReceiptReferenceIndexRepository(_queueConfiguration, _tableServiceClient));
        });
    public AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageReceiptJournalRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageAccountMasterDataRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageOutletMasterDataRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStoragePosSystemMasterDataRepository(_queueConfiguration, _tableServiceClient);
        });
    public AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository() => new(async () =>
        {
            await Initialized;
            return new AzureTableStorageAgencyMasterDataRepository(_queueConfiguration, _tableServiceClient);
        });

    public async Task InitAsync()
    {
        try
        {
            var databaseMigrator = new DatabaseMigrator(_logger, _tableServiceClient, _blobServiceClient, _queueConfiguration);
            await databaseMigrator.MigrateAsync().ConfigureAwait(false);

            var configurationRepository = new AzureTableStorageConfigurationRepository(_queueConfiguration, _tableServiceClient);
            var baseStorageConfig = ParseStorageConfiguration(_configuration);
            var cashBoxes = (await configurationRepository.GetCashBoxListAsync().ConfigureAwait(false)).ToList();
            if (cashBoxes.Count == 0)
            {
                await ForcePersistMasterDataAsync(baseStorageConfig, new AzureTableStorageAccountMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStorageOutletMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStorageAgencyMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStoragePosSystemMasterDataRepository(_queueConfiguration, _tableServiceClient)).ConfigureAwait(false);
            }
            var dbCashBox = cashBoxes.FirstOrDefault(x => x.ftCashBoxId == baseStorageConfig.CashBox.ftCashBoxId);
            await PersistConfigurationParallelAsync(baseStorageConfig, dbCashBox, configurationRepository, _logger).ConfigureAwait(false);
            _initializedCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during initialization of the AzureStorageProvider.");
            _initializedCompletionSource.SetException(e);
        }
    }
}
