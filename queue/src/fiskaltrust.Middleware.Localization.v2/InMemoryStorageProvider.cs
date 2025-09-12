using System.Collections.ObjectModel;
using System.Text;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ES;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.storage.encryption.V0;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public class InMemoryStorageProvider : BaseStorageBootStrapper, IStorageProvider
{
    private readonly ILogger<IMiddlewareBootstrapper> _logger;
    private readonly Dictionary<string, object> _configuration;

    private readonly TaskCompletionSource _initializedCompletionSource;
    public Task Initialized => _initializedCompletionSource.Task;

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

    public InMemoryStorageProvider(ILoggerFactory loggerFactory, Guid id, Dictionary<string, object> configuration)
    {
        _configuration = configuration;
        _initializedCompletionSource = new TaskCompletionSource();
        _logger = loggerFactory.CreateLogger<IMiddlewareBootstrapper>();

        _configurationRepository = new InMemoryConfigurationRepository();
        _actionJournalRepository = new InMemoryActionJournalRepository();
        _queueItemRepository = new InMemoryQueueItemRepository();
        _receiptJournalRepository = new InMemoryReceiptJournalRepository();
        _accountMasterDataRepository = new InMemoryAccountMasterDataRepository();
        _journalESRepository = new InMemoryJournalESRepository();
        _outletMasterDataRepository = new InMemoryOutletMasterDataRepository();
        _posSystemMasterDataRepository = new InMemoryPosSystemMasterDataRepository();
        _agencyMasterDataRepository = new InMemoryAgencyMasterDataRepository();

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

    public async Task InitAsync()
    {
        try
        {
            var baseStorageConfig = ParseStorageConfiguration(_configuration);
            var cashBoxes = (await _configurationRepository.GetCashBoxListAsync().ConfigureAwait(false)).ToList();
            if (cashBoxes.Count == 0)
            {
                await ForcePersistMasterDataAsync(baseStorageConfig, _accountMasterDataRepository, _outletMasterDataRepository, _agencyMasterDataRepository, _posSystemMasterDataRepository).ConfigureAwait(false);
            }
            var dbCashBox = cashBoxes.FirstOrDefault(x => x.ftCashBoxId == baseStorageConfig.CashBox.ftCashBoxId);
            await PersistConfigurationParallelAsync(baseStorageConfig, dbCashBox, _configurationRepository, _logger).ConfigureAwait(false);
            _initializedCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during initialization of the InMemoryStorageProvider.");
            _initializedCompletionSource.SetException(e);
        }
    }
}