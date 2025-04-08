using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ScuBootstrapperTests
    {
        [Fact]
        public void Test1()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "DeviceUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);

            _ = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }
        //[Fact]
        //public void Test2()
        //{
        //    var content = EpsonRTPrinterSCU.PerformUnspecifiedProtocolReceipt(ReceiptExamples.NonFiscal());
        //    var data = fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities.SoapSerializer.Serialize(content);
        //    var httpClient = new HttpClient
        //    {
        //        BaseAddress = new Uri("http://10.0.0.40"),
        //        Timeout = TimeSpan.FromMilliseconds(15000)
        //    };
        //    var commandUrl = $"cgi-bin/fpmate.cgi?timeout=15000";
        //    var response = httpClient.PostAsync(commandUrl, new StringContent(data, Encoding.UTF8, "application/xml")).GetAwaiter().GetResult();

        //    Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        //}
        //[Fact]
        //public void Test3()
        //{
        //    var content = EpsonRTPrinterSCU.PerformUnspecifiedProtocolReceipt(ReceiptExamples.NonFiscalReceipt());
        //    var data = fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Utilities.SoapSerializer.Serialize(content);
        //    var httpClient = new HttpClient
        //    {
        //        BaseAddress = new Uri("http://10.0.0.40"),
        //        Timeout = TimeSpan.FromMilliseconds(15000)
        //    };
        //    var commandUrl = $"cgi-bin/fpmate.cgi?timeout=15000";
        //    var response = httpClient.PostAsync(commandUrl, new StringContent(data, Encoding.UTF8, "application/xml")).GetAwaiter().GetResult();

        //    Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        //}
    }
}