using AutoFixture;
using fiskaltrust.ifPOS.v2.Cases;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.Models.Cases.Tests;

public class StateFlagsTests
{
    private readonly IFixture _fixture;

    public StateFlagsTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void FuzzTest_WithFlag()
    {
        for (var i = 0; i < 1000; i++)
        {
            var state = _fixture.Create<State>();
            var stateFlag = _fixture.Create<StateFlags>();

            var result = state.WithFlag(stateFlag);

            result.IsFlag(stateFlag).Should().BeTrue();
        }
    }


    [Fact]
    public void FuzzTest_WithState()
    {
        for (var i = 0; i < 1000; i++)
        {
            var state = _fixture.Create<State>();
            var stateState = _fixture.Create<State>();

            var result = state.WithState(stateState);

            result.State().Should().Be(stateState);
            result.IsState(stateState).Should().BeTrue();
        }
    }


    [Fact]
    public void FuzzTest_Reset()
    {
        for (var i = 0; i < 1000; i++)
        {
            var state = _fixture.Create<State>();

            var result = state.Reset();

            result.Should().Be((State) (0xFFFF_F000_0000_0000 & (ulong) state));
        }
    }

    [Fact]
    public void FuzzTest_WithVersion()
    {
        for (var i = 0; i < 1000; i++)
        {
            var state = _fixture.Create<State>();
            var version = (byte) (_fixture.Create<byte>() >> 4);

            var result = state.WithVersion(version);

            result.Version().Should().Be(version);
        }
    }

    [Fact]
    public void FuzzTest_WithCountry()
    {
        foreach (var (country, code) in new List<(string, ulong)> {
            ("AT", 0x4154),
            ("DE", 0x4445),
            ("FR", 0x4652),
            ("IT", 0x4954),
            ("ME", 0x4D45),
            ("ES", 0x4752),
            ("GR", 0x4752),
            ("PT", 0x5054),
            })
        {
            var state = _fixture.Create<State>();

            var resultCode = state.WithCountry(code);
            resultCode.CountryCode().Should().Be(code);

            var resultString = state.WithCountry(country);
            resultString.Country().Should().Be(country);
        }
    }
}
