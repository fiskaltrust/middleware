using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Repositories.AT;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;
using fiskaltrust.Middleware.Storage.Azure.Repositories.FR;
using fiskaltrust.Middleware.Storage.Azure.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.Azure.Repositories.ME;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.Azure
{
    public class AzureStorageBootstrapper : BaseStorageBootStrapper
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly TableServiceClient _tableServiceClient;

        private AzureConfigurationRepository _configurationRepository;

        public AzureStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var storageUrl = configuration["storageUrl"].ToString();
            _queueConfiguration = new QueueConfiguration { QueueId = queueId };
            _tableServiceClient = new TableServiceClient(new Uri(storageUrl), new DefaultAzureCredential());
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Dictionary<string,object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            var databaseMigrator = new DatabaseMigrator(logger, _tableServiceClient, _queueConfiguration);
            await databaseMigrator.MigrateAsync().ConfigureAwait(false);

            _configurationRepository = new AzureConfigurationRepository(_queueConfiguration, _tableServiceClient);
            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new AzureAccountMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzureOutletMasterDataRepository(_queueConfiguration, _tableServiceClient),
                new AzureAgencyMasterDataRepository(_queueConfiguration, _tableServiceClient), new AzurePosSystemMasterDataRepository(_queueConfiguration, _tableServiceClient)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton(_queueConfiguration);
            services.AddSingleton(_tableServiceClient);

            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository, AzureQueueItemRepository>();
            services.AddScoped<IMiddlewareQueueItemRepository, AzureQueueItemRepository>();
            services.AddSingleton<IReadOnlyQueueItemRepository, AzureQueueItemRepository>();
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>, AzureQueueItemRepository>();

            services.AddSingleton<IJournalATRepository, AzureJournalATRepository>();
            services.AddSingleton<IReadOnlyJournalATRepository, AzureJournalATRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>, AzureJournalATRepository>();

            services.AddSingleton<IJournalDERepository, AzureJournalDERepository>();
            services.AddSingleton<IMiddlewareJournalDERepository, AzureJournalDERepository>();
            services.AddSingleton<IJournalDERepository, AzureJournalDERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>, AzureJournalDERepository>();

            services.AddSingleton<IJournalFRRepository, AzureJournalFRRepository>();
            services.AddSingleton<IReadOnlyJournalFRRepository, AzureJournalFRRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>, AzureJournalFRRepository>();

            services.AddSingleton<IMiddlewareJournalMERepository, AzureJournalMERepository>();
            services.AddSingleton<IJournalMERepository, AzureJournalMERepository>();
            services.AddSingleton<IReadOnlyJournalMERepository, AzureJournalMERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalME>, AzureJournalMERepository>();

            services.AddSingleton<IReceiptJournalRepository, AzureReceiptJournalRepository>();
            services.AddSingleton<IReadOnlyReceiptJournalRepository, AzureReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>, AzureReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, AzureActionJournalRepository>();
            services.AddSingleton<IActionJournalRepository, AzureActionJournalRepository>();
            services.AddSingleton<IReadOnlyActionJournalRepository, AzureActionJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>, AzureActionJournalRepository>();

            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, AzureFailedFinishTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, AzureFailedStartTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, AzureOpenTransactionRepository>();

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, AzureAccountMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, AzureOutletMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, AzureAgencyMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, AzurePosSystemMasterDataRepository>();
        }
    }
}
