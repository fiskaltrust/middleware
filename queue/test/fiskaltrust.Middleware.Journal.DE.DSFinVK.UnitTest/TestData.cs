using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.ifPOS.v0;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest
{
    public static class TestData
    {
        public static HashSet<ReceiptReferencesGroupedData> ReceiptReferenceData => new HashSet<ReceiptReferencesGroupedData>();

        public static List<ftReceiptJournal> ReceiptJournals => new List<ftReceiptJournal>
        {
            new ftReceiptJournal
            {
                ftQueueItemId = Guid.Parse("8163953f-24cf-4b25-9c71-e2e1b07eb310"),
                ftReceiptNumber = 1
            },
            new ftReceiptJournal
            {
                ftQueueItemId = Guid.Parse("52a1c806-ea86-46fb-8d5b-bbb298fdb98b"),
                ftReceiptNumber = 2
            },
            new ftReceiptJournal
            {
                ftQueueItemId = Guid.Parse("28c4f206-f8bf-4f8e-996d-bcd8b7e8835d"),
                ftReceiptNumber = 3
            },
            new ftReceiptJournal
            {
                ftQueueItemId = Guid.Parse("91e9de14-88c2-4dd7-8a61-5f83329c31ad"),
                ftReceiptNumber = 4
            },
            new ftReceiptJournal
            {
                ftQueueItemId = Guid.Parse("01d3e6ba-493f-4c57-ac2a-006d1944cdfd"),
                ftReceiptNumber = 5
            }
        };
        
        public static List<ftQueueItem> QueueItemsWithoutDailyClosings => new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("8163953f-24cf-4b25-9c71-e2e1b07eb310"),
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001 }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000 })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("52a1c806-ea86-46fb-8d5b-bbb298fdb98b"),
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001 }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000 })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("28c4f206-f8bf-4f8e-996d-bcd8b7e8835d"),
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001 }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000 })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("91e9de14-88c2-4dd7-8a61-5f83329c31ad"),
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001 }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000 })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("01d3e6ba-493f-4c57-ac2a-006d1944cdfd"),
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001 }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000 })
            }
        };

        public static List<ftQueueItem> QueueItemsWithDailyClosings => new List<ftQueueItem>
        {
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("8163953f-24cf-4b25-9c71-e2e1b07eb310"),
                ftQueueRow = 1,
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001, cbReceiptMoment = new DateTime(2020, 1, 1) }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000, ftReceiptMoment = new DateTime(2020, 1, 1)})
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("52a1c806-ea86-46fb-8d5b-bbb298fdb98b"),
                ftQueueRow = 2,
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000007, cbReceiptMoment = new DateTime(2020, 1, 1) }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000, ftReceiptMoment = new DateTime(2020, 1, 1) })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("28c4f206-f8bf-4f8e-996d-bcd8b7e8835d"),
                ftQueueRow = 3,
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001, cbReceiptMoment = new DateTime(2020, 1, 1) }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000, ftReceiptMoment = new DateTime(2020, 1, 1) })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("91e9de14-88c2-4dd7-8a61-5f83329c31ad"),
                ftQueueRow = 4,
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000007, cbReceiptMoment = new DateTime(2020, 1, 2) }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000, ftReceiptMoment = new DateTime(2020, 1, 2) })
            },
            new ftQueueItem
            {
                ftQueueItemId = Guid.Parse("01d3e6ba-493f-4c57-ac2a-006d1944cdfd"),
                ftQueueRow = 5,
                request = JsonConvert.SerializeObject(new ReceiptRequest{ ftReceiptCase = 0x4445000000000001, cbReceiptMoment = new DateTime(2020, 1, 2) }),
                response = JsonConvert.SerializeObject(new ReceiptResponse{ ftState = 0x4445000000000000, ftReceiptMoment = new DateTime(2020, 1, 2) })
            }
        };
    }
}
