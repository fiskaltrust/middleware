using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
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

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class AzureTableStorageBootstrapper : BaseStorageBootStrapper
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly QueueConfiguration _queueConfiguration;
        private TableServiceClient _tableServiceClient;
        private BlobServiceClient _blobServiceClient;
        private AzureTableStorageConfigurationRepository _configurationRepository;

        public AzureTableStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration.ConvertToCaseInSensitive();
            _logger = logger;
            _queueConfiguration = new QueueConfiguration { QueueId = queueId };
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            if (string.IsNullOrEmpty(configuration["storageaccountname"]?.ToString()))
            {
                throw new Exception("The parameter 'storageUrl' needs to be defined.");
            }
            var accountName = configuration["storageaccountname"]?.ToString();
            _tableServiceClient = new TableServiceClient(new Uri($"https://{accountName}.table.core.windows.net/"), new DefaultAzureCredential());
            _blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net/"), new DefaultAzureCredential());

            var databaseMigrator = new DatabaseMigrator(logger, _tableServiceClient, _queueConfiguration);
            await databaseMigrator.MigrateAsync().ConfigureAwait(false);

            _configurationRepository = new AzureTableStorageConfigurationRepository(_queueConfiguration, _tableServiceClient);
            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new AzureTableStorageAccountMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStorageOutletMasterDataRepository(_queueConfiguration, _tableServiceClient),
                new AzureTableStorageAgencyMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureTableStoragePosSystemMasterDataRepository(_queueConfiguration, _tableServiceClient)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton(_queueConfiguration);
            services.AddSingleton(_tableServiceClient);
            services.AddSingleton(_blobServiceClient);

            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddScoped<IMiddlewareQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddSingleton<IReadOnlyQueueItemRepository, AzureTableStorageQueueItemRepository>();
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>, AzureTableStorageQueueItemRepository>();

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
            
            services.AddSingleton<IReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IReadOnlyReceiptJournalRepository, AzureTableStorageReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>, AzureTableStorageReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, AzureTableStorageActionJournalRepository>();
            services.AddSingleton<IActionJournalRepository, AzureTableStorageActionJournalRepository>();
            services.AddSingleton<IReadOnlyActionJournalRepository, AzureTableStorageActionJournalRepository>();
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
