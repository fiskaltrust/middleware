using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.SCU.IT.Abstraction;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class RebootCommandTests
    {
        [Fact]
        public void RebootCommand_IsDirectIO4034_RestartPrinter()
        {
            var xml = EpsonCommandFactory.RebootCommand();

            Assert.Contains("<printerCommand>", xml);
            Assert.Contains("command=\"4034\"", xml);
            // PARAM=01 (Web server) + INDEX=98 (Restart printer) + FUNCTION=00 + 64-byte space padding.
            Assert.Contains("data=\"019800" + new string(' ', 64) + "\"", xml);
        }

        [Theory]
        [InlineData(0x4954_2040_0000_2000, true)]   // ZeroReceipt + reboot flag
        [InlineData(0x4954_2000_0000_2000, false)]  // plain ZeroReceipt
        [InlineData(0x4954_2001_0000_2000, false)]  // XReport flag must not be read as reboot
        public void IsRebootRequest_DetectsOnlyTheRebootFlag(long ftReceiptCase, bool expected)
        {
            var request = new ReceiptRequest { ftReceiptCase = ftReceiptCase };

            Assert.Equal(expected, request.IsRebootRequest());
        }
    }
}
