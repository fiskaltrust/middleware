using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
    public class CRC32Tests
    {
        [Theory]
        [InlineData("Beer", 4173646227)]
        [InlineData("", null)]
        public void ComputeHash_Should_CreateCorrectCrc32Hashes(string input, uint? expectedOutput)
        {
            var sut = new CRC32();
            sut.ComputeHash(input).Should().Be(expectedOutput);
        }
    }
}
