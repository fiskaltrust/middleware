using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.ifPOS.v1.de;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
namespace fiskaltrust.Middleware.SCU.AT.PrimeSignHSM.IntegrationTest
{
    public class PrimeSignHSMTests
    {
        private readonly IATSSCD _instance = null;
        public PrimeSignHSMTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new ScuBootstrapper
            {
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(new PrimeSignSCUConfiguration()
                {
                    // TODO SETTINGS
                }))
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            _instance = serviceCollection.BuildServiceProvider().GetService<IATSSCD>();
        }

        [Fact]
        public void TestInstance()
        {
            _instance.Should().NotBeNull();
        }

        [Fact]
        public async Task Echo()
        {
           var Message = "try to use \"echo\"";

            var response = await _instance.EchoAsync(new EchoRequest() { Message = Message });
            response.Message.Should().Be(Message);

        }

        [Fact]
        public async Task Zda()
        {
            var Response = await _instance.ZdaAsync();
            Response.ZDA.Should().Be("AT3");

        }

        //[Fact]
        //public async Task Certificate()
        //{
        //    var Response = await _instance.CertificateAsync();
        //    Response.Certificate.Should().NotBeNull();

        //}

        //[Fact]
        //public async Task Sign()
        //{
        //    var Response = await _instance.SignAsync(new SignRequest(){Data=new byte[]{00,00,00,00}});
        //    Response.SignedData.Should().NotBeNull();
        //}

    }
}