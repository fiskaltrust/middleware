using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IStorageProvider
{
    Task Initialized { get; }
    AsyncLazy<IConfigurationRepository> CreateConfigurationRepository();
    AsyncLazy<IMiddlewareQueueItemRepository> CreateMiddlewareQueueItemRepository();
    AsyncLazy<IMiddlewareReceiptJournalRepository> CreateMiddlewareReceiptJournalRepository();
    AsyncLazy<IMiddlewareActionJournalRepository> CreateMiddlewareActionJournalRepository();
    AsyncLazy<IMiddlewareRepository<ftJournalES>> CreateMiddlewareJournalESRepository();
    AsyncLazy<IMasterDataRepository<AccountMasterData>> CreateAccountMasterDataRepository();
    AsyncLazy<IMasterDataRepository<OutletMasterData>> CreateOutletMasterDataRepository();
    AsyncLazy<IMasterDataRepository<PosSystemMasterData>> CreatePosSystemMasterDataRepository();
    AsyncLazy<IMasterDataRepository<AgencyMasterData>> CreateAgencyMasterDataRepository();
}
