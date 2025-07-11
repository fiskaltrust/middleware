using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IStorageProvider
{
    public Task Initialized { get; }
    public AsyncLazy<IConfigurationRepository> CreateConfigurationRepository();
    public AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository();
    public AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository();
    public AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository();
    public AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository();
    public AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository();
    public AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository();
    public AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository();
}
