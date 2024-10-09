using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT
{
    public class SAFTMappingTests
    {
        [Fact]
        public void SAFTMapping_ShouldMapHeaderCorrectly()
        {
            var nvoice = ReceiptExamples.CASH_SALES_RECEIPT;
            var auditFile = SAFTMapping.CreateAuditFile([ nvoice ]);

            auditFile.MasterFiles.Customer.Should().HaveCount(1);
            auditFile.MasterFiles.Product.Should().HaveCount(1);
        }
    }
}
