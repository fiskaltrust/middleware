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
using fiskaltrust.Middleware.Storage.EF.Repositories.IT;

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

            if (_efStorageConfiguration.ConnectionString.StartsWith("raw:"))
            {
                _connectionString = _efStorageConfiguration.ConnectionString.Substring("raw:".Length);
            }
            else
            {
                _connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(_efStorageConfiguration.ConnectionString), queueId.ToByteArray()));
            }

            Update(_connectionString, _efStorageConfiguration.MigrationsTimeoutSec, queueId, logger);

            var configurationRepository = new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId));

            var baseStorageConfig = ParseStorageConfiguration(configuration);

            if (!_connectionString.Contains("MultipleActiveResultSets"))
            {
                _connectionString += ";MultipleActiveResultSets=true";
            }
            var context = new MiddlewareDbContext(_connectionString, _queueId);
            await PersistMasterDataAsync(baseStorageConfig, configurationRepository,
                new EfAccountMasterDataRepository(context), new EfOutletMasterDataRepository(context),
                new EfAgencyMasterDataRepository(context), new EfPosSystemMasterDataRepository(context)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton(x => new MiddlewareDbContext(_connectionString, _queueId));

            services.AddSingleton<IConfigurationRepository>(_ => new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId)));
            services.AddSingleton<IReadOnlyConfigurationRepository>(_ => new EfConfigurationRepository(new MiddlewareDbContext(_connectionString, _queueId)));

            services.AddSingleton<IQueueItemRepository, EfQueueItemRepository>();
            services.AddSingleton<IReadOnlyQueueItemRepository, EfQueueItemRepository>();
            services.AddSingleton<IMiddlewareQueueItemRepository, EfQueueItemRepository>();
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>, EfQueueItemRepository>();

            services.AddSingleton<IJournalATRepository, EfJournalATRepository>();
            services.AddSingleton<IReadOnlyJournalATRepository, EfJournalATRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>, EfJournalATRepository>();

            services.AddSingleton<IMiddlewareJournalDERepository, EfJournalDERepository>();
            services.AddSingleton<IJournalDERepository, EfJournalDERepository>();
            services.AddSingleton<IReadOnlyJournalDERepository, EfJournalDERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>, EfJournalDERepository>();

            services.AddSingleton<IJournalFRRepository, EfJournalFRRepository>();
            services.AddSingleton<IReadOnlyJournalFRRepository, EfJournalFRRepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>, EfJournalFRRepository>();

            services.AddSingleton<IMiddlewareJournalMERepository, EfJournalMERepository>();
            services.AddSingleton<IJournalMERepository, EfJournalMERepository>();
            services.AddSingleton<IReadOnlyJournalMERepository, EfJournalMERepository>();
            services.AddSingleton<IMiddlewareRepository<ftJournalME>, EfJournalMERepository>();

            services.AddSingleton<IJournalITRepository, EfJournalITRepository>();
            services.AddSingleton<IReadOnlyJournalITRepository, EfJournalITRepository>();
            services.AddSingleton<IMiddlewareJournalITRepository, EfJournalITRepository>();

            services.AddSingleton<IReceiptJournalRepository, EfReceiptJournalRepository>();
            services.AddSingleton<IReadOnlyReceiptJournalRepository, EfReceiptJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>, EfReceiptJournalRepository>();

            services.AddSingleton<IMiddlewareActionJournalRepository, EfActionJournalRepository>();
            services.AddSingleton<IActionJournalRepository, EfActionJournalRepository>();
            services.AddSingleton<IReadOnlyActionJournalRepository, EfActionJournalRepository>();
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>, EfActionJournalRepository>();

            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, EfFailedStartTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, EfFailedFinishTransactionRepository>();
            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, EfOpenTransactionRepository>();

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, EfAccountMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, EfOutletMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, EfAgencyMasterDataRepository>();
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, EfPosSystemMasterDataRepository>();
        }

        public static void Update(string connectionString, int timeoutSec, Guid queueId, ILogger<IMiddlewareBootstrapper> logger)
        {
            var schemaString = queueId.ToString("D");
            var contextMigrationsConfiguration = new DbMigrationsConfiguration<MiddlewareDbContext>
            {
                AutomaticMigrationsEnabled = false,
                CommandTimeout = timeoutSec,
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
