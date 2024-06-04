using Xunit;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public class ConversionHelperTests
    {
        [Fact]
        public void ToBase64UrlString_ShouldConvertBytesToBase64UrlString()
        {
            // Arrange
            byte[] bytes = { 72, 101, 108, 108, 111 }; // "Hello" in ASCII

            // Act
            var base64UrlString = ConversionHelper.ToBase64UrlString(bytes);

            // Assert
            Assert.Equal("SGVsbG8", base64UrlString);
        }

        [Fact]
        public void FromBase64UrlString_ShouldConvertBase64UrlStringToBytes()
        {
            // Arrange
            var base64UrlString = "SGVsbG8"; // "Hello" in Base64UrlString format

            // Act
            var bytes = ConversionHelper.FromBase64UrlString(base64UrlString);

            // Assert
            byte[] expectedBytes = { 72, 101, 108, 108, 111 }; // "Hello" in ASCII
            Assert.Equal(expectedBytes, bytes);
        }
    }
}