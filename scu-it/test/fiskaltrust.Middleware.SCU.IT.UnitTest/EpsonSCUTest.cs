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
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.UnitTest
{
    public class EpsonSCUTest
    {
        private static readonly Guid _queueId = Guid.NewGuid();
        private static readonly Uri _deviceUrl = new Uri("https://f51f-88-116-45-202.ngrok-free.app");
        private readonly EpsonScuConfiguration _config = new EpsonScuConfiguration
        {
            DeviceUrl = _deviceUrl.ToString()
        };

        private IITSSCD GetSUT()
        {
            var serviceCollection = new ServiceCollection();
            //serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_config))
            };
            sut.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }

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
