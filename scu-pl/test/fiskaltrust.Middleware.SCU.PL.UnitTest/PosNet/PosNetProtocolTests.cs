using System.Text;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class PosNetProtocolTests
{
    [Fact]
    public void Crc16_MatchesTheSpecCheckVector()
    {
        // POT-I-DEV-05: "The implementation used can normally be verified by calculating the
        // sum of '123456789'. The expected value is 0x31c3."
        PosNetCrc16.Compute(Encoding.ASCII.GetBytes("123456789")).Should().Be(0x31C3);
    }

    [Fact]
    public void Crc16_MatchesTheSpecFrameExample()
    {
        // POT-I-DEV-05 checksum example: trinit[TAB]bm0[TAB] -> 0x4825.
        PosNetCrc16.Compute(Encoding.ASCII.GetBytes("trinit\tbm0\t")).Should().Be(0x4825);
    }

    [Fact]
    public void Encode_Trinit_ProducesTheSpecExampleFrame()
    {
        var frame = PosNetFrame.Encode(PosNetCommands.Trinit());

        frame.Should().Equal(Encoding.ASCII.GetBytes("\x02trinit\tbm0\t#4825\x03"));
    }

    [Fact]
    public void Decode_StandardResponse_ReturnsEchoedCommandAndParameters()
    {
        var frame = EncodeResponse("scomm\tfs1\ttz1\tts0\thr1\tnuAB123\t");

        var response = PosNetFrame.Decode(frame);

        response.IsError.Should().BeFalse();
        response.CommandId.Should().Be("scomm");
        response.Parameters.Should().Contain(new KeyValuePair<string, string>("fs", "1"));
        response.Parameters.Should().Contain(new KeyValuePair<string, string>("ts", "0"));
        response.Parameters.Should().Contain(new KeyValuePair<string, string>("nu", "AB123"));
    }

    [Fact]
    public void Decode_ErrorResponse_CarriesTheDecimalErrorCode()
    {
        var frame = EncodeResponse("trline\t?2005\t");

        var response = PosNetFrame.Decode(frame);

        response.IsError.Should().BeTrue();
        response.ErrorCode.Should().Be(2005);
        response.CommandId.Should().Be("trline");
    }

    [Fact]
    public void Decode_FrameErrorResponse_IsRecognized()
    {
        var frame = EncodeResponse("ERR\t?53\t");

        var response = PosNetFrame.Decode(frame);

        response.IsFrameError.Should().BeTrue();
        response.IsError.Should().BeTrue();
        response.ErrorCode.Should().Be(53);
    }

    [Fact]
    public void Decode_TamperedChecksum_Throws()
    {
        var frame = EncodeResponse("scomm\tfs1\t");
        frame[2] ^= 0xFF;

        var act = () => PosNetFrame.Decode(frame);

        act.Should().Throw<PosNetProtocolException>().WithMessage("*CRC16*");
    }

    [Theory]
    [InlineData("Sok\tvt0")]
    [InlineData("Sok\x03")]
    [InlineData("Sok\r\nJabłka")]
    public void Encode_ValueWithAFramingCharacter_IsRejected(string goodsName)
    {
        // The protocol has no escaping, so such a value would not travel as content: a TAB opens a
        // further field (another vt, i.e. a different PTU slot) and an ETX ends the frame early.
        var act = () => PosNetFrame.Encode(PosNetCommands.Trline(goodsName, 1, 200, 1m, 200));

        act.Should().Throw<PosNetProtocolException>().WithMessage("*control character*");
    }

    [Theory]
    [InlineData("Sok\tvt0", "Sok vt0")]
    [InlineData("Sok\x03", "Sok")]
    [InlineData("Sok\r\nJabłka", "Sok  Jabłka")]
    [InlineData("Jabłka", "Jabłka")]
    public void ToField_ReplacesFramingCharacters_AndKeepsPolishText(string value, string expected)
    {
        PosNetText.ToField(value, 80).Should().Be(expected);
    }

    [Fact]
    public void ToField_CutsToTheFieldLength_WithoutTrailingPadding()
    {
        PosNetText.ToField("Sok pomarańczowy", 8).Should().Be("Sok poma");
        PosNetText.ToField("Sok pomarańczowy", 4).Should().Be("Sok");
    }

    [Fact]
    public void Decode_MissingFrameMarkers_Throws()
    {
        var act = () => PosNetFrame.Decode(Encoding.ASCII.GetBytes("scomm\tfs1\t#0000"));

        act.Should().Throw<PosNetProtocolException>();
    }

    internal static byte[] EncodeResponse(string payload)
    {
        var payloadBytes = Encoding.ASCII.GetBytes(payload);
        var crc = PosNetCrc16.Compute(payloadBytes);
        return Encoding.ASCII.GetBytes($"\x02{payload}#{crc:X4}\x03");
    }
}
