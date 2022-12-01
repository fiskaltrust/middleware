using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.Migrations
{
    public class Migration_000_Initial : IAzureStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;

        public Migration_000_Initial(TableServiceClient tableServiceClient) => _tableServiceClient = tableServiceClient;

        public int Version => 0;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftJournalAT));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftJournalDE));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftJournalFR));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftJournalME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftCashBox));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueue));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueueAT));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueueDE));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueueFR));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueueME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftSignaturCreationUnitAT));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftSignaturCreationUnitDE));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftSignaturCreationUnitFR));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftSignaturCreationUnitME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(FailedFinishTransaction));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(FailedStartTransaction));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(OpenTransaction));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(AccountMasterData));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(AgencyMasterData));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(OutletMasterData));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(PosSystemMasterData));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftActionJournal));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftQueueItem));
            await _tableServiceClient.CreateTableIfNotExistsAsync(nameof(ftReceiptJournal));
        }
    }
}