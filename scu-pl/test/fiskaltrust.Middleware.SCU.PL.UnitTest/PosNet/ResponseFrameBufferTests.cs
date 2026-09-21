using fiskaltrust.Middleware.SCU.PL.PosNet.Transport;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.UnitTest.PosNet;

public class ResponseFrameBufferTests
{
    private static readonly byte[] s_frame = [0x02, (byte)'s', (byte)'c', 0x09, (byte)'#', (byte)'1', (byte)'2', (byte)'3', (byte)'4', 0x03];

    [Fact]
    public void Append_AssemblesAFrameThatArrivesInPieces()
    {
        var buffer = new ResponseFrameBuffer();

        buffer.Append(s_frame.AsSpan(0, 3));
        buffer.IsComplete.Should().BeFalse();
        buffer.Append(s_frame.AsSpan(3, 6));
        buffer.IsComplete.Should().BeFalse();
        buffer.Append(s_frame.AsSpan(9));

        buffer.IsComplete.Should().BeTrue();
        buffer.ToArray().Should().Equal(s_frame);
    }

    [Fact]
    public void Append_DropsWhatComesBeforeTheStx()
    {
        var buffer = new ResponseFrameBuffer();

        buffer.Append([0x00, 0xFF, (byte)'x']);
        buffer.Append(s_frame);

        buffer.ToArray().Should().Equal(s_frame);
    }

    [Fact]
    public void Append_DropsWhatComesAfterTheEtx()
    {
        var buffer = new ResponseFrameBuffer();

        buffer.Append([.. s_frame, 0x02, (byte)'n', (byte)'e', (byte)'x', (byte)'t']);

        buffer.IsComplete.Should().BeTrue();
        buffer.ToArray().Should().Equal(s_frame);
    }
}
