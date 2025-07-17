using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.IT;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ME;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.encryption.V0;
using System.Linq;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class AzureTableStorageBootstrapper : BaseStorageBootStrapper
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly AzureTableStorageConfiguration _tableStorageConfiguration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly QueueConfiguration _queueConfiguration;

        private TableServiceClient _tableServiceClient;
        private BlobServiceClient _blobServiceClient;
        private AzureTableStorageConfigurationRepository _configurationRepository;

        public AzureTableStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, AzureTableStorageConfiguration tableStorageConfiguration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _tableStorageConfiguration = tableStorageConfiguration;
            _logger = logger;
            _queueConfiguration = new QueueConfiguration { QueueId = queueId };
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync().Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync()
        {
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
                _tableServiceClient = new TableServiceClient(tableUri, new DefaultAzureCredential());
                _blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
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

            var databaseMigrator = new DatabaseMigrator(_logger, _tableServiceClient, _blobServiceClient, _queueConfiguration);
            await databaseMigrator.MigrateAsync().ConfigureAwait(false);

            _configurationRepository = new AzureTableStorageConfigurationRepository(_queueConfiguration, _tableServiceClient);
            var baseStorageConfig = ParseStorageConfiguration(_configuration);

            var cashBoxes = (await _configurationRepository.GetCashBoxListAsync().ConfigureAwait(false)).ToList();
            if (cashBoxes.Count == 0)
            {
                await ForcePersistMasterDataAsync(baseStorageConfig, new AzureTableStorageAccountMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStorageOutletMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStorageAgencyMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStoragePosSystemMasterDataRepository(_queueConfiguration, _tableServiceClient)).ConfigureAwait(false);
            }

            var dbCashBox = cashBoxes.FirstOrDefault(x => x.ftCashBoxId == baseStorageConfig.CashBox.ftCashBoxId);
            await PersistConfigurationParallelAsync(baseStorageConfig, dbCashBox, _configurationRepository, _logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton(_queueConfiguration);
            services.AddSingleton(_tableServiceClient);
            services.AddSingleton(_blobServiceClient);

            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddScoped<IMiddlewareQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddSingleton<IReadOnlyQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>, AzureTableStorageQueueItemRepository>();
            services.AddSingleton<AzureTableStorageReceiptReferenceIndexRepository>();

            services.AddSingleton<IJournalATRepository, AzureTableStorageJournalATRepository>();
            services.AddSingleton<IReadOnlyJournalATRepository, AzureTableStorageJournalATRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>, AzureTableStorageJournalATRepository>();

            services.AddSingleton<IJournalDERepository, AzureTableStorageJournalDERepository>();
            services.AddSingleton<IMiddlewareJournalDERepository, AzureTableStorageJournalDERepository>();
            services.AddSingleton<IReadOnlyJournalDERepository, AzureTableStorageJournalDERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>, AzureTableStorageJournalDERepository>();

            services.AddSingleton<IJournalFRRepository, AzureTableStorageJournalFRRepository>();
            services.AddSingleton<IReadOnlyJournalFRRepository, AzureTableStorageJournalFRRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>, AzureTableStorageJournalFRRepository>();

            services.AddSingleton<IMiddlewareJournalMERepository, AzureTableStorageJournalMERepository>();
            services.AddSingleton<IJournalMERepository, AzureTableStorageJournalMERepository>();
            services.AddSingleton<IReadOnlyJournalMERepository, AzureTableStorageJournalMERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalME>, AzureTableStorageJournalMERepository>();

            services.AddSingleton<IJournalITRepository, AzureTableStorageJournalITRepository>();
            services.AddSingleton<IReadOnlyJournalITRepository, AzureTableStorageJournalITRepository>();
            services.AddSingleton<IMiddlewareJournalITRepository, AzureTableStorageJournalITRepository>();

            services.AddSingleton<IMiddlewareReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IReadOnlyReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>, AzureTableStorageReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, AzureTableStorageActionJournalRepository>();
            services.AddSingleton<IActionJournalRepository, AzureTableStorageActionJournalRepository>();
            services.AddSingleton<IReadOnlyActionJournalRepository, AzureTableStorageActionJournalRepository>();
            services.AddSingleton<IMiddlewareReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>, AzureTableStorageActionJournalRepository>();

            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, AzureTableStorageFailedFinishTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, AzureTableStorageFailedStartTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, AzureTableStorageOpenTransactionRepository>();

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, AzureTableStorageAccountMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, AzureTableStorageOutletMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, AzureTableStorageAgencyMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, AzureTableStoragePosSystemMasterDataRepository>();
        }
    }
}
