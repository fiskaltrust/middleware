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

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class EpsonSCUTest
    {

        [Fact(Skip = "Only with active Testprinter")]
        public async Task GetSerialNumber_GetResult_11DigetSerialnrAsync()
        {
            var config = new EpsonScuConfiguration()
            {
                DeviceUrl = "https://469b-194-93-177-143.eu.ngrok.io"
            };

            var epsonScu = new EpsonSCU(new Mock<ILogger<EpsonSCU>>().Object, config, new Epson.Utilities.EpsonCommandFactory(config));

            var serialnr = await epsonScu.GetSerialNumberAsync("I").ConfigureAwait(false);
            serialnr.Should().Be("99IEC018305");
        }
    }
}
