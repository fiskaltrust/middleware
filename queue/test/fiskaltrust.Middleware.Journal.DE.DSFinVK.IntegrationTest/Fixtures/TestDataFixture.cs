using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest
{
    public class TestDataFixture
    {
        public IList<ReceiptEntry> InfoOrderWithPosReceipt =>
            new List<ReceiptEntry>
            {
                new ReceiptEntry
                {
                    ReceiptJournal = new ftReceiptJournal
                    {
                        ftQueueItemId = Guid.Parse("40805f8d-3d00-452c-8838-1ac72e5daf29"),
                        ftReceiptNumber = 1
                    },
                    ReceiptRequest = JsonTestData.Requests.InfoOrder,
                    ReceiptResponse = JsonTestData.Responses.InfoOrder,
                    QueueItem = new ftQueueItem
                    {
                        ftQueueItemId = Guid.Parse("40805f8d-3d00-452c-8838-1ac72e5daf29"),
                        cbReceiptMoment = new DateTime(2021, 01, 19, 12, 13, 0),
                        cbReceiptReference = "R1",
                        cbTerminalID = "T1",
                    },
                },
                new ReceiptEntry
                {
                    ReceiptJournal = new ftReceiptJournal
                    {
                        ftQueueItemId = Guid.Parse("bb66d5d4-629c-4298-aad4-033423e1ea44"),
                        ftReceiptNumber = 2
                    },
                    ReceiptRequest = JsonTestData.Requests.PosReceipt,
                    ReceiptResponse = JsonTestData.Responses.PosReceipt,
                    QueueItem = new ftQueueItem
                    {
                        ftQueueItemId = Guid.Parse("bb66d5d4-629c-4298-aad4-033423e1ea44"),
                        cbReceiptMoment = new DateTime(2021, 01, 19, 14, 55, 0),
                        cbReceiptReference = "R1",
                        cbTerminalID = "T1",
                    },
                },
            };

        public IList<ReceiptEntry> DailyClosingReceipt =>
            new List<ReceiptEntry>
            {
                new ReceiptEntry
                {
                    ReceiptJournal = new ftReceiptJournal
                    {
                        ftQueueItemId = Guid.Parse("40805f8d-3d00-452c-8838-1ac72e5daf29"),
                        ftReceiptNumber = 1
                    },
                    ReceiptRequest = JsonTestData.Requests.DailyClosingReceipt,
                    ReceiptResponse = JsonTestData.Responses.DailyClosingReceipt,
                    QueueItem = new ftQueueItem
                    {
                        ftQueueItemId = Guid.Parse("40805f8d-3d00-452c-8838-1ac72e5daf29"),
                        cbReceiptMoment = new DateTime(2021, 01, 19, 12, 13, 0),
                        cbReceiptReference = "D1",
                        cbTerminalID = "T1",
                    },
                }
            };
    }
}