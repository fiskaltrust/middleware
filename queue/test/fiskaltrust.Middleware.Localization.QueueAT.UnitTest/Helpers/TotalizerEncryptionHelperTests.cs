using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.Helpers
{
    public class TotalizerEncryptionHelperTests : IClassFixture<SignProcessorATFixture>
    {
        public Guid encryptkey { get; } = Guid.Parse("ef9764af-1102-41e8-b901-eb89d45cde1d");

        private readonly SignProcessorATFixture _fixture;
        public TotalizerEncryptionHelperTests(SignProcessorATFixture fixture)
        {
            _fixture = fixture;
        }
        [Fact]
        public async Task EncryptDecryptionTotalizer_ReturnsEncryptedTotalizer()
        {
            // Arrange
            await _fixture.CreateConfigurationRepository().ConfigureAwait(false);
            var receiptIdentification = "ft1#23";
            var totalizerValue = 123.45m;

            // Act
            var encryptedTotalizer = TotalizerEncryptionHelper.EncryptTotalizer(_fixture.queueAT.CashBoxIdentification, receiptIdentification, _fixture.queueAT.EncryptionKeyBase64, totalizerValue);
            var decryptedTotalizer = TotalizerEncryptionHelper.DecryptTotalizer(_fixture.queueAT.CashBoxIdentification, receiptIdentification, _fixture.queueAT.EncryptionKeyBase64, encryptedTotalizer);

            // Assert
            Assert.NotNull(encryptedTotalizer);
            Assert.Equal(5, encryptedTotalizer.Length);
            Assert.Equal(totalizerValue, decryptedTotalizer);

        }
    }
}
