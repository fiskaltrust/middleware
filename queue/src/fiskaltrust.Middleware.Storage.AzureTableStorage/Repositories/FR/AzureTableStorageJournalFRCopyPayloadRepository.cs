using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Helpers;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR
{
    public class AzureTableStorageFtJournalFRCopyPayload : BaseTableEntity
    {
        public Guid QueueId { get; set; }
        public string CashBoxIdentification { get; set; }
        public string Siret { get; set; }
        public string ReceiptId { get; set; }
        public DateTime ReceiptMoment { get; set; }
        public Guid QueueItemId { get; set; }
        public string CopiedReceiptReference { get; set; }
        public string CertificateSerialNumber { get; set; }
        public long TimeStamp { get; set; }
    }

    /// <summary>
    /// One row per issued copy. The copies of a receipt share a partition, keyed by the copied
    /// receipt reference, so counting them is a single-partition query (market-fr#235).
    /// </summary>
    public class AzureTableStorageJournalFRCopyPayloadRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtJournalFRCopyPayload, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public AzureTableStorageJournalFRCopyPayloadRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "JournalFRCopyPayload";

        protected override void EntityUpdated(ftJournalFRCopyPayload entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            var partitionKey = PartitionKeyOf(cbPreviousReceiptReference);
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalFRCopyPayload>(x => x.PartitionKey == partitionKey, select: new[] { nameof(ITableEntity.RowKey) });
            return await result.CountAsync().ConfigureAwait(false);
        }

        protected override AzureTableStorageFtJournalFRCopyPayload MapToAzureEntity(ftJournalFRCopyPayload src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtJournalFRCopyPayload
            {
                PartitionKey = PartitionKeyOf(src.CopiedReceiptReference),
                RowKey = src.QueueItemId.ToString(),
                QueueId = src.QueueId,
                CashBoxIdentification = src.CashBoxIdentification,
                Siret = src.Siret,
                ReceiptId = src.ReceiptId,
                ReceiptMoment = src.ReceiptMoment.ToUniversalTime(),
                QueueItemId = src.QueueItemId,
                CopiedReceiptReference = src.CopiedReceiptReference,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp,
            };
        }

        protected override ftJournalFRCopyPayload MapToStorageEntity(AzureTableStorageFtJournalFRCopyPayload src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftJournalFRCopyPayload
            {
                QueueId = src.QueueId,
                CashBoxIdentification = src.CashBoxIdentification,
                Siret = src.Siret,
                ReceiptId = src.ReceiptId,
                ReceiptMoment = src.ReceiptMoment,
                QueueItemId = src.QueueItemId,
                CopiedReceiptReference = src.CopiedReceiptReference,
                CertificateSerialNumber = src.CertificateSerialNumber,
                TimeStamp = src.TimeStamp,
            };
        }

        // A receipt reference may hold characters Azure does not allow in a key.
        private static string PartitionKeyOf(string copiedReceiptReference) => ConversionHelper.ToBase64UrlString(Encoding.UTF8.GetBytes(copiedReceiptReference ?? ""));
    }
}
