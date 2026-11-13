using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
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
        public long? QueueRow { get; set; }
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

        /// <summary>
        /// Adds the copy and rejects one that is already journaled, as the other storages do: the
        /// journal is append-only, so an existing copy must never be overwritten.
        /// </summary>
        public override async Task InsertAsync(ftJournalFRCopyPayload storageEntity)
        {
            EntityUpdated(storageEntity);
            try
            {
                await _tableClient.AddEntityAsync(MapToAzureEntity(storageEntity)).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                throw new Exception("Entity with the same ID already exists.", ex);
            }
        }

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            var partitionKey = PartitionKeyOf(cbPreviousReceiptReference);
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalFRCopyPayload>(x => x.PartitionKey == partitionKey, select: new[] { nameof(ITableEntity.RowKey) });
            return await result.CountAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<ftJournalFRCopyPayload>> GetCopiesAsync(string copiedReceiptReference)
        {
            var partitionKey = PartitionKeyOf(copiedReceiptReference);
            var result = _tableClient.QueryAsync<AzureTableStorageFtJournalFRCopyPayload>(x => x.PartitionKey == partitionKey);
            return await result.Select(MapToStorageEntity).ToListAsync().ConfigureAwait(false);
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
                QueueRow = src.QueueRow,
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
                QueueRow = src.QueueRow,
            };
        }

        private const int MaxPartitionKeyLength = 1000;

        /// <summary>
        /// The partition of a copied reference. A receipt reference may hold characters Azure does not allow in a key, so it is
        /// Base64Url-encoded; one too long for the 1 KiB key limit of Azure Table Storage is keyed by its SHA-256 instead, prefixed so it
        /// cannot meet an encoded reference. A reference that fits keeps the key it always had.
        /// </summary>
        public static string PartitionKeyOf(string copiedReceiptReference)
        {
            var bytes = Encoding.UTF8.GetBytes(copiedReceiptReference ?? "");
            var key = ConversionHelper.ToBase64UrlString(bytes);
            if (key.Length <= MaxPartitionKeyLength)
            {
                return key;
            }

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return "sha256_" + ConversionHelper.ToBase64UrlString(sha256.ComputeHash(bytes));
        }
    }
}
