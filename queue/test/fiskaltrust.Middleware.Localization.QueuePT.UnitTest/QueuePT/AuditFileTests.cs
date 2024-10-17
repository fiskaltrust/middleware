using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT
{
    public class SAFTMappingTests
    {
        [Fact]
        public void SAFTMapping_ShouldMapHeaderCorrectly()
        {
            var queueItem = new ftQueueItem
            {
                request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
            };
            var auditFile = SAFTMapping.CreateAuditFile([queueItem]);
            auditFile.MasterFiles.Customer.Should().HaveCount(1);
            auditFile.MasterFiles.Product.Should().HaveCount(1);
        }
    }
}
