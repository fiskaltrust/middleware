using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using fiskaltrust.ifPOS.v1;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Helper
{
    public class FileHelperTests
    {
        [Fact]
        public void ReadFileAsChunks_ShouldReturnChunks()
        {
            // Arrange
            var path = "testfile.txt";
            var maxChunkSize = 4;
            var testContent = "Hello World!";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, testContent);

            // Act
            var chunked = new List<byte>();
            foreach (var chunk in FileHelper.ReadFileAsChunks(path, maxChunkSize))
            {
                chunked.AddRange(chunk);
            }
            var utfString = Encoding.UTF8.GetString(chunked.ToArray(), 0, chunked.ToArray().Length);
            // Assert
            Assert.Equal(testContent, utfString);
        }
    }
}
