using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public interface IStorageProvider
{
    Task Initialized { get; }
    IConfigurationRepository GetConfigurationRepository();
    IMiddlewareQueueItemRepository GetMiddlewareQueueItemRepository();
    IMiddlewareReceiptJournalRepository GetMiddlewareReceiptJournalRepository();
    IMiddlewareActionJournalRepository GetMiddlewareActionJournalRepository();

    IMasterDataRepository<AccountMasterData> GetAccountMasterDataRepository();
    IMasterDataRepository<OutletMasterData> GetOutletMasterDataRepository();
    IMasterDataRepository<PosSystemMasterData> GetPosSystemMasterDataRepository();
    IMasterDataRepository<AgencyMasterData> GetAgencyMasterDataRepository();
}