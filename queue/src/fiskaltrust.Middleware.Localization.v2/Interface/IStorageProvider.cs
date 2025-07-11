using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IStorageProvider
{
    Task Initialized { get; }
    Lazy<Task<IConfigurationRepository>> ConfigurationRepository { get; }
    Lazy<Task<IMiddlewareQueueItemRepository>> MiddlewareQueueItemRepository { get; }
    Lazy<Task<IMiddlewareReceiptJournalRepository>> MiddlewareReceiptJournalRepository { get; }
    Lazy<Task<IMiddlewareActionJournalRepository>> MiddlewareActionJournalRepository { get; }
    Lazy<Task<IMasterDataRepository<AccountMasterData>>> AccountMasterDataRepository { get; }
    Lazy<Task<IMasterDataRepository<OutletMasterData>>> OutletMasterDataRepository { get; }
    Lazy<Task<IMasterDataRepository<PosSystemMasterData>>> PosSystemMasterDataRepository { get; }
    Lazy<Task<IMasterDataRepository<AgencyMasterData>>> AgencyMasterDataRepository { get; }
}
