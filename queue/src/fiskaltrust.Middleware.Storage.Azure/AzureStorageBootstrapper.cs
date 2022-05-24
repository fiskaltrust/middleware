using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Repositories.AT;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;
using fiskaltrust.Middleware.Storage.Azure.Repositories.FR;
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
        private string _connectionString;

        private AzureConfigurationRepository _configurationRepository;
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly Guid _queueId;

        public AzureStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _queueId = queueId;
            _logger = logger;
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Dictionary<string,object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _connectionString = configuration["connectionstring"].ToString();
            _configurationRepository = new AzureConfigurationRepository(_queueId, _connectionString);
            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new AzureAccountMasterDataRepository(_queueId, _connectionString), new AzureOutletMasterDataRepository(_queueId, _connectionString),
                new AzureAgencyMasterDataRepository(_queueId, _connectionString), new AzurePosSystemMasterDataRepository(_queueId, _connectionString)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository>(x => new AzureQueueItemRepository(_queueId, _connectionString));
            services.AddScoped<IMiddlewareQueueItemRepository>(x => new AzureQueueItemRepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyQueueItemRepository>(x => new AzureQueueItemRepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>>(x => new AzureQueueItemRepository(_queueId, _connectionString));

            services.AddSingleton<IJournalATRepository>(x => new AzureJournalATRepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyJournalATRepository>(x => new AzureJournalATRepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>>(x => new AzureJournalATRepository(_queueId, _connectionString));

            services.AddSingleton<IJournalDERepository>(x => new AzureJournalDERepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareJournalDERepository>(x => new AzureJournalDERepository(_queueId, _connectionString));
            services.AddSingleton<IJournalDERepository>(x => new AzureJournalDERepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>>(x => new AzureJournalDERepository(_queueId, _connectionString));

            services.AddSingleton<IJournalFRRepository>(x => new AzureJournalFRRepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyJournalFRRepository>(x => new AzureJournalFRRepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>>(x => new AzureJournalFRRepository(_queueId, _connectionString));

            services.AddSingleton<IMiddlewareJournalMERepository>(x => new AzureJournalMERepository(_queueId, _connectionString));
            services.AddSingleton<IJournalMERepository>(x => new AzureJournalMERepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyJournalMERepository>(x => new AzureJournalMERepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalME>>(x => new AzureJournalMERepository(_queueId, _connectionString));

            services.AddSingleton<IReceiptJournalRepository>(x => new AzureReceiptJournalRepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyReceiptJournalRepository>(x => new AzureReceiptJournalRepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>>(x => new AzureReceiptJournalRepository(_queueId, _connectionString));

            services.AddSingleton<IMiddlewareActionJournalRepository>(x => new AzureActionJournalRepository(_queueId, _connectionString));
            services.AddSingleton<IActionJournalRepository>(x => new AzureActionJournalRepository(_queueId, _connectionString));
            services.AddSingleton<IReadOnlyActionJournalRepository>(x => new AzureActionJournalRepository(_queueId, _connectionString));
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>>(x => new AzureActionJournalRepository(_queueId, _connectionString));

            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, AzureFailedFinishTransactionRepository>(x => new AzureFailedFinishTransactionRepository(_queueId, _connectionString));
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, AzureFailedStartTransactionRepository>(x => new AzureFailedStartTransactionRepository(_queueId, _connectionString));
            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, AzureOpenTransactionRepository>(x => new AzureOpenTransactionRepository(_queueId, _connectionString));

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, AzureAccountMasterDataRepository>(x => new AzureAccountMasterDataRepository(_queueId, _connectionString));
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, AzureOutletMasterDataRepository>(x => new AzureOutletMasterDataRepository(_queueId, _connectionString));
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, AzureAgencyMasterDataRepository>(x => new AzureAgencyMasterDataRepository(_queueId, _connectionString));
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, AzurePosSystemMasterDataRepository>(x => new AzurePosSystemMasterDataRepository(_queueId, _connectionString));
        }
    }
}
