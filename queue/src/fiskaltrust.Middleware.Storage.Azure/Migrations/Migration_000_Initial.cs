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
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_000_Initial(TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 0;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftJournalAT)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftJournalDE)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftJournalFR)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftJournalME)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftCashBox)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueue)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueueAT)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueueDE)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueueFR)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueueME)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftSignaturCreationUnitAT)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftSignaturCreationUnitDE)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftSignaturCreationUnitFR)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftSignaturCreationUnitME)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(FailedFinishTransaction)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(FailedStartTransaction)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(OpenTransaction)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(AccountMasterData)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(AgencyMasterData)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(OutletMasterData)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(PosSystemMasterData)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftActionJournal)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftQueueItem)));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(nameof(ftReceiptJournal)));
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
}
}