using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT.Exports.SAFTPT;

public class SAFTMappingTests
{
    [Fact]
    public void SAFTMapping_ShouldMapHeaderCorrectly()
    {
        var queueItem = new ftQueueItem
        {
            request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
            response = JsonSerializer.Serialize(new ReceiptResponse
            {
                ftState = (State) 0x5054_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "ft1234#FS 123893/2444",
                ftSignatures = [
                    new SignatureItem
                    {
                        Data = "hash_data",
                        ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
                        ftSignatureFormat = (SignatureFormat)0x001,
                    },
                    new SignatureItem
                    {
                        Data = "atcud_data",
                        ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
                        ftSignatureFormat = (SignatureFormat)0x001,
                    }
                ],
                ftReceiptMoment = DateTime.UtcNow,
            }),
        };
        var auditFile = new SaftExporter().CreateAuditFile(new storage.V0.MasterData.AccountMasterData
        {
            TaxId = "123456789",
        }, [queueItem], 0);
        auditFile.MasterFiles.Customer.Should().HaveCount(1);
        auditFile.MasterFiles.Product.Should().HaveCount(1);
    }
}
