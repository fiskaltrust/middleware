using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.storage.encryption.V0;
using System.Text;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.Middleware.Storage.EF.Repositories.AT;
using fiskaltrust.Middleware.Storage.EF.Repositories.DE;
using fiskaltrust.Middleware.Storage.EF.Repositories.FR;
using System.Data.Entity.Migrations;
using fiskaltrust.Middleware.Storage.EF.Helpers;
using System.Data.Entity.Migrations.History;
using fiskaltrust.Middleware.Storage.EF;
using System.Linq;
using System.Data.Entity.Infrastructure;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Repositories;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Storage.EF.Repositories.ME;
using fiskaltrust.Middleware.Storage.EF.Repositories.MasterData;

namespace fiskaltrust.Middleware.Storage.Ef
{
    public class EfStorageBootstrapper : BaseStorageBootStrapper
    {
        private string _connectionString;
        private readonly Dictionary<string, object> _configuration;
        private readonly EfStorageConfiguration _efStorageConfiguration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly Guid _queueId;

        public EfStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, EfStorageConfiguration efStorageConfiguration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _efStorageConfiguration = efStorageConfiguration;
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
            if (string.IsNullOrEmpty(_efStorageConfiguration.ConnectionString))
            {
                throw new Exception("Database connectionstring not defined");
            }
            _connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(_efStorageConfiguration.ConnectionString), queueId.ToByteArray()));
            Update(_connectionString, queueId, logger);

            var configurationRepository = new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId));

            var baseStorageConfig = ParseStorageConfiguration(configuration);

            var context = new MiddlewareDbContext(_connectionString, _queueId);
            await PersistMasterDataAsync(baseStorageConfig, configurationRepository,
                new EfAccountMasterDataRepository(context), new EfOutletMasterDataRepository(context),
                new EfAgencyMasterDataRepository(context), new EfPosSystemMasterDataRepository(context)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddTransient(x => new MiddlewareDbContext(_connectionString, _queueId));

            services.AddTransient<IConfigurationRepository>(_ => new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId)));
            services.AddTransient<IReadOnlyConfigurationRepository>(_ => new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId)));

            services.AddTransient<IQueueItemRepository, EfQueueItemRepository>();
            services.AddTransient<IReadOnlyQueueItemRepository, EfQueueItemRepository>();
            services.AddTransient<IMiddlewareQueueItemRepository, EfQueueItemRepository>();
            services.AddTransient<IMiddlewareRepository<ftQueueItem>, EfQueueItemRepository>();

            services.AddTransient<IJournalATRepository, EfJournalATRepository>();
            services.AddTransient<IReadOnlyJournalATRepository, EfJournalATRepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalAT>, EfJournalATRepository>();

            services.AddTransient<IMiddlewareJournalDERepository, EfJournalDERepository>();
            services.AddTransient<IJournalDERepository, EfJournalDERepository>();
            services.AddTransient<IReadOnlyJournalDERepository, EfJournalDERepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalDE>, EfJournalDERepository>();

            services.AddTransient<IJournalFRRepository, EfJournalFRRepository>();
            services.AddTransient<IReadOnlyJournalFRRepository, EfJournalFRRepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalFR>, EfJournalFRRepository>();

            services.AddTransient<IMiddlewareJournalMERepository, EfJournalMERepository>();
            services.AddTransient<IJournalMERepository, EfJournalMERepository>();
            services.AddTransient<IReadOnlyJournalMERepository, EfJournalMERepository>();
            services.AddTransient<IMiddlewareRepository<ftJournalME>, EfJournalMERepository>();

            services.AddTransient<IReceiptJournalRepository, EfReceiptJournalRepository>();
            services.AddTransient<IReadOnlyReceiptJournalRepository, EfReceiptJournalRepository>();
            services.AddTransient<IMiddlewareRepository<ftReceiptJournal>, EfReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, EfActionJournalRepository>();
            services.AddTransient<IActionJournalRepository, EfActionJournalRepository>();
            services.AddTransient<IReadOnlyActionJournalRepository, EfActionJournalRepository>();
            services.AddTransient<IMiddlewareRepository<ftActionJournal>, EfActionJournalRepository>();

            services.AddTransient<IPersistentTransactionRepository<FailedStartTransaction>, EfFailedStartTransactionRepository>();
            services.AddTransient<IPersistentTransactionRepository<FailedFinishTransaction>, EfFailedFinishTransactionRepository>();
            services.AddTransient<IPersistentTransactionRepository<OpenTransaction>, EfOpenTransactionRepository>();

            services.AddTransient<IMasterDataRepository<AccountMasterData>, EfAccountMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<OutletMasterData>, EfOutletMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<AgencyMasterData>, EfAgencyMasterDataRepository>();
            services.AddTransient<IMasterDataRepository<PosSystemMasterData>, EfPosSystemMasterDataRepository>();
        }

        public static void Update(string connectionString, Guid queueId, ILogger<IMiddlewareBootstrapper> logger)
        {
            var schemaString = queueId.ToString("D");
            var contextMigrationsConfiguration = new DbMigrationsConfiguration<MiddlewareDbContext>
            {
                AutomaticMigrationsEnabled = false,
                TargetDatabase = new DbConnectionInfo(connectionString, "System.Data.SqlClient"),
                MigrationsAssembly = typeof(MiddlewareDbContext).Assembly,
                MigrationsNamespace = "fiskaltrust.Middleware.Storage.EF.Migrations"
            };

            contextMigrationsConfiguration.SetSqlGenerator("System.Data.SqlClient", new ContextMigrationSqlGenerator(connectionString, schemaString));
            contextMigrationsConfiguration.SetHistoryContextFactory("System.Data.SqlClient", (existingConnection, defaultSchema) => new HistoryContext(existingConnection, schemaString));
            var contextMigrator = new DbMigrator(contextMigrationsConfiguration);
            var pendingMigrations = contextMigrator.GetPendingMigrations().ToArray();
            if (pendingMigrations.Length > 0)
            {
                MigrationQueueIdProvider.QueueId = queueId;

                logger.LogInformation($"{pendingMigrations.Count()} pending database updates were detected. Updating database now.");
                logger.LogDebug($"The following migrations are pending: {string.Join(", ", pendingMigrations)}");
                contextMigrator.Update();
                logger.LogInformation("Successfully updated database.");
            }
        }
    }
}
