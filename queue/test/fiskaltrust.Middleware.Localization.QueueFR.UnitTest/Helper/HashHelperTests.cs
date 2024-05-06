using System.IO;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public class HashHelperTests
    {
        [Fact]
        public void ComputeSHA256Base64Url_ValidFileName_ReturnsExpectedResult()
        {
            // Arrange
            var path = "testhashfile.txt";
            var testContent = "Hello World!";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, testContent);

            // Act
            var actualHash = HashHelper.ComputeSHA256Base64Url(path);

            // Assert
            Assert.Equal("f4OxZX_x_FO5LcGBSKHWXfwtSx-j1ncoSt3SABJtkGk", actualHash);
        }
    }
}
