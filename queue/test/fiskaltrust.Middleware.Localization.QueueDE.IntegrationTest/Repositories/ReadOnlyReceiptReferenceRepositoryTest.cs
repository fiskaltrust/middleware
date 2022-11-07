using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest.Repositories
{
    public class ReadOnlyReceiptReferenceRepositoryTest
    {

        [Fact]
        public async Task GetReceiptReferenceAsync_MultipleSameRefs_QueuedRerenceData()
        {
            var queueRow = 1;
            var receiptFixture = GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));
            var queueItemFixture = GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.cbReceiptReference, "TestReference2").
            With(r => r.response, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptResponse>())));


            var ref2 = queueItemFixture.Create<ftQueueItem>();
            ref2.cbReceiptReference = "TestReference1";
            ref2.ftQueueRow = queueRow;
            ref2.TimeStamp = DateTime.UtcNow.Ticks;
            await Task.Delay(1);
            var expectedEntries = queueItemFixture.CreateMany<ftQueueItem>(4).ToList();
            foreach (var item in expectedEntries)
            {
                item.TimeStamp = DateTime.UtcNow.Ticks;
                item.ftQueueRow = queueRow++;
                await Task.Delay(1);
            }
            expectedEntries.Add(ref2);
            var request = receiptFixture.Create<ReceiptRequest>();
            request.cbPreviousReceiptReference = "TestReference1";
            expectedEntries[3].request = JsonConvert.SerializeObject(request);

            var ref3 = queueItemFixture.Create<ftQueueItem>();
            ref3.cbReceiptReference = "TestReference3";
            ref3.ftQueueRow = queueRow++;
            expectedEntries.Add(ref3);

            var requ4 = receiptFixture.Create<ReceiptRequest>();
            requ4.cbPreviousReceiptReference = "TestReference3";
            var ref4 = queueItemFixture.Create<ftQueueItem>();
            ref4.cbReceiptReference = "TestReference4";
            ref4.ftQueueRow = queueRow++;
            ref4.request = JsonConvert.SerializeObject(requ4);
            expectedEntries.Add(ref4);

            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach(var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            var sorted = expectedEntries.OrderBy(x => x.TimeStamp).ToList();

            for (var i = 0; i < sorted.Count(); i++)
            {
                if (sorted[i].cbReceiptReference == "TestReference2" && i > 0)
                {
                    var referenceData = receiptRef.Where(x => x.SourceQueueItemId == sorted[i - 1].ftQueueItemId).FirstOrDefault();
                    referenceData.TargetQueueItemId.Should().Be(sorted[i].ftQueueItemId);

                }
                else if (sorted[i].cbReceiptReference == "TestReference1")
                {
                    var referenceData = receiptRef.Where(x => x.SourceQueueItemId == sorted[i].ftQueueItemId).FirstOrDefault();
                    referenceData.TargetQueueItemId.Should().Be(expectedEntries[3].ftQueueItemId);
                }
                else if (sorted[i].cbReceiptReference == "TestReference3")
                {
                    var referenceData = receiptRef.Where(x => x.SourceQueueItemId == sorted[i].ftQueueItemId).FirstOrDefault();
                    referenceData.TargetQueueItemId.Should().Be(ref4.ftQueueItemId);
                }
                else if (sorted[i].cbReceiptReference == "TestReference4")
                {
                    var referenceData = receiptRef.Where(x => x.SourceQueueItemId == sorted[i].ftQueueItemId).FirstOrDefault();
                    referenceData.Should().BeNull();
                }
            }
        }

        private static Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Inject(DateTime.UtcNow);
            fixture.Customize<decimal>(c => c.FromFactory<int>(i => i * 1.33M));
            fixture.Customize<ftQueueItem>(c => c.With(r => r.request, JsonConvert.SerializeObject(fixture.Create<ReceiptRequest>())));
            return fixture;
        }
    }
}
