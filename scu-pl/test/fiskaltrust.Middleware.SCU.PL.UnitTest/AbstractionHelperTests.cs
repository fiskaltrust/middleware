using System.Text.Json;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest;

public class PLAmountExtensionsTests
{
    [Theory]
    [InlineData("12.34", 1234L)]
    [InlineData("0.005", 1L)]
    [InlineData("-1.005", -101L)]
    [InlineData("23", 2300L)]
    public void ToGrosze_ShouldRoundAwayFromZero(string amount, long expected)
        => decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture).ToGrosze().Should().Be(expected);

    [Fact]
    public void GroszeToPln_ShouldRoundTrip()
        => 1234L.GroszeToPln().Should().Be(12.34m);
}

public class PLDeviceInfoTests
{
    [Fact]
    public void ToPLSSCDInfo_ShouldRoundTripThroughInfoData()
    {
        var deviceInfo = new PLDeviceInfo
        {
            DeviceSerialNumber = "ABC1234567",
            UniqueDeviceNumber = "ZAS1234567890",
            RegistrationNumber = "1234567890",
            FiscalizationState = PLFiscalizationState.Fiscalized,
            VatRateTable = new List<PLVatRateTableEntry> { new() { PtuSlot = "A", VatRatePercent = 23m } },
            CrkReachable = true,
            EReceiptCapable = true,
            CurrentZReportNumber = 42,
        };

        var info = deviceInfo.ToPLSSCDInfo();
        info.InfoData.Should().NotBeNullOrEmpty();

        var roundTripped = PLDeviceInfo.FromPLSSCDInfo(info);
        roundTripped.Should().BeEquivalentTo(deviceInfo);
    }

    [Fact]
    public void FromPLSSCDInfo_ShouldReturnNull_WhenInfoDataIsNull()
        => PLDeviceInfo.FromPLSSCDInfo(new ifPOS.v2.pl.PLSSCDInfo()).Should().BeNull();

    /// <summary>
    /// QueuePL decides whether to activate the queue by reading FiscalizationState straight out of
    /// the InfoData blob as a JSON number, deliberately without referencing this package. That makes
    /// the numeric encoding a wire contract rather than an implementation detail: a string-enum
    /// converter or a renumbered enum would leave the initial-operation receipt silently failing.
    /// This test is the guard on this side of that contract.
    /// </summary>
    [Theory]
    [InlineData(PLFiscalizationState.Fiscalized, 2)]
    [InlineData(PLFiscalizationState.NonFiscal, 1)]
    [InlineData(PLFiscalizationState.ReadOnly, 3)]
    public void ToPLSSCDInfo_ShouldWriteTheFiscalizationStateAsItsNumber(PLFiscalizationState state, int expected)
    {
        var info = new PLDeviceInfo { FiscalizationState = state }.ToPLSSCDInfo();

        using var document = JsonDocument.Parse(info.InfoData!);
        var serialized = document.RootElement.GetProperty("FiscalizationState");
        serialized.ValueKind.Should().Be(JsonValueKind.Number);
        serialized.GetInt32().Should().Be(expected);
    }
}

public class PLStateMapperTests
{
    [Fact]
    public void Map_ShouldFlagDeviceUnreachable()
        => PLStateMapper.Map(new PLDeviceUnreachableException("printer offline"))
            .Should().Be((State)0x504C_2001_EEEE_EEEE);

    [Fact]
    public void Map_ShouldReturnPlainError_ForOtherFailures()
        => PLStateMapper.Map(new PLDeviceErrorException(2005, "no transaction mode"))
            .Should().Be((State)0x504C_2000_EEEE_EEEE);
}

public class SignatureTypePLTests
{
    [Fact]
    public void SignatureTypes_ShouldCarryThePLCountryCode()
    {
        foreach (SignatureTypePL type in Enum.GetValues(typeof(SignatureTypePL)))
        {
            ((SignatureType)(ulong)(long)type).Country().Should().Be("PL");
        }
    }

    [Fact]
    public void IsType_ShouldMatchOnTheLowestNibbles()
        => ((SignatureType)0x504C_2000_0000_0104UL).IsType(SignatureTypePL.ZReportNumber).Should().BeTrue();
}
