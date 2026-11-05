using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters.Interfaces;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK
{
    internal class DSFinVKDailyClosingFormatter
    {
        private const string SCHEMA_DIR = "Schema";
        private const string INDEX_FILENAME = "index.xml";
        private const string GDPDU_FILENAME = "gdpdu-01-09-2004.dtd";
        private const string EOL = "\r\n";
        private readonly IEnumerable<IModuleFormatter> _moduleFormatters;

        public DSFinVKDailyClosingFormatter(ILogger logger, DSFinVKParameters parameters, MasterDataConfiguration fallbackMasterData)
        {
            _moduleFormatters = new List<IModuleFormatter>
            {
                new MasterDataModuleFormatter(logger, parameters, fallbackMasterData),
                new SingleRecordingsFormatter(logger, parameters),
                new CashpointClosingFormatter(logger, parameters)
            };
        }

        public async Task Process(IList<ReceiptEntry> receiptEntries, List<ReceiptReferencesGroupedData> receiptReferences, long zNumber, string targetDirectory, TseData tseData)
        {
            var dailyClosingJournal = receiptEntries.Last().ReceiptJournal;
            var dailyClosingQueuItem = receiptEntries.Last().QueueItem;
            targetDirectory = Path.Combine(targetDirectory, $"{dailyClosingJournal.ftReceiptMoment:yyyy-MM-dd}_{zNumber:D5}");

            foreach (var formatter in _moduleFormatters)
            {
                await formatter.ProcessAsync(targetDirectory, receiptEntries, new DailyClosingParameters { ZNumber = zNumber, DailyClosingMoment = dailyClosingQueuItem.cbReceiptMoment }, receiptReferences, tseData);
            }

            await AddSchemaFilesAsync(targetDirectory);
        }

        private async Task AddSchemaFilesAsync(string targetDirectory)
        {
            await ResourceHelper.WriteResourceToFileAsync(SCHEMA_DIR, INDEX_FILENAME, Path.Combine(targetDirectory, INDEX_FILENAME), EOL);
            await ResourceHelper.WriteResourceToFileAsync(SCHEMA_DIR, GDPDU_FILENAME, Path.Combine(targetDirectory, GDPDU_FILENAME), EOL);
        }
    }
}
