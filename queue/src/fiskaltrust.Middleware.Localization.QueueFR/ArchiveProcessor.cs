using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class ArchiveProcessor
    {
        private readonly ILogger<ArchiveProcessor> _logger;
        private readonly IMiddlewareRepository<ftQueueItem> _mwQueueItemRepository;
        private readonly IMiddlewareRepository<ftReceiptJournal> _mwReceiptJournalRepository;
        private readonly IReadOnlyReceiptJournalRepository _receiptJournalRepository;
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;

        public ArchiveProcessor(ILogger<ArchiveProcessor> logger, IMiddlewareRepository<ftQueueItem> mwQueueItemRepository, IMiddlewareRepository<ftReceiptJournal> mwReceiptJournalRepository,
            IReadOnlyReceiptJournalRepository receiptJournalRepository, IReadOnlyQueueItemRepository queueItemRepository)
        {
            _logger = logger;
            _mwQueueItemRepository = mwQueueItemRepository;
            _mwReceiptJournalRepository = mwReceiptJournalRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _queueItemRepository = queueItemRepository;
        }

        public async Task ExportArchiveDataAsync(string targetFile, ArchivePayload archivePayload, ftSignaturCreationUnitFR signatureCreationUnitFR)
        {
            var fromQueueItemId = archivePayload.FirstContainedReceiptQueueItemId;
            var toQueueItemId = archivePayload.LastContainedReceiptQueueItemId;
            if (!fromQueueItemId.HasValue || !toQueueItemId.HasValue)
            {
                _logger.LogDebug("Archive payload incomplete, skipping export.");
                return;
            }

            var fromQueueItem = await _queueItemRepository.GetAsync(fromQueueItemId.Value);
            var toQueueItem = await _queueItemRepository.GetAsync(toQueueItemId.Value);
            var toReceiptJournal = await _receiptJournalRepository.GetAsync(archivePayload.LastReceiptJournalId);

            var queueItems = _mwQueueItemRepository.GetByTimeStampRangeAsync(fromQueueItem.TimeStamp, toQueueItem.TimeStamp);
            var receiptJournals = _mwReceiptJournalRepository.GetByTimeStampRangeAsync(fromQueueItem.TimeStamp, toReceiptJournal.TimeStamp);

            var tempDirectory = Path.Combine(Path.GetDirectoryName(targetFile), "raw");
            try
            {
                CreateCertificationFile(tempDirectory, signatureCreationUnitFR);
                await CreateReceiptJournalsFileAsync(receiptJournals, tempDirectory);
                await CreateQueueItemsFileAsync(queueItems, receiptJournals, tempDirectory);

                ZipFile.CreateFromDirectory(tempDirectory, targetFile);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        private void CreateCertificationFile(string workingDirectory, ftSignaturCreationUnitFR signatureCreationUnitFR) 
            => File.WriteAllBytes(Path.Combine(workingDirectory, $"{signatureCreationUnitFR.CertificateSerialNumber}.cer"), Convert.FromBase64String(signatureCreationUnitFR.CertificateBase64));

        private async Task CreateQueueItemsFileAsync(IAsyncEnumerable<ftQueueItem> queueItems, IAsyncEnumerable<ftReceiptJournal> receiptJournals, string workingDirectory)
        {
            static async IAsyncEnumerable<ftQueueItem> FilterQueueItems(IAsyncEnumerable<ftQueueItem> queueItems, IAsyncEnumerable< ftReceiptJournal> ftReceiptJournals)
            {
                var ids = await ftReceiptJournals.Select(x => x.ftQueueItemId).ToListAsync();
                await foreach (var item in queueItems)
                {
                    if (ids.Contains(item.ftQueueItemId))
                    {
                        yield return item;
                    }
                }
            }
            
            var tempPath = Path.Combine(workingDirectory, "queueItems.csv");
            await CsvHelper.CreateQueueItemsCSVAsync(FilterQueueItems(queueItems, receiptJournals), tempPath);

            var hash = HashHelper.ComputeSHA256Base64Url(tempPath);
            File.Move(tempPath, $"queueItems.{hash}.csv");
        }

        private async Task CreateReceiptJournalsFileAsync(IAsyncEnumerable<ftReceiptJournal> receiptJournals, string workingDirectory)
        {
            var tempPath = Path.Combine(workingDirectory, "receiptJournals.csv");
            await CsvHelper.CreateReceiptJournalCSVAsync(receiptJournals, tempPath);

            var hash = HashHelper.ComputeSHA256Base64Url(tempPath);
            File.Move(tempPath, $"receiptJournals.{hash}.csv");
        }
    }
}
