using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.EFCore.Repositories;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.AT;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.DE;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.DE.MasterData;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.FR;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.ME;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable
namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL
{
    public class EFCorePostgreSQLStorageBootstrapper : BaseStorageBootStrapper
    {
        private string _connectionString;
        private DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext> _optionsBuilder;
        private readonly Dictionary<string, object> _configuration;
        private readonly PostgreSQLStorageConfiguration _posgresQLStorageConfiguration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly Guid _queueId;

        public EFCorePostgreSQLStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, PostgreSQLStorageConfiguration posgresQLStorageConfiguration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _posgresQLStorageConfiguration = posgresQLStorageConfiguration;
            _logger = logger;
            _queueId = queueId;
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_queueId, _configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            if (string.IsNullOrEmpty(_posgresQLStorageConfiguration.ConnectionString))
            {
                throw new Exception("Database connectionstring not defined");
            }
            _connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(_posgresQLStorageConfiguration.ConnectionString), queueId.ToByteArray()));
            _optionsBuilder = new DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext>();
            _optionsBuilder.UseNpgsql(_connectionString);
            Update(_optionsBuilder.Options, queueId, logger);

            var configurationRepository = new EFCoreConfigurationRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId));

            var baseStorageConfig = ParseStorageConfiguration(configuration);

            var context = new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId);
            await PersistMasterDataAsync(baseStorageConfig, configurationRepository,
                new EFCoreAccountMasterDataRepository(context), new EFCoreOutletMasterDataRepository(context),
                new EFCoreAgencyMasterDataRepository(context), new EFCorePosSystemMasterDataRepository(context));
            await PersistConfigurationAsync(baseStorageConfig, configurationRepository, logger);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddTransient(x => new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId));

            services.AddTransient<IConfigurationRepository>(_ => new EFCoreConfigurationRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyConfigurationRepository>(_ => new EFCoreConfigurationRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IQueueItemRepository>(_ => new EFCoreQueueItemRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyQueueItemRepository>(_ => new EFCoreQueueItemRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareQueueItemRepository>(_ => new EFCoreQueueItemRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftQueueItem>>(_ => new EFCoreQueueItemRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IJournalATRepository>(_ => new EFCoreJournalATRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyJournalATRepository>(_ => new EFCoreJournalATRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftJournalAT>>(_ => new EFCoreJournalATRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IJournalDERepository>(_ => new EFCoreJournalDERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyJournalDERepository>(_ => new EFCoreJournalDERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftJournalDE>>(_ => new EFCoreJournalDERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IJournalFRRepository>(_ => new EFCoreJournalFRRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyJournalFRRepository>(_ => new EFCoreJournalFRRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftJournalFR>>(_ => new EFCoreJournalFRRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IJournalMERepository>(_ => new EFCoreJournalMERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyJournalMERepository>(_ => new EFCoreJournalMERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftJournalME>>(_ => new EFCoreJournalMERepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IReceiptJournalRepository>(_ => new EFCoreReceiptJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyReceiptJournalRepository>(_ => new EFCoreReceiptJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftReceiptJournal>>(_ => new EFCoreReceiptJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddSingleton<IMiddlewareActionJournalRepository>(_ => new EFCoreActionJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IActionJournalRepository>(_ => new EFCoreActionJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IReadOnlyActionJournalRepository>(_ => new EFCoreActionJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMiddlewareRepository<ftActionJournal>>(_ => new EFCoreActionJournalRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IPersistentTransactionRepository<FailedStartTransaction>>(_ => new EFCoreFailedStartTransactionRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IPersistentTransactionRepository<FailedFinishTransaction>>(_ => new EFCoreFailedFinishTransactionRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IPersistentTransactionRepository<OpenTransaction>>(_ => new EFCoreOpenTransactionRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

            services.AddTransient<IMasterDataRepository<AccountMasterData>>(_ => new EFCoreAccountMasterDataRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMasterDataRepository<OutletMasterData>>(_ => new EFCoreOutletMasterDataRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMasterDataRepository<AgencyMasterData>>(_ => new EFCoreAgencyMasterDataRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));
            services.AddTransient<IMasterDataRepository<PosSystemMasterData>>(_ => new EFCorePosSystemMasterDataRepository(new PostgreSQLMiddlewareDbContext(_optionsBuilder.Options, _queueId)));

        }

        public static void Update(DbContextOptions dbContextOptions, Guid queueId, ILogger<IMiddlewareBootstrapper> logger)
        {
            using (var context = new PostgreSQLMiddlewareDbContext(dbContextOptions, queueId))
            {
                context.Database.SetCommandTimeout(160);
                context.Database.EnsureCreated();
                context.Database.Migrate();
            }
        }
    }
}
