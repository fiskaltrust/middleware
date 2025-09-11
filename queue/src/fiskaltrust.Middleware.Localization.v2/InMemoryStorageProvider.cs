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
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly AsyncLazy<IMiddlewareActionJournalRepository> _actionJournalRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository;
    private readonly AsyncLazy<IMiddlewareReceiptJournalRepository> _receiptJournalRepository;
    private readonly AsyncLazy<IMasterDataRepository<AccountMasterData>> _accountMasterDataRepository;
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalESRepository;
    private readonly AsyncLazy<IMasterDataRepository<OutletMasterData>> _outletMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<PosSystemMasterData>> _posSystemMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<AgencyMasterData>> _agencyMasterDataRepository;

    public InMemoryStorageProvider(ILoggerFactory loggerFactory, Guid id, Dictionary<string, object> configuration)
    {
        _configuration = configuration;
        _initializedCompletionSource = new TaskCompletionSource();
        _logger = loggerFactory.CreateLogger<IMiddlewareBootstrapper>();

        _configurationRepository = new AsyncLazy<IConfigurationRepository>(async () =>
        {
            await Initialized;
            return new InMemoryConfigurationRepository();
        });
        _actionJournalRepository = new AsyncLazy<IMiddlewareActionJournalRepository>(async () =>
        {
            await Initialized;
            return new InMemoryActionJournalRepository();
        });
        _queueItemRepository = new AsyncLazy<IMiddlewareQueueItemRepository>(async () =>
        {
            await Initialized;
            return new InMemoryQueueItemRepository();
        });
        _receiptJournalRepository = new AsyncLazy<IMiddlewareReceiptJournalRepository>(async () =>
        {
            await Initialized;
            return new InMemoryReceiptJournalRepository();
        });
        _accountMasterDataRepository = new AsyncLazy<IMasterDataRepository<AccountMasterData>>(async () =>
        {
            await Initialized;
            return new InMemoryAccountMasterDataRepository();
        });
        _journalESRepository = new AsyncLazy<IMiddlewareJournalESRepository>(async () =>
        {
            await Initialized;
            return new InMemoryJournalESRepository();
        });
        _outletMasterDataRepository = new AsyncLazy<IMasterDataRepository<OutletMasterData>>(async () =>
        {
            await Initialized;
            return new InMemoryOutletMasterDataRepository();
        });
        _posSystemMasterDataRepository = new AsyncLazy<IMasterDataRepository<PosSystemMasterData>>(async () =>
        {
            await Initialized;
            return new InMemoryPosSystemMasterDataRepository();
        });
        _agencyMasterDataRepository = new AsyncLazy<IMasterDataRepository<AgencyMasterData>>(async () =>
        {
            await Initialized;
            return new InMemoryAgencyMasterDataRepository();
        });

        Task.Run(() => InitAsync());
    }

    public AsyncLazy<IConfigurationRepository> CreateConfigurationRepository() => _configurationRepository;
    public AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository() => _actionJournalRepository;
    public AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository() => _queueItemRepository;
    public AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository() => _receiptJournalRepository;
    public AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository() => _accountMasterDataRepository;
    public AsyncLazy<IMiddlewareJournalESRepository> CreateMiddlewareJournalESRepository() => _journalESRepository;
    public AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository() => _outletMasterDataRepository;
    public AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository() => _posSystemMasterDataRepository;
    public AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository() => _agencyMasterDataRepository;

    public async Task InitAsync()
    {
        try
        {
            var configurationRepository = new InMemoryConfigurationRepository();
            var baseStorageConfig = ParseStorageConfiguration(_configuration);
            var cashBoxes = (await configurationRepository.GetCashBoxListAsync().ConfigureAwait(false)).ToList();
            if (cashBoxes.Count == 0)
            {
                await ForcePersistMasterDataAsync(baseStorageConfig, new InMemoryAccountMasterDataRepository(), new InMemoryOutletMasterDataRepository(), new InMemoryAgencyMasterDataRepository(), new InMemoryPosSystemMasterDataRepository()).ConfigureAwait(false);
            }
            var dbCashBox = cashBoxes.FirstOrDefault(x => x.ftCashBoxId == baseStorageConfig.CashBox.ftCashBoxId);
            await PersistConfigurationParallelAsync(baseStorageConfig, dbCashBox, configurationRepository, _logger).ConfigureAwait(false);
            _initializedCompletionSource.SetResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during initialization of the InMemoryStorageProvider.");
            _initializedCompletionSource.SetException(e);
        }
    }
}