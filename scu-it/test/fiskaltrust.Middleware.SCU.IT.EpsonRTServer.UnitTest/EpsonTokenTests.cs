using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;
using FluentAssertions;
using FluentAssertions.Execution;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonTokenTests
    {
        [Fact]
        public void TryParse_Should_Decode_A_Real_Token()
        {
            // Real token returned by createToken (device 99SEA004010).
            var token = EpsonToken.TryParse("99SEA004010FISK0001838282026070207420004000070100");

            using (new AssertionScope())
            {
                token.Should().NotBeNull();
                token!.SerialNumber.Should().Be("99SEA004010");
                token.TillId.Should().Be("FISK0001");
                token.ZRepNumber.Should().Be(742);
                token.NextDocNumber.Should().Be(4);
                token.DailyAmountCents.Should().Be(70100);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("tooshort")]
        public void TryParse_Should_Return_Null_For_Invalid_Input(string? input)
        {
            EpsonToken.TryParse(input).Should().BeNull();
        }
    }
}
