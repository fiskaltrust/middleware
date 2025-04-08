using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using Moq;
using Newtonsoft.Json;
using Xunit;
using FluentAssertions;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest
{
    public class BaseStorageBootStrapperTests
    {
        private class TestableBaseStorageBootStrapper : BaseStorageBootStrapper
        {
            public new Task PopulateFtJournalFRCopyPayloadTableAsync(IJournalFRCopyPayloadRepository journalFRCopyPayloadRepository, IMiddlewareJournalFRRepository journalFRRepository)
            {
                return base.PopulateFtJournalFRCopyPayloadTableAsync(journalFRCopyPayloadRepository, journalFRRepository);
            }
        }

        [Fact]
        public void ftJournalFRCopyPayload_ShouldSerializeCorrectly()
        {
            var copyPayload = new CopyPayload
            {
                CashBoxIdentification = "sadf",
                CertificateSerialNumber = "1234567890",
                CopiedReceiptReference = "1234567890",
                QueueId = Guid.NewGuid(),
                QueueItemId = Guid.NewGuid(),
                ReceiptId = "sdfas;dlfkjas",
                ReceiptMoment = DateTime.Now,
                Siret = "asdlfajsdofj"
            };
            var jCopyPayload = copyPayload.ToJournalFRCopyPayload();
            
            var copyPayloadSer = JsonConvert.SerializeObject(copyPayload);
            var jCopyPayloadSer = JsonConvert.SerializeObject(jCopyPayload);

            var copyPayloadDe = JsonConvert.DeserializeObject<CopyPayload>(jCopyPayloadSer);
            var jCopyPayloadDe = JsonConvert.DeserializeObject<ftJournalFRCopyPayload>(copyPayloadSer);

            copyPayload.Should().BeEquivalentTo(copyPayloadDe);
            jCopyPayload.Should().BeEquivalentTo(jCopyPayloadDe);
        }

        [Fact]
        public async Task PopulateFtJournalFRCopyPayloadTableAsync_ShouldInsertCopyPayloadBasedOnCopyPayloadClass()
        {
            var mockJournalFRCopyPayloadRepository = new Mock<IJournalFRCopyPayloadRepository>();
            var mockJournalFRRepository = new Mock<IMiddlewareJournalFRRepository>();

            var copyPayload = new CopyPayload
            {
                CopiedReceiptReference = "TestValue"
            };
            var jwt = JwtTestHelper.GenerateJwt(copyPayload);

            var asyncEnumerableResult = new List<ftJournalFR>
            {
                new ftJournalFR { JWT = jwt }
            }.ToAsyncEnumerable();

            mockJournalFRRepository.Setup(r => r.GetProcessedCopyReceiptsAsync()).Returns(asyncEnumerableResult);
            mockJournalFRCopyPayloadRepository.Setup(r => r.InsertAsync(It.IsAny<ftJournalFRCopyPayload>()))
                .Returns(Task.FromResult(true));

            var bootstrapper = new TestableBaseStorageBootStrapper();

            await bootstrapper.PopulateFtJournalFRCopyPayloadTableAsync(mockJournalFRCopyPayloadRepository.Object, mockJournalFRRepository.Object);

            mockJournalFRCopyPayloadRepository.Verify(repo => repo.InsertAsync(It.IsAny<ftJournalFRCopyPayload>()), Times.Once());
        }

        [Fact]
        public async Task TestInsertionOfCopyPayloadWithPreviousReceiptReference()
        {
            var inMemoryJournalFRCopyPayloadRepository = new InMemoryJournalFRCopyPayloadRepository();

            var copyPayload = new CopyPayload
            {
                CopiedReceiptReference = "TestValue"
            };
            var jwt = JwtTestHelper.GenerateJwt(copyPayload);

            var mockJournalFRRepository = new Mock<IMiddlewareJournalFRRepository>();
            var asyncEnumerableResult = new List<ftJournalFR>
            {
                new() { JWT = jwt }
            }.ToAsyncEnumerable();

            mockJournalFRRepository.Setup(r => r.GetProcessedCopyReceiptsAsync()).Returns(asyncEnumerableResult);

            var bootstrapper = new TestableBaseStorageBootStrapper();
            await bootstrapper.PopulateFtJournalFRCopyPayloadTableAsync(inMemoryJournalFRCopyPayloadRepository, mockJournalFRRepository.Object);

            var count = await inMemoryJournalFRCopyPayloadRepository.GetCountOfCopiesAsync("TestValue");
            Assert.Equal(1, count);
        }
    }
}