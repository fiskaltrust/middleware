using fiskaltrust.Middleware.SCU.IT.Epson.Utilities;
using Xunit;
using System.IO;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class PrinterResponseTests
    {
        [Fact]
        public void GetPrinterResponse_PrinterStatusBasic_CreateObject()
        {
            var file = new StreamReader(Path.Combine("Testdata", "ResponsePrinterStatusBasic.xml"));
            file.BaseStream.Position = 0;
            var response = EpsonXmlWriter.GetPrinterResponse(file.BaseStream);
            Assert.NotNull(response);
            response.AdditionalInfo.CpuRel.Should().Be("07.00");
            response.AdditionalInfo.MfRel.Should().Be("04.3");
            response.AdditionalInfo.MfStatus.Should().Be("0");
            response.AdditionalInfo.FpStatus.Should().Be("00110");
        }

        [Fact]
        public void GetPrinterResponse_PrinterStatus_CreateObject()
        {
            var file = new StreamReader(Path.Combine("Testdata", "ResponsePrinterStatus.xml"));
            file.BaseStream.Position = 0;
            var response = EpsonXmlWriter.GetPrinterResponse(file.BaseStream);
            Assert.NotNull(response);
            response.AdditionalInfo.RtType.Should().Be("12");
            response.AdditionalInfo.MainStatus.Should().Be("23");
            response.AdditionalInfo.SubStatus.Should().Be("34");
            response.AdditionalInfo.DailyOpen.Should().Be("45");
            response.AdditionalInfo.NoWorkingPeriod.Should().Be("56");
            response.AdditionalInfo.FileToSend.Should().Be("67");
            response.AdditionalInfo.OldFileToSend.Should().Be("78");
            response.AdditionalInfo.FileRejected.Should().Be("89");
            response.AdditionalInfo.ExpiryCD.Should().Be("90");
            response.AdditionalInfo.ExpiryCA.Should().Be("001");
            response.AdditionalInfo.TrainingMode.Should().Be("012");
            response.AdditionalInfo.UpgradeResult.Should().Be("023");
        }
    }
}
