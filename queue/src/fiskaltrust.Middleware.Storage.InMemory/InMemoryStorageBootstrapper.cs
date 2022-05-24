using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.AT;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE.MasterData;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.InMemory
{
    public class InMemoryStorageBootstrapper : BaseStorageBootStrapper
    {
        private InMemoryConfigurationRepository _configurationRepository;
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;

        public InMemoryStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _ = queueId;
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        public async Task InitAsync(Dictionary<string,object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configurationRepository = new InMemoryConfigurationRepository();
            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new InMemoryAccountMasterDataRepository(), new InMemoryOutletMasterDataRepository(),
                new InMemoryAgencyMasterDataRepository(), new InMemoryPosSystemMasterDataRepository()).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository, InMemoryQueueItemRepository>();
            services.AddSingleton<IMiddlewareQueueItemRepository, InMemoryQueueItemRepository>();
            services.AddSingleton<IReadOnlyQueueItemRepository, InMemoryQueueItemRepository>();
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>, InMemoryQueueItemRepository>();

            services.AddSingleton<IJournalATRepository, InMemoryJournalATRepository>();
            services.AddSingleton<IReadOnlyJournalATRepository, InMemoryJournalATRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>, InMemoryJournalATRepository>();

            services.AddSingleton<IMiddlewareJournalDERepository, InMemoryJournalDERepository >();
            services.AddSingleton<IReadOnlyJournalDERepository, InMemoryJournalDERepository>();
            services.AddSingleton<IJournalDERepository, InMemoryJournalDERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>, InMemoryJournalDERepository>();

            services.AddSingleton<IJournalFRRepository, InMemoryJournalFRRepository>();
            services.AddSingleton<IReadOnlyJournalFRRepository, InMemoryJournalFRRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>, InMemoryJournalFRRepository>();

            services.AddSingleton<IMiddlewareJournalMERepository, InMemoryJournalMERepository>();
            services.AddSingleton<IJournalMERepository, InMemoryJournalMERepository>();
            services.AddSingleton<IReadOnlyJournalMERepository, InMemoryJournalMERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalME>, InMemoryJournalMERepository>();

            services.AddSingleton<IReceiptJournalRepository, InMemoryReceiptJournalRepository>();
            services.AddSingleton<IReadOnlyReceiptJournalRepository, InMemoryReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>, InMemoryReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, InMemoryActionJournalRepository>();
            services.AddSingleton<IActionJournalRepository, InMemoryActionJournalRepository>();
            services.AddSingleton<IReadOnlyActionJournalRepository, InMemoryActionJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>, InMemoryActionJournalRepository>();

            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, InMemoryFailedFinishTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, InMemoryFailedStartTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, InMemoryOpenTransactionRepository>();
            
            services.AddSingleton<IMasterDataRepository<AccountMasterData>, InMemoryAccountMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, InMemoryOutletMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, InMemoryAgencyMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, InMemoryPosSystemMasterDataRepository>();
        }
    }
}
