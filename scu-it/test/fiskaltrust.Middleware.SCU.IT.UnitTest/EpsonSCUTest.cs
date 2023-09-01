using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using fiskaltrust.Middleware.SCU.IT.Epson;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using FluentAssertions;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class EpsonSCUTest
    {

        [Fact]
        public async Task GetSerialNumber_GetResult_11DigetSerialnrAsync()
        {
            var config = new EpsonScuConfiguration()
            {
                DeviceUrl = "http://192.168.0.34"
            };

            var epsonv2 = new EpsonCommunicationClientV2(new Mock<ILogger<EpsonSCU>>().Object, config, new Epson.Utilities.EpsonCommandFactory(config));
            var epsonScu = new EpsonSCU(new Mock<ILogger<EpsonSCU>>().Object, config, new Epson.Utilities.EpsonCommandFactory(config), epsonv2);

            var processRequest = new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4954_2000_0000_2000
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftCashBoxIdentification = "02020402",
                    ftQueueID = Guid.NewGuid().ToString()
                }
            };

            _ = await epsonScu.ProcessReceiptAsync(processRequest);
        }
    }
}
