using Xunit;
using System.IO;
using FluentAssertions;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class PrinterResponseTests
    {
        [Fact]
        public void DeviceStatuse_FromBytes_CreateObject()
        {
            var printerstatus = new int[] { 0, 0, 1, 1, 0 };
            var deviceStatus = new DeviceStatus(printerstatus);
            deviceStatus.CashDrawer.Should().Be(CashDrawer.Closed);
            deviceStatus.ElectronicJournal.Should().Be(ElectronicJournal.Ok);
            deviceStatus.Invoice.Should().Be(Invoice.NoDocument);
            deviceStatus.Operative.Should().Be(Operative.RegistrationState);
            deviceStatus.Printer.Should().Be(Printer.Ok);
        }

        [Fact]
        public void GetPrinterResponse_PrinterStatusBasic_CreateObject()
        {
            var file = new StreamReader(Path.Combine("Testdata", "ResponsePrinterStatusBasic.xml"));
            file.BaseStream.Position = 0;
            var response = SoapSerializer.Deserialize<StatusResponse>(file.BaseStream);
            Assert.NotNull(response);
            response.Printerstatus.CpuRel.Should().Be("07.00");
            response.Printerstatus.MfRel.Should().Be("04.3");
            response.Printerstatus.MfStatus.Should().Be("0");
            response.Printerstatus.FpStatus.Should().Be("00110");
        }

        [Fact]
        public void GetPrinterResponse_PrinterStatus_CreateObject()
        {
            var file = new StreamReader(Path.Combine("Testdata", "ResponsePrinterStatus.xml"));
            file.BaseStream.Position = 0;
            var response = SoapSerializer.Deserialize<StatusResponse>(file.BaseStream);
            Assert.NotNull(response);
            response.Printerstatus.RtType.Should().Be("12");
            response.Printerstatus.MainStatus.Should().Be("23");
            response.Printerstatus.SubStatus.Should().Be("34");
            response.Printerstatus.DailyOpen.Should().Be("45");
            response.Printerstatus.NoWorkingPeriod.Should().Be("56");
            response.Printerstatus.FileToSend.Should().Be("67");
            response.Printerstatus.OldFileToSend.Should().Be("78");
            response.Printerstatus.FileRejected.Should().Be("89");
            response.Printerstatus.ExpiryCD.Should().Be("90");
            response.Printerstatus.ExpiryCA.Should().Be("001");
            response.Printerstatus.TrainingMode.Should().Be("012");
            response.Printerstatus.UpgradeResult.Should().Be("023");
        }
    }
}
