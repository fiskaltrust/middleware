using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.MySQL.Repositories;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.AT;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.DE;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.DE.MasterData;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.FR;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.MySQL
{
    public class MySQLBootstrapper : BaseStorageBootStrapper
    {
        private string _connectionString;
        private MySQLConfigurationRepository _configurationRepository;
        private readonly Dictionary<string, object> _configuration;
        private readonly MySQLStorageConfiguration _mySQLStorageConfiguration;
        private readonly Guid _queueId;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;

        public MySQLBootstrapper(Guid queueId, Dictionary<string, object> configuration, MySQLStorageConfiguration mySQLStorageConfiguration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _configuration = configuration;
            _mySQLStorageConfiguration = mySQLStorageConfiguration;
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
            if (string.IsNullOrEmpty(_mySQLStorageConfiguration.ConnectionString))
            {
                throw new Exception("Database connectionstring not defined");
            }

            _connectionString = Encoding.UTF8.GetString(Encryption.Decrypt(Convert.FromBase64String(_mySQLStorageConfiguration.ConnectionString), queueId.ToByteArray()));

            var databaseMigrator = new DatabaseMigrator(_connectionString, queueId, logger);
            var dbName = await databaseMigrator.MigrateAsync().ConfigureAwait(false);

            _connectionString += $"database={ dbName };";

            _configurationRepository = new MySQLConfigurationRepository(_connectionString);

            var baseStorageConfig = ParseStorageConfiguration(configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,
                new MySQLAccountMasterDataRepository(_connectionString), new MySQLOutletMasterDataRepository(_connectionString),
                new MySQLAgencyMasterDataRepository(_connectionString), new MySQLPosSystemMasterDataRepository(_connectionString)).ConfigureAwait(false);
            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, logger).ConfigureAwait(false);
        }

        private void AddRepositories(IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRepository>(_configurationRepository);
            services.AddSingleton<IReadOnlyConfigurationRepository>(_configurationRepository);

            services.AddSingleton<IQueueItemRepository>(x => new MySQLQueueItemRepository(_connectionString));
            services.AddSingleton<IMiddlewareQueueItemRepository>(x => new MySQLQueueItemRepository(_connectionString));
            services.AddSingleton<IReadOnlyQueueItemRepository>(x => new MySQLQueueItemRepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftQueueItem>>(x => new MySQLQueueItemRepository(_connectionString));

            services.AddSingleton<IJournalATRepository>(x => new MySQLJournalATRepository(_connectionString));
            services.AddSingleton<IReadOnlyJournalATRepository>(x => new MySQLJournalATRepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalAT>>(x => new MySQLJournalATRepository(_connectionString));

            services.AddSingleton<IMiddlewareJournalDERepository>(x => new MySQLJournalDERepository(_connectionString));
            services.AddSingleton<IJournalDERepository>(x => new MySQLJournalDERepository(_connectionString));
            services.AddSingleton<IReadOnlyJournalDERepository>(x => new MySQLJournalDERepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalDE>>(x => new MySQLJournalDERepository(_connectionString));

            services.AddSingleton<IJournalFRRepository>(x => new MySQLJournalFRRepository(_connectionString));
            services.AddSingleton<IReadOnlyJournalFRRepository>(x => new MySQLJournalFRRepository(_connectionString));
            services.AddSingleton<IMiddlewareJournalFRRepository>(x => new MySQLJournalFRRepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftJournalFR>>(x => new MySQLJournalFRRepository(_connectionString));

            services.AddSingleton<IReceiptJournalRepository>(x => new MySQLReceiptJournalRepository(_connectionString));
            services.AddSingleton<IReadOnlyReceiptJournalRepository>(x => new MySQLReceiptJournalRepository(_connectionString));
            services.AddSingleton<IMiddlewareReceiptJournalRepository>(x => new MySQLReceiptJournalRepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftReceiptJournal>>(x => new MySQLReceiptJournalRepository(_connectionString));

            services.AddSingleton<IActionJournalRepository>(x => new MySQLActionJournalRepository(_connectionString));
            services.AddSingleton<IReadOnlyActionJournalRepository>(x => new MySQLActionJournalRepository(_connectionString));
            services.AddSingleton<IMiddlewareActionJournalRepository>(x => new MySQLActionJournalRepository(_connectionString));
            services.AddSingleton<IMiddlewareRepository<ftActionJournal>>(x => new MySQLActionJournalRepository(_connectionString));

            services.AddSingleton<IPersistentTransactionRepository<OpenTransaction>, MySQLOpenTransactionRepository>(x => new MySQLOpenTransactionRepository(_connectionString));
            services.AddSingleton<IPersistentTransactionRepository<FailedFinishTransaction>, MySQLFailedFinishTransactionRepository>(x => new MySQLFailedFinishTransactionRepository(_connectionString));
            services.AddSingleton<IPersistentTransactionRepository<FailedStartTransaction>, MySQLFailedStartTransactionRepository>(x => new MySQLFailedStartTransactionRepository(_connectionString));

            services.AddSingleton<IMasterDataRepository<AccountMasterData>, MySQLAccountMasterDataRepository>(x => new MySQLAccountMasterDataRepository(_connectionString));
            services.AddSingleton<IMasterDataRepository<OutletMasterData>, MySQLOutletMasterDataRepository>(x => new MySQLOutletMasterDataRepository(_connectionString));
            services.AddSingleton<IMasterDataRepository<PosSystemMasterData>, MySQLPosSystemMasterDataRepository>(x => new MySQLPosSystemMasterDataRepository(_connectionString));
            services.AddSingleton<IMasterDataRepository<AgencyMasterData>, MySQLAgencyMasterDataRepository>(x => new MySQLAgencyMasterDataRepository( _connectionString));
        }
    }
}
