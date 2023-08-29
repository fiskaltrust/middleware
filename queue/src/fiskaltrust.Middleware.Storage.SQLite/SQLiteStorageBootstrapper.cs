using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.AT;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.DE;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.DE.MasterData;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.ME;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.SQLite
{
    public class SQLiteStorageBootstrapper : BaseStorageBootStrapper
    {
        private string _sqliteFile;
        private SqliteConnectionFactory _connectionFactory;
        private SQLiteConfigurationRepository _configurationRepository;
        private readonly Dictionary<string, object> _configuration;
        private readonly Guid _queueId;
        private readonly SQLiteStorageConfiguration _sqliteStorageConfiguration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;

        public SQLiteStorageBootstrapper(Guid queueId, Dictionary<string, object> configuration, SQLiteStorageConfiguration sqliteStorageConfiguration, ILogger<IMiddlewareBootstrapper> logger, SQLiteJournalFRRepository journalFrRepository)
            : base(journalFrRepository) 
        {
            _configuration = configuration;
            _sqliteStorageConfiguration = sqliteStorageConfiguration;
            _queueId = queueId;
            _logger = logger;
        }

        public void ConfigureStorageServices(IServiceCollection serviceCollection)
        {
            InitAsync(_queueId, _configuration, _logger).Wait();
            AddRepositories(serviceCollection);
        }

        private async Task InitAsync(Guid queueId, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _sqliteFile = Path.Combine(configuration["servicefolder"].ToString(), $"{queueId}.sqlite");
            _connectionFactory = new SqliteConnectionFactory();
            var databaseMigrator = new DatabaseMigrator(_connectionFactory, _sqliteStorageConfiguration.MigrationsTimeoutSec, _sqliteFile, _configuration, logger);
            await databaseMigrator.MigrateAsync().ConfigureAwait(false);
            await databaseMigrator.SetWALMode().ConfigureAwait(false);

            _configurationRepository = new SQLiteConfigurationRepository(_connectionFactory, _sqliteFile);

            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new SQLiteAccountMasterDataRepository(_connectionFactory, _sqliteFile), new SQLiteOutletMasterDataRepository(_connectionFactory, _sqliteFile),
                new SQLiteAgencyMasterDataRepository(_connectionFactory, _sqliteFile), new SQLitePosSystemMasterDataRepository(_connectionFactory, _sqliteFile)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository>(x => new SQLiteQueueItemRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareQueueItemRepository>(x => new SQLiteQueueItemRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyQueueItemRepository>(x => new SQLiteQueueItemRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>>(x => new SQLiteQueueItemRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IJournalATRepository>(x => new SQLiteJournalATRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyJournalATRepository>(x => new SQLiteJournalATRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>>(x => new SQLiteJournalATRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IMiddlewareJournalDERepository>(x => new SQLiteJournalDERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IJournalDERepository>(x => new SQLiteJournalDERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyJournalDERepository>(x => new SQLiteJournalDERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>>(x => new SQLiteJournalDERepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IJournalFRRepository>(x => new SQLiteJournalFRRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyJournalFRRepository>(x => new SQLiteJournalFRRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareJournalFRRepository>(x => new SQLiteJournalFRRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>>(x => new SQLiteJournalFRRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IMiddlewareJournalMERepository>(x => new SQLiteJournalMERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IJournalMERepository>(x => new SQLiteJournalMERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyJournalMERepository>(x => new SQLiteJournalMERepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftJournalME>>(x => new SQLiteJournalMERepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IJournalITRepository>(x => new SQLiteJournalITRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyJournalITRepository>(x => new SQLiteJournalITRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareJournalITRepository>(x => new SQLiteJournalITRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IReceiptJournalRepository>(x => new SQLiteReceiptJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyReceiptJournalRepository>(x => new SQLiteReceiptJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareReceiptJournalRepository>(x => new SQLiteReceiptJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>>(x => new SQLiteReceiptJournalRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IMiddlewareActionJournalRepository>(x => new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IActionJournalRepository>(x => new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IReadOnlyActionJournalRepository>(x => new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareActionJournalRepository>(x => new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>>(x => new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, SQLiteOpenTransactionRepository>(x => new SQLiteOpenTransactionRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, SQLiteFailedFinishTransactionRepository>(x => new SQLiteFailedFinishTransactionRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, SQLiteFailedStartTransactionRepository>(x => new SQLiteFailedStartTransactionRepository(_connectionFactory, _sqliteFile));

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, SQLiteAccountMasterDataRepository>(x => new SQLiteAccountMasterDataRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, SQLiteOutletMasterDataRepository>(x => new SQLiteOutletMasterDataRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, SQLitePosSystemMasterDataRepository>(x => new SQLitePosSystemMasterDataRepository(_connectionFactory, _sqliteFile));
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, SQLiteAgencyMasterDataRepository>(x => new SQLiteAgencyMasterDataRepository(_connectionFactory, _sqliteFile));
        }
    }
}
