using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.Storage
{
    public interface IQueueStorageProvider : ILocalizedQueueStorageProvider
    {
        Task CreateActionJournalAsync(ftActionJournal actionJournal);
        Task CreateActionJournalAsync(string message, string type, Guid? queueItemId);
        Task FinishQueueItem(ftQueueItem queueItem, ReceiptResponse receiptResponse);
        Task<long> GetCurrentRow();
        Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data);
        Task<(List<Receipt>?, string? error)> GetReferencedReceiptsAsync(ReceiptRequest data);
        Task<long> IncrementQueueRow();
        Task<ftQueue> GetQueueAsync();
        Task<long> GetReceiptNumerator();
        Task<ftReceiptJournal> InsertReceiptJournal(ftQueueItem queueItem, ReceiptRequest receiptrequest);
        Task<ftQueueItem> ReserveNextQueueItem(ReceiptRequest receiptRequest);
    }

    public interface ILocalizedQueueStorageProvider
    {
        Task ActivateQueueAsync();
        Task DeactivateQueueAsync();
    }
}