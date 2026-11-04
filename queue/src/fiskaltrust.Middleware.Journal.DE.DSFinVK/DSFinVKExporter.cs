using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Journal.DE.Common.Repositories;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK
{
    public class DSFinVKExporter
    {
        private readonly ILogger _logger;
        private readonly IDailyClosingRepository _dailyClosingRepository;
        private readonly storage.V0.IReadOnlyReceiptJournalRepository _receiptJournalRepository;
        private readonly storage.V0.IReadOnlyQueueItemRepository _queueItemRepository;
        private readonly IReadOnlyReceiptReferenceRepository _receiptReferenceRepository;
        private readonly storage.V0.IReadOnlyMasterDataConfiguration _fallbackMasterDataConfig;

        public DSFinVKExporter(ILogger logger, storage.V0.IReadOnlyReceiptJournalRepository receiptJournalRepository, storage.V0.IReadOnlyQueueItemRepository queueItemRepository,
                IReadOnlyReceiptReferenceRepository receiptReferenceRepository, IDailyClosingRepository dailyClosingRepository, storage.V0.IReadOnlyMasterDataConfiguration fallbackMasterDataConfig)
        {
            _logger = logger;
            _dailyClosingRepository = dailyClosingRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _queueItemRepository = queueItemRepository;
            _receiptReferenceRepository = receiptReferenceRepository;
            _fallbackMasterDataConfig = fallbackMasterDataConfig;
        }

        public async Task ExportAsync(DSFinVKParameters parameters)
        {
            var tseDataScu = new TseData
            {
                PublicKeyBase64 = parameters.PublicKeyBase64,
                SignAlgorithm = parameters.SignAlgorithm,
                TimeFormat = parameters.TimeFormat
            };
            var dailyClosingSplitter = new DailyClosingSplitter(_receiptJournalRepository, _queueItemRepository, _receiptReferenceRepository, _dailyClosingRepository, _logger, tseDataScu);

            var fallbackMasterData = _fallbackMasterDataConfig != null ? await _fallbackMasterDataConfig.GetMasterDataConfiguration() : null;
            var formatter = new DSFinVKDailyClosingFormatter(_logger, parameters, fallbackMasterData);
            var receiptsByDailyClosing = dailyClosingSplitter.SplitByDailyClosingReceiptAsync(parameters.FirstZNumber, parameters.From, parameters.To, parameters.SkipDuplicatedReceipts);


            await foreach (var (receiptEntries, receiptReference, zNumber, tseData) in receiptsByDailyClosing)
            {
                try
                {
                    _logger.LogInformation($"Creating DSfinV-K for receipts before {receiptEntries.Last().ReceiptJournal.ftReceiptMoment}.");

                    await formatter.Process(receiptEntries, receiptReference, zNumber, parameters.TargetDirectory, tseData);
                    _logger.LogInformation($"Finished creating DSfinV-K for receipts before {receiptEntries.Last().ReceiptJournal.ftReceiptMoment}.");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "An error occured while generating the DSFinV-K export.");
                }
            }
        }
    }
}
