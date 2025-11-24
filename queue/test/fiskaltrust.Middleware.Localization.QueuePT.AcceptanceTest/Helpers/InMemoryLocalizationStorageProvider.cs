using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.InMemory;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT.AcceptanceTest.Helpers;

internal sealed class InMemoryLocalizationStorageProvider : IStorageProvider
{
    private readonly Task _initialized;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository;
    private readonly AsyncLazy<IMiddlewareReceiptJournalRepository> _receiptJournalRepository;
    private readonly AsyncLazy<IMiddlewareActionJournalRepository> _actionJournalRepository;
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalEsRepository;
    private readonly AsyncLazy<IMasterDataRepository<AccountMasterData>> _accountMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<OutletMasterData>> _outletMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<PosSystemMasterData>> _posSystemMasterDataRepository;
    private readonly AsyncLazy<IMasterDataRepository<AgencyMasterData>> _agencyMasterDataRepository;

    public InMemoryLocalizationStorageProvider(Guid queueId, Dictionary<string, object> configuration, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<IMiddlewareBootstrapper>();
        var bootstrapper = new InMemoryStorageBootstrapper(queueId, configuration, logger);

        var services = new ServiceCollection();
        bootstrapper.ConfigureStorageServices(services);

        services.AddSingleton<IMiddlewareJournalESRepository, InMemoryJournalEsRepository>();
        services.AddSingleton<IJournalESRepository>(sp => sp.GetRequiredService<IMiddlewareJournalESRepository>());
        services.AddSingleton<IReadOnlyJournalESRepository>(sp => sp.GetRequiredService<IMiddlewareJournalESRepository>());

        var provider = services.BuildServiceProvider();

        _configurationRepository = new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(provider.GetRequiredService<IConfigurationRepository>()));
        _queueItemRepository = new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(provider.GetRequiredService<IMiddlewareQueueItemRepository>()));
        _receiptJournalRepository = new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult(provider.GetRequiredService<IMiddlewareReceiptJournalRepository>()));
        _actionJournalRepository = new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult(provider.GetRequiredService<IMiddlewareActionJournalRepository>()));
        _journalEsRepository = new AsyncLazy<IMiddlewareJournalESRepository>(() => Task.FromResult(provider.GetRequiredService<IMiddlewareJournalESRepository>()));
        _accountMasterDataRepository = new AsyncLazy<IMasterDataRepository<AccountMasterData>>(() => Task.FromResult(provider.GetRequiredService<IMasterDataRepository<AccountMasterData>>()));
        _outletMasterDataRepository = new AsyncLazy<IMasterDataRepository<OutletMasterData>>(() => Task.FromResult(provider.GetRequiredService<IMasterDataRepository<OutletMasterData>>()));
        _posSystemMasterDataRepository = new AsyncLazy<IMasterDataRepository<PosSystemMasterData>>(() => Task.FromResult(provider.GetRequiredService<IMasterDataRepository<PosSystemMasterData>>()));
        _agencyMasterDataRepository = new AsyncLazy<IMasterDataRepository<AgencyMasterData>>(() => Task.FromResult(provider.GetRequiredService<IMasterDataRepository<AgencyMasterData>>()));

        _initialized = Task.CompletedTask;
    }

    public Task Initialized => _initialized;

    public AsyncLazy<IConfigurationRepository> CreateConfigurationRepository() => _configurationRepository;

    public AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository() => _queueItemRepository;

    public AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository() => _receiptJournalRepository;

    public AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository() => _actionJournalRepository;

    public AsyncLazy<IMiddlewareJournalESRepository> CreateMiddlewareJournalESRepository() => _journalEsRepository;

    public AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository() => _accountMasterDataRepository;

    public AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository() => _outletMasterDataRepository;

    public AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository() => _posSystemMasterDataRepository;

    public AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository() => _agencyMasterDataRepository;

    private sealed class InMemoryJournalEsRepository : AbstractInMemoryRepository<Guid, ftJournalES>, IMiddlewareJournalESRepository
    {
        public InMemoryJournalEsRepository() : base(new List<ftJournalES>())
        {
        }

        protected override Guid GetIdForEntity(ftJournalES entity) => entity.ftJournalESId;

        protected override void EntityUpdated(ftJournalES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public new Task<IEnumerable<ftJournalES>> GetAsync() => base.GetAsync();

        public new Task<ftJournalES> GetAsync(Guid id) => base.GetAsync(id);

        public new Task InsertAsync(ftJournalES entity) => base.InsertAsync(entity);
    }
}
