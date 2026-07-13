using fiskaltrust.Middleware.SCU.IT.EpsonRTServer;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.UnitTest
{
    public class EpsonRTServerErrorCodesTests
    {
        // "Receipt accepted with error in log file" per the Create Receipt behaviour table.
        [Theory]
        [InlineData(-27)]
        [InlineData(-35)]
        [InlineData(-36)]
        [InlineData(-38)]
        [InlineData(-42)]
        [InlineData(-43)]
        [InlineData(-44)]
        [InlineData(-46)]
        [InlineData(-52)]
        public void IsReceiptAcceptedWithWarning_Should_Be_True_For_Accepted_Codes(int code)
            => EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(code).Should().BeTrue();

        // Blocking rejections, state-out-of-sync (handled separately), N/A and unknown codes are NOT accepted.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-8)]
        [InlineData(-20)]
        [InlineData(-21)]
        [InlineData(-25)]
        [InlineData(-26)]
        [InlineData(-28)]
        [InlineData(-32)]
        [InlineData(-34)]
        [InlineData(-53)]
        public void IsReceiptAcceptedWithWarning_Should_Be_False_For_Other_Codes(int code)
            => EpsonRTServerErrorCodes.IsReceiptAcceptedWithWarning(code).Should().BeFalse();

        [Theory]
        [InlineData(-43, true)]
        [InlineData(-44, true)]
        [InlineData(-52, false)]
        [InlineData(-42, false)]
        public void IsLotteryNotRegistered_Should_Match_Only_43_And_44(int code, bool expected)
            => EpsonRTServerErrorCodes.IsLotteryNotRegistered(code).Should().Be(expected);
    }
}
