using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetReceiptReadbackTests
{
    private static readonly TimeZoneInfo s_warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    private static PosNetResponse Rtcget(params (string Key, string Value)[] fields)
        => new("rtcget", null, fields.ToDictionary(f => f.Key, f => f.Value));

    [Theory]
    [InlineData("2026-10-09T11:49:13+02:00", "2026-10-09T11:49:13+02:00")]
    [InlineData("2026-10-09T09:49:13Z", "2026-10-09T09:49:13+00:00")]
    [InlineData("2026-10-09T05:49:13-04:00", "2026-10-09T05:49:13-04:00")]
    public void ReadDeviceMoment_IsoClockWithOffset_IsTakenAsItIs(string tm, string expected)
        => PosNetReceiptReadback.ReadDeviceMoment(Rtcget(("da", "2000-01-01;00:00"), ("tm", tm)), s_warsaw).Should().Be(expected);

    [Fact]
    public void ReadDeviceMoment_WallClockOnly_IsPolishTimeToTheMinute()
        => PosNetReceiptReadback.ReadDeviceMoment(Rtcget(("da", "2026-07-01;9:05")), s_warsaw).Should().Be("2026-07-01T09:05+02:00");

    [Fact]
    public void ReadDeviceMoment_WallClockInTheRepeatedAutumnHour_IsReadAsStandardTime()
        // 02:30 occurs twice on 2026-10-25; da cannot say which, the later one is documented.
        => PosNetReceiptReadback.ReadDeviceMoment(Rtcget(("da", "2026-10-25;02:30")), s_warsaw).Should().Be("2026-10-25T02:30+01:00");

    [Fact]
    public void ReadDeviceMoment_WallClockWithoutAZone_IsNotGuessed()
        // A host without zone data: the moment is left out rather than signed with a made-up offset.
        => PosNetReceiptReadback.ReadDeviceMoment(Rtcget(("da", "2026-10-09;11:49"), ("tm", "2026-10-09T11:49:13")), registerZone: null).Should().BeNull();
}
