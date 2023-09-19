using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest
{
    public class BaseStorageBootStrapperTests
    {
        private class TestableBaseStorageBootStrapper : BaseStorageBootStrapper
        {
            public new async Task PopulateFtJournalFRCopyPayloadTableAsync(IJournalFRCopyPayloadRepository journalFRCopyPayloadRepository, IMiddlewareJournalFRRepository journalFRRepository)
            {
                await base.PopulateFtJournalFRCopyPayloadTableAsync(journalFRCopyPayloadRepository, journalFRRepository);
            }
        }

        [Fact]
        public async Task PopulateFtJournalFRCopyPayloadTableAsync_ShouldInsertCopyPayload()
        {
            var mockJournalFRCopyPayloadRepository = new Mock<IJournalFRCopyPayloadRepository>();
            var mockJournalFRRepository = new Mock<IMiddlewareJournalFRRepository>();
            var jwt = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")) + "." +
                      Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"TestPayload\":\"TestValue\"}")) + "." +
                      Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
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
            var mockJournalFRRepository = new Mock<IMiddlewareJournalFRRepository>();
            var jwt = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")) + "." +
                      Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"CopiedReceiptReference\":\"TestValue\"}")) + "." +
                      Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
            var asyncEnumerableResult = new List<ftJournalFR>
            {
                new ftJournalFR { JWT = jwt }
            }.ToAsyncEnumerable();

            mockJournalFRRepository.Setup(r => r.GetProcessedCopyReceiptsAsync()).Returns(asyncEnumerableResult);

            var bootstrapper = new TestableBaseStorageBootStrapper();

            await bootstrapper.PopulateFtJournalFRCopyPayloadTableAsync(inMemoryJournalFRCopyPayloadRepository, mockJournalFRRepository.Object);

            var count = await inMemoryJournalFRCopyPayloadRepository.GetCountOfCopiesAsync("TestValue"); 

            Assert.Equal(1, count); 
        }
    }
}
