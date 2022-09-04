using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using static System.FormattableString;

namespace fiskaltrust.Middleware.Localization.QueueFR.Helpers
{
    public static class ArchiveCsvHelper
    {
        private const string SEPERATOR = ",";

        public static async Task CreateReceiptJournalCSVAsync(IAsyncEnumerable<ftReceiptJournal> receiptJournals, string targetFile)
        {
            var header = new string[]
            {
                nameof(ftReceiptJournal.ftQueueId),
                nameof(ftReceiptJournal.ftQueueItemId),
                nameof(ftReceiptJournal.ftReceiptHash),
                nameof(ftReceiptJournal.ftReceiptJournalId),
                nameof(ftReceiptJournal.ftReceiptMoment),
                nameof(ftReceiptJournal.ftReceiptNumber),
                nameof(ftReceiptJournal.ftReceiptTotal),
                nameof(ftReceiptJournal.TimeStamp)
            };

            using var writer = File.CreateText(targetFile);
            await writer.WriteLineAsync(string.Join(SEPERATOR, header));

            await foreach (var item in receiptJournals)
            {
                var cells = new string[]
                {
                    Invariant($"{item.ftQueueId}"),
                    Invariant($"{item.ftQueueItemId}"),
                    item.ftReceiptHash,
                    Invariant($"{item.ftReceiptJournalId}"),
                    Invariant($"{item.ftReceiptMoment}"),
                    Invariant($"{item.ftReceiptNumber}"),
                    Invariant($"{item.ftReceiptTotal}"),
                    Invariant($"{item.TimeStamp}")
                };

                await writer.WriteLineAsync(string.Join(SEPERATOR, cells));
            }
        }

        public static async Task CreateQueueItemsCSVAsync(IAsyncEnumerable<ftQueueItem> queueItems, string targetFile)
        {
            var header = new string[]
            {
                nameof(ftQueueItem.cbReceiptMoment),
                nameof(ftQueueItem.cbReceiptReference),
                nameof(ftQueueItem.cbTerminalID),
                nameof(ftQueueItem.country),
                nameof(ftQueueItem.ftDoneMoment),
                nameof(ftQueueItem.ftQueueId),
                nameof(ftQueueItem.ftQueueItemId),
                nameof(ftQueueItem.ftQueueMoment),
                nameof(ftQueueItem.ftQueueRow),
                nameof(ftQueueItem.ftQueueTimeout),
                nameof(ftQueueItem.ftWorkMoment),
                nameof(ftQueueItem.request),
                nameof(ftQueueItem.requestHash),
                nameof(ftQueueItem.response),
                nameof(ftQueueItem.responseHash),
                nameof(ftQueueItem.TimeStamp),
                nameof(ftQueueItem.version)
            };

            using var writer = File.CreateText(targetFile);
            await writer.WriteLineAsync(string.Join(SEPERATOR, header));

            await foreach (var item in queueItems)
            {
                var cells = new string[]
                {
                    Invariant($"{item.cbReceiptMoment}"),
                    item.cbReceiptReference,
                    item.cbTerminalID,
                    item.country,
                    Invariant($"{item.ftDoneMoment}"),
                    Invariant($"{item.ftQueueId}"),
                    Invariant($"{item.ftQueueItemId}"),
                    Invariant($"{item.ftQueueMoment}"),
                    Invariant($"{item.ftQueueRow}"),
                    Invariant($"{item.ftQueueTimeout}"),
                    Invariant($"{item.ftWorkMoment}"),
                    Invariant($"\"{EscapeJson(item.request)}\""),
                    item.requestHash,
                    Invariant($"\"{EscapeJson(item.response)}\""),
                    item.responseHash,
                    Invariant($"{item.TimeStamp}"),
                    item.version
                };

                await writer.WriteLineAsync(string.Join(SEPERATOR, cells));
            }
        }

        private static string EscapeJson(string str) => str.Replace("\"", "\"\"");
    }
}
