using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.FormattableString;
using fiskaltrust.storage.V0;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public class ArchiveCsvHelperTests
    {
        [Fact]
        public async Task CreateReceiptJournalCSVAsync_ShouldCreateCSVFileWithCorrectHeaderAndData()
        {
            // Arrange
            var receiptJournals = new List<ftReceiptJournal>
            {
                new ftReceiptJournal
                {
                    ftQueueId = Guid.NewGuid(),
                    ftQueueItemId = Guid.NewGuid(),
                    ftReceiptHash = "hash1",
                    ftReceiptJournalId = Guid.NewGuid(),
                    ftReceiptMoment = DateTime.Now,
                    ftReceiptNumber = 123,
                    ftReceiptTotal = 10.0m,
                    TimeStamp = DateTime.Now.Ticks,
                },
                new ftReceiptJournal
                {
                    ftQueueId = Guid.NewGuid(),
                    ftQueueItemId = Guid.NewGuid(),
                    ftReceiptHash = "hash2",
                    ftReceiptJournalId = Guid.NewGuid(),
                    ftReceiptMoment = DateTime.Now,
                    ftReceiptNumber = 456,
                    ftReceiptTotal = 20.0m,
                    TimeStamp = DateTime.Now.Ticks
                }
            };

            var targetFile = "receiptJournal.csv";

            // Act
            await ArchiveCsvHelper.CreateReceiptJournalCSVAsync(receiptJournals.ToAsyncEnumerable(), targetFile);

            // Assert
            Assert.True(File.Exists(targetFile));

            var lines = File.ReadAllLines(targetFile);
            Assert.Equal(3, lines.Length); // Header + 2 data rows

            var header = lines[0].Split(',');
            Assert.Equal(8, header.Length); // Number of columns in the header
            Assert.Equal("ftQueueId", header[0]);
            Assert.Equal("ftQueueItemId", header[1]);
            Assert.Equal("ftReceiptHash", header[2]);
            Assert.Equal("ftReceiptJournalId", header[3]);
            Assert.Equal("ftReceiptMoment", header[4]);
            Assert.Equal("ftReceiptNumber", header[5]);
            Assert.Equal("ftReceiptTotal", header[6]);
            Assert.Equal("TimeStamp", header[7]);

            var dataRow1 = lines[1].Split(',');
            Assert.Equal(8, dataRow1.Length); // Number of columns in the data row
            Assert.Equal(receiptJournals[0].ftQueueId.ToString(), dataRow1[0]);
            Assert.Equal(receiptJournals[0].ftQueueItemId.ToString(), dataRow1[1]);
            Assert.Equal("hash1", dataRow1[2]);
            Assert.Equal(receiptJournals[0].ftReceiptJournalId.ToString(), dataRow1[3]);
            Assert.Equal(Invariant($"{receiptJournals[0].ftReceiptMoment}"), dataRow1[4]);
            Assert.Equal(receiptJournals[0].ftReceiptNumber.ToString(), dataRow1[5]);
            Assert.Equal(Invariant($"{receiptJournals[0].ftReceiptTotal}"), dataRow1[6]);
            Assert.Equal(receiptJournals[0].TimeStamp.ToString(), dataRow1[7]);


            var dataRow2 = lines[2].Split(',');
            Assert.Equal(8, dataRow2.Length); // Number of columns in the data row
            Assert.Equal(receiptJournals[1].ftQueueId.ToString(), dataRow2[0]);
            Assert.Equal(receiptJournals[1].ftQueueItemId.ToString(), dataRow2[1]);
            Assert.Equal("hash2", dataRow2[2]);
            Assert.Equal(receiptJournals[1].ftReceiptJournalId.ToString(), dataRow2[3]);
            Assert.Equal(Invariant($"{receiptJournals[1].ftReceiptMoment}"), dataRow2[4]);
            Assert.Equal(receiptJournals[1].ftReceiptNumber.ToString(), dataRow2[5]);
            Assert.Equal(Invariant($"{receiptJournals[1].ftReceiptTotal}"), dataRow2[6]);
            Assert.Equal(receiptJournals[1].TimeStamp.ToString(), dataRow2[7]);
        }

        [Fact]
        public async Task CreateQueueItemsCSVAsync_ShouldCreateCSVFileWithCorrectHeaderAndData()
        {
            // Arrange
            var queueItems = new List<ftQueueItem>
            {
                new ftQueueItem
                {
                    cbReceiptMoment = DateTime.Now,
                    cbReceiptReference = "ref1",
                    cbTerminalID = "terminal1",
                    country = "country1",
                    ftDoneMoment = DateTime.Now,
                    ftQueueId = Guid.NewGuid(),
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = DateTime.Now,
                    ftQueueRow = 1,
                    ftQueueTimeout = 10,
                    ftWorkMoment = DateTime.Now,
                    request = "request1",
                    requestHash = "hash1",
                    response = "response1",
                    responseHash = "hash1",
                    TimeStamp = DateTime.Now.Ticks,
                    version = "1.0"
                },
                new ftQueueItem
                {
                    cbReceiptMoment = DateTime.Now,
                    cbReceiptReference = "ref2",
                    cbTerminalID = "terminal2",
                    country = "country2",
                    ftDoneMoment = DateTime.Now,
                    ftQueueId = Guid.NewGuid(),
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueMoment = DateTime.Now,
                    ftQueueRow = 2,
                    ftQueueTimeout =20,
                    ftWorkMoment = DateTime.Now,
                    request = "request2",
                    requestHash = "hash2",
                    response = "response2",
                    responseHash = "hash2",
                    TimeStamp = DateTime.Now.Ticks,
                    version = "2.0"
                }
            };

            var targetFile = "queueItems.csv";

            // Act
            await ArchiveCsvHelper.CreateQueueItemsCSVAsync(queueItems.ToAsyncEnumerable(), targetFile);

            // Assert
            Assert.True(File.Exists(targetFile));

            var lines = File.ReadAllLines(targetFile);
            Assert.Equal(3, lines.Length); // Header + 2 data rows

            var header = lines[0].Split(',');
            Assert.Equal(17, header.Length); // Number of columns in the header
            Assert.Equal("cbReceiptMoment", header[0]);
            Assert.Equal("cbReceiptReference", header[1]);
            Assert.Equal("cbTerminalID", header[2]);
            Assert.Equal("country", header[3]);
            Assert.Equal("ftDoneMoment", header[4]);
            Assert.Equal("ftQueueId", header[5]);
            Assert.Equal("ftQueueItemId", header[6]);
            Assert.Equal("ftQueueMoment", header[7]);
            Assert.Equal("ftQueueRow", header[8]);
            Assert.Equal("ftQueueTimeout", header[9]);
            Assert.Equal("ftWorkMoment", header[10]);
            Assert.Equal("request", header[11]);
            Assert.Equal("requestHash", header[12]);
            Assert.Equal("response", header[13]);
            Assert.Equal("responseHash", header[14]);
            Assert.Equal("TimeStamp", header[15]);
            Assert.Equal("version", header[16]);

            var dataRow1 = lines[1].Split(',');
            Assert.Equal(17, dataRow1.Length); // Number of columns in the data row
            Assert.Equal(Invariant($"{queueItems[0].cbReceiptMoment}"), dataRow1[0]);
            Assert.Equal(queueItems[0].cbReceiptReference, dataRow1[1]);
            Assert.Equal(queueItems[0].cbTerminalID, dataRow1[2]);
            Assert.Equal(queueItems[0].country, dataRow1[3]);
            Assert.Equal(Invariant($"{queueItems[0].ftDoneMoment}"), dataRow1[4]);
            Assert.Equal(queueItems[0].ftQueueId.ToString(), dataRow1[5]);
            Assert.Equal(queueItems[0].ftQueueItemId.ToString(), dataRow1[6]);
            Assert.Equal(Invariant($"{queueItems[0].ftQueueMoment}"), dataRow1[7]);
            Assert.Equal(queueItems[0].ftQueueRow.ToString(), dataRow1[8]);
            Assert.Equal(queueItems[0].ftQueueTimeout.ToString(), dataRow1[9]);
            Assert.Equal(Invariant($"{queueItems[0].ftWorkMoment}"), dataRow1[10]);
            Assert.Equal(queueItems[0].request, dataRow1[11].Replace("\"", ""));
            Assert.Equal(queueItems[0].requestHash, dataRow1[12].Replace("\"", ""));
            Assert.Equal(queueItems[0].response, dataRow1[13].Replace("\"", ""));
            Assert.Equal(queueItems[0].responseHash, dataRow1[14].Replace("\"", ""));
            Assert.Equal(queueItems[0].TimeStamp.ToString(), dataRow1[15]);
            Assert.Equal(queueItems[0].version, dataRow1[16]);

            var dataRow2 = lines[2].Split(',');
            Assert.Equal(17, dataRow2.Length); // Number of columns in the data row
            Assert.Equal(Invariant($"{queueItems[1].cbReceiptMoment}"), dataRow2[0]);
            Assert.Equal(queueItems[1].cbReceiptReference, dataRow2[1]);
            Assert.Equal(queueItems[1].cbTerminalID, dataRow2[2]);
            Assert.Equal(queueItems[1].country, dataRow2[3]);
            Assert.Equal(Invariant($"{queueItems[1].ftDoneMoment}"), dataRow2[4]);
            Assert.Equal(queueItems[1].ftQueueId.ToString(), dataRow2[5]);
            Assert.Equal(queueItems[1].ftQueueItemId.ToString(), dataRow2[6]);
            Assert.Equal(Invariant($"{queueItems[1].ftQueueMoment}"), dataRow2[7]);
            Assert.Equal(queueItems[1].ftQueueRow.ToString(), dataRow2[8]);
            Assert.Equal(queueItems[1].ftQueueTimeout.ToString(), dataRow2[9]);
            Assert.Equal(Invariant($"{queueItems[1].ftWorkMoment}"), dataRow2[10]);
            Assert.Equal(queueItems[1].request, dataRow2[11].Replace("\"", ""));
            Assert.Equal(queueItems[1].requestHash, dataRow2[12].Replace("\"", ""));
            Assert.Equal(queueItems[1].response, dataRow2[13].Replace("\"", ""));
            Assert.Equal(queueItems[1].responseHash, dataRow2[14].Replace("\"", ""));
            Assert.Equal(queueItems[1].TimeStamp.ToString(), dataRow2[15]);
            Assert.Equal(queueItems[1].version, dataRow2[16]);
        }
    }
}
