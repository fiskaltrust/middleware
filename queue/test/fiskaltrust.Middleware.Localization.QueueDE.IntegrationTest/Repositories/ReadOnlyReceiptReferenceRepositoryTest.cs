using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Exports.Common.Helpers;
using fiskaltrust.Exports.Common.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueDE.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
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
            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach(var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            var resultlist = await receiptRef.ToAsyncEnumerable().ToListAsync();
            var sorted = expectedEntries.OrderBy(x => x.TimeStamp).ToList();

            for (var i = 1; i <= resultlist.Count(); i++)
            {
                var responseSource = JsonConvert.DeserializeObject<ReceiptResponse>(sorted[i-1].response);
                var responseTarget = JsonConvert.DeserializeObject<ReceiptResponse>(sorted[i].response);
                resultlist[i-1].RefReceiptId.Should().Be(responseSource.ftReceiptIdentification);
                resultlist[i-1].RefMoment.Should().Be(sorted[i - 1].cbReceiptMoment);
                resultlist[i-1].RefReceiptId = responseSource.ftReceiptIdentification;
                resultlist[i - 1].TargetQueueItemId = sorted[i].ftQueueItemId;
                resultlist[i - 1].SourceQueueItemId = sorted[i - 1].ftQueueItemId;
                resultlist[i - 1].TargetReceiptIdentification = responseTarget.ftReceiptIdentification;
                
            }
        }

        [Fact]
        public async Task GetReceiptReferenceAsync_OneWithPreviouseReceipt_ValidResult()
        {
            var receiptFixture = GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.response, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptResponse>())));

            var expectedEntries = new List<ftQueueItem>();

            var requ1 = receiptFixture.Create<ReceiptRequest>();
            requ1.cbPreviousReceiptReference = "";
            var ref1 = queueItemFixture.Create<ftQueueItem>();
            ref1.cbReceiptReference = "TestReference1";
            ref1.ftQueueRow = 1;
            ref1.request = JsonConvert.SerializeObject(requ1);
            expectedEntries.Add(ref1);

            await Task.Delay(1);
            var requ2 = receiptFixture.Create<ReceiptRequest>();
            requ2.cbPreviousReceiptReference = "TestReference1";
            var ref2 = queueItemFixture.Create<ftQueueItem>();
            ref2.cbReceiptReference = "TestReference2";
            ref2.ftQueueRow = 2;
            ref2.TimeStamp = DateTime.UtcNow.Ticks;
            ref2.request = JsonConvert.SerializeObject(requ2);
            expectedEntries.Add(ref2);

            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach (var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            receiptRef.Should().HaveCount(1);
            var responseSource = JsonConvert.DeserializeObject<ReceiptResponse>(ref1.response);
            var responseTarget = JsonConvert.DeserializeObject<ReceiptResponse>(ref2.response);
            receiptRef.First().RefReceiptId.Should().Be(responseSource.ftReceiptIdentification);
            receiptRef.First().RefMoment.Should().Be(ref1.cbReceiptMoment);
            receiptRef.First().RefReceiptId = responseSource.ftReceiptIdentification;
            receiptRef.First().TargetQueueItemId = ref2.ftQueueItemId;
            receiptRef.First().SourceQueueItemId = ref1.ftQueueItemId;
            receiptRef.First().TargetReceiptIdentification = responseTarget.ftReceiptIdentification;
        }


        [Fact]
        public async Task GetReceiptReferenceAsync_TwoWithPreviouseReceipt_ValidResult()
        {
            var receiptFixture = GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.response, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptResponse>())));

            var expectedEntries = new List<ftQueueItem>();

            var requ1 = receiptFixture.Create<ReceiptRequest>();
            requ1.cbPreviousReceiptReference = "";
            var ref1 = queueItemFixture.Create<ftQueueItem>();
            ref1.cbReceiptReference = "TestReference1";
            ref1.ftQueueRow = 1;
            ref1.request = JsonConvert.SerializeObject(requ1);
            expectedEntries.Add(ref1);

            await Task.Delay(1);
            var requ2 = receiptFixture.Create<ReceiptRequest>();
            requ2.cbPreviousReceiptReference = "TestReference1";
            var ref2 = queueItemFixture.Create<ftQueueItem>();
            ref2.cbReceiptReference = "TestReference2";
            ref2.ftQueueRow = 2;
            ref2.TimeStamp = DateTime.UtcNow.Ticks;
            ref2.request = JsonConvert.SerializeObject(requ2);
            expectedEntries.Add(ref2);

            await Task.Delay(1);
            var requ3 = receiptFixture.Create<ReceiptRequest>();
            requ3.cbPreviousReceiptReference = "";
            var ref3 = queueItemFixture.Create<ftQueueItem>();
            ref3.cbReceiptReference = "TestReference1";
            ref3.ftQueueRow = 1;
            ref3.request = JsonConvert.SerializeObject(requ1);
            expectedEntries.Add(ref3);

            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach (var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            receiptRef.Should().HaveCount(2);

            var responseref1 = JsonConvert.DeserializeObject<ReceiptResponse>(ref1.response);
            var responseref2 = JsonConvert.DeserializeObject<ReceiptResponse>(ref2.response);
            var responseref3 = JsonConvert.DeserializeObject<ReceiptResponse>(ref3.response);

            var resultlist = await receiptRef.ToAsyncEnumerable().ToListAsync();

            resultlist[0].RefMoment = ref3.cbReceiptMoment;
            resultlist[0].RefReceiptId = responseref3.ftReceiptIdentification;
            resultlist[0].TargetQueueItemId = ref1.ftQueueItemId;
            resultlist[0].SourceQueueItemId = ref3.ftQueueItemId;
            resultlist[0].TargetReceiptIdentification = responseref1.ftReceiptIdentification;

            resultlist[1].RefMoment = ref1.cbReceiptMoment;
            resultlist[1].RefReceiptId = responseref1.ftReceiptIdentification;
            resultlist[1].TargetQueueItemId = ref2.ftQueueItemId;
            resultlist[1].SourceQueueItemId = ref1.ftQueueItemId;
            resultlist[1].TargetReceiptIdentification = responseref2.ftReceiptIdentification;
        }

        [Fact]
        public async Task GetReceiptReferenceAsync_ThreeWithPreviouseReceipt_ValidResult()
        {
            var receiptFixture = GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.response, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptResponse>())));

            var expectedEntries = new List<ftQueueItem>();
            var rowcount = 0;

            var requ1 = receiptFixture.Create<ReceiptRequest>();
            requ1.cbPreviousReceiptReference = "";
            var ref1 = queueItemFixture.Create<ftQueueItem>();
            ref1.cbReceiptReference = "TestReference1";
            ref1.ftQueueRow = rowcount++;
            ref1.request = JsonConvert.SerializeObject(requ1);
            expectedEntries.Add(ref1);

            await Task.Delay(1);
            var requ2 = receiptFixture.Create<ReceiptRequest>();
            requ2.cbPreviousReceiptReference = "";
            var ref2 = queueItemFixture.Create<ftQueueItem>();
            ref2.cbReceiptReference = "TestReference1";
            ref2.ftQueueRow = rowcount++;
            ref2.request = JsonConvert.SerializeObject(requ2);
            expectedEntries.Add(ref2);

            await Task.Delay(1);
            var requ3 = receiptFixture.Create<ReceiptRequest>();
            requ3.cbPreviousReceiptReference = "TestReference1";
            var ref3 = queueItemFixture.Create<ftQueueItem>();
            ref3.cbReceiptReference = "TestReference2";
            ref3.ftQueueRow = rowcount++;
            ref3.TimeStamp = DateTime.UtcNow.Ticks;
            ref3.request = JsonConvert.SerializeObject(requ3);
            expectedEntries.Add(ref3);

            await Task.Delay(1);
            var requ4 = receiptFixture.Create<ReceiptRequest>();
            requ4.cbPreviousReceiptReference = "";
            var ref4 = queueItemFixture.Create<ftQueueItem>();
            ref4.cbReceiptReference = "TestReference1";
            ref4.ftQueueRow = rowcount++;
            ref4.request = JsonConvert.SerializeObject(requ4);
            expectedEntries.Add(ref4);

            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach (var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            receiptRef.Should().HaveCount(3);

            var responseref1 = JsonConvert.DeserializeObject<ReceiptResponse>(ref1.response);
            var responseref2 = JsonConvert.DeserializeObject<ReceiptResponse>(ref2.response);
            var responseref3 = JsonConvert.DeserializeObject<ReceiptResponse>(ref3.response);
            var responseref4 = JsonConvert.DeserializeObject<ReceiptResponse>(ref4.response);

            var resultlist = await receiptRef.ToAsyncEnumerable().ToListAsync();

            resultlist[0].RefMoment = ref1.cbReceiptMoment;
            resultlist[0].RefReceiptId = responseref1.ftReceiptIdentification;
            resultlist[0].TargetQueueItemId = ref2.ftQueueItemId;
            resultlist[0].SourceQueueItemId = ref1.ftQueueItemId;
            resultlist[0].TargetReceiptIdentification = responseref2.ftReceiptIdentification;

            resultlist[1].RefMoment = ref3.cbReceiptMoment;
            resultlist[1].RefReceiptId = responseref3.ftReceiptIdentification;
            resultlist[1].TargetQueueItemId = ref4.ftQueueItemId;
            resultlist[1].SourceQueueItemId = ref3.ftQueueItemId;
            resultlist[1].TargetReceiptIdentification = responseref4.ftReceiptIdentification;

            resultlist[2].RefMoment = ref2.cbReceiptMoment;
            resultlist[2].RefReceiptId = responseref2.ftReceiptIdentification;
            resultlist[2].TargetQueueItemId = ref3.ftQueueItemId;
            resultlist[2].SourceQueueItemId = ref2.ftQueueItemId;
            resultlist[2].TargetReceiptIdentification = responseref3.ftReceiptIdentification;
        }


        [Fact]
        public async Task GetReceiptReferenceAsync_ExternalReference_ValidResult()
        {
            var receiptFixture = GetFixture();
            receiptFixture.Customize<ReceiptRequest>(c => c.With(r => r.ftReceiptCase, 4919338172267102209));

            var queueItemFixture = GetFixture();
            queueItemFixture.Customize<ftQueueItem>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks).
            With(r => r.request, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptRequest>())).
            With(r => r.response, JsonConvert.SerializeObject(receiptFixture.Create<ReceiptResponse>())));

            var expectedEntries = new List<ftQueueItem>();

            var requ1 = receiptFixture.Create<ReceiptRequest>();
            requ1.cbPreviousReceiptReference = "";
            requ1.ftReceiptCaseData = "{\"RefType\": \"Transaktion\", \"RefMoment\": \"2020-08-21T12:12:32\", \"RefCashBoxIdentification\": \"other-test-cbi\", \"RefClosingNr\": 12, \"RefReceiptId\": \"ft123#IT123\"}";
            var ref1 = queueItemFixture.Create<ftQueueItem>();
            ref1.cbReceiptReference = "TestReference1";
            ref1.ftQueueRow = 1;
            ref1.request = JsonConvert.SerializeObject(requ1);
            expectedEntries.Add(ref1);

            var queueItemRepo = new InMemoryQueueItemRepository();
            foreach (var item in expectedEntries)
            {
                await queueItemRepo.InsertOrUpdateAsync(item);
            }

            var readonlyRepo = new ReadOnlyReceiptReferenceRepository(queueItemRepo, new Mock<IReadOnlyActionJournalRepository>().Object);
            var receiptRef = await readonlyRepo.GetReceiptReferenceAsync(DateTime.UtcNow.AddDays(-1).Ticks, DateTime.UtcNow.AddDays(1).Ticks);
            receiptRef.Should().HaveCount(1);
            var respTarget = JsonConvert.DeserializeObject<ReceiptResponse>(ref1.response);
            var requestTarget = JsonConvert.DeserializeObject<ReceiptRequest>(ref1.request);

            var receiptCaseData = SerializationHelper.GetReceiptCaseData(requestTarget);
            receiptRef.First().TargetReceiptIdentification = respTarget.ftReceiptIdentification;
            receiptRef.First().TargetReceiptCaseData = receiptCaseData;
            receiptRef.First().TargetQueueItemId = ref1.ftQueueItemId;
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
