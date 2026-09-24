using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.Base.Interface;
using fiskaltrust.Middleware.Storage.SQLite;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public class SQLiteStorageProvider : BaseStorageBootStrapper, IStorageProvider
{
    private readonly ILogger<IMiddlewareBootstrapper> _logger;
    private readonly Dictionary<string, object> _configuration;

    private readonly TaskCompletionSource<bool> _initializedCompletionSource;
    public Task Initialized => _initializedCompletionSource.Task;

    private string _sqliteFile;
    private SqliteConnectionFactory _connectionFactory;
    private readonly Guid _queueId;
    private readonly SQLiteStorageConfiguration _sqliteStorageConfiguration;

    // Singleton repository instances
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;
    private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
    private readonly IMasterDataRepository<AccountMasterData> _accountMasterDataRepository;
    private readonly IMiddlewareJournalESRepository _journalESRepository;
    private readonly IMasterDataRepository<OutletMasterData> _outletMasterDataRepository;
    private readonly IMasterDataRepository<PosSystemMasterData> _posSystemMasterDataRepository;
    private readonly IMasterDataRepository<AgencyMasterData> _agencyMasterDataRepository;

    


    public SQLiteStorageProvider(ILoggerFactory loggerFactory, Guid queueId, Dictionary<string, object> configuration,
            SQLiteStorageConfiguration sqliteStorageConfiguration)
    {
        _configuration = configuration;
        _initializedCompletionSource = new TaskCompletionSource<bool>();
        _logger = loggerFactory.CreateLogger<IMiddlewareBootstrapper>();
        _queueId = queueId;
        _sqliteStorageConfiguration = sqliteStorageConfiguration;

        _sqliteFile = Path.Combine(_configuration["servicefolder"].ToString(), $"{_queueId}.sqlite");
        _connectionFactory = new SqliteConnectionFactory();

        _configurationRepository = new SQLiteConfigurationRepository(_connectionFactory, _sqliteFile);
        _actionJournalRepository = new SQLiteActionJournalRepository(_connectionFactory, _sqliteFile);
        _queueItemRepository = new SQLiteQueueItemRepository(_connectionFactory, _sqliteFile);
        _receiptJournalRepository = new SQLiteReceiptJournalRepository(_connectionFactory, _sqliteFile);
        _accountMasterDataRepository = new SQLiteAccountMasterDataRepository(_connectionFactory, _sqliteFile);
        //_journalESRepository = new SQLiteJournalESRepository();
        _outletMasterDataRepository = new SQLiteOutletMasterDataRepository(_connectionFactory, _sqliteFile);
        _posSystemMasterDataRepository = new SQLitePosSystemMasterDataRepository(_connectionFactory, _sqliteFile);
        _agencyMasterDataRepository = new SQLiteAgencyMasterDataRepository(_connectionFactory, _sqliteFile);

        Task.Run(() => InitAsync());
    }

    private AsyncLazy<T> CreateAsyncLazy<T>(T from) => new AsyncLazy<T>(async () => { await Initialized; return from; });

    public AsyncLazy<IConfigurationRepository> CreateConfigurationRepository() => CreateAsyncLazy(_configurationRepository);
    public AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository() => CreateAsyncLazy(_actionJournalRepository);
    public AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository() => CreateAsyncLazy(_queueItemRepository);
    public AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository() => CreateAsyncLazy(_receiptJournalRepository);
    public AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository() => CreateAsyncLazy(_accountMasterDataRepository);
    public AsyncLazy<IMiddlewareJournalESRepository> CreateMiddlewareJournalESRepository() => CreateAsyncLazy(_journalESRepository);
    public AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository() => CreateAsyncLazy(_outletMasterDataRepository);
    public AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository() => CreateAsyncLazy(_posSystemMasterDataRepository);
    public AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository() => CreateAsyncLazy(_agencyMasterDataRepository);

    private async Task InitAsync()
    {
        try
        { 
       
            var databaseMigrator = new DatabaseMigrator(_connectionFactory, _sqliteStorageConfiguration.MigrationsTimeoutSec, _sqliteFile, _configuration, _logger);

            var newlyAppliedMigrations = await databaseMigrator.MigrateAsync().ConfigureAwait(false);
            await databaseMigrator.SetWALMode().ConfigureAwait(false);
            var baseStorageConfig = ParseStorageConfiguration(_configuration);

            await PersistMasterDataAsync(baseStorageConfig, _configurationRepository,_accountMasterDataRepository, _outletMasterDataRepository,_agencyMasterDataRepository, _posSystemMasterDataRepository).ConfigureAwait(false);

            var journalFRCopyPayloadRepository = new SQLiteJournalFRCopyPayloadRepository(_connectionFactory, _sqliteFile);
            var journalFRRepository = new SQLiteJournalFRRepository(_connectionFactory, _sqliteFile);

            await PerformMigrationInitialization(newlyAppliedMigrations, journalFRCopyPayloadRepository, journalFRRepository).ConfigureAwait(false);

            await PersistConfigurationAsync(baseStorageConfig, _configurationRepository, _logger).ConfigureAwait(false);
            _initializedCompletionSource.SetResult(true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during initialization of the SQLiteStorageProvider.");
            _initializedCompletionSource.SetException(e);
        }
    }
}