using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    /// <summary>
    /// An Azure partition key holds at most 1 KiB ([01 #25]): a copied receipt reference whose Base64Url form would not fit
    /// is keyed by its SHA-256 instead; every reference that fits keeps the key it always had. No storage is needed.
    /// </summary>
    public class CopyPayloadPartitionKeyTests
    {
        [Fact]
        public void AShortReference_KeepsItsBase64UrlKey()
            => AzureTableStorageJournalFRCopyPayloadRepository.PartitionKeyOf("R-1").Should().Be("Ui0x");

        [Fact]
        public void ALongReference_GetsAKeyThatFits()
        {
            var key = AzureTableStorageJournalFRCopyPayloadRepository.PartitionKeyOf(new string('x', 2000));

            key.Length.Should().BeLessThan(1024);
            key.Should().NotBe(AzureTableStorageJournalFRCopyPayloadRepository.PartitionKeyOf(new string('x', 2001)));
        }
    }
}
