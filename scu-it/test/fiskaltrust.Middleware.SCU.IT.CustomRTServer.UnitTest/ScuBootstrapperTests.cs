using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
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
                    { "ServerUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }


        [Fact]
        public void Test22()
        {
            var result = CustomRTServer.CreatePrintSignature01(4, 5, "48548580asdjföekjfölavloöj==", DateTime.UtcNow, new QueueIdentification
            {
                RTServerSerialNumber = "96SRT001239",
                CashUuId = "00010002"
            }, "2348923409234", "823482382").ToString();

            var nocustomer = CustomRTServer.CreatePrintSignature01(4, 5, "48548580asdjföekjfölavloöj==", DateTime.UtcNow, new QueueIdentification
            {
                RTServerSerialNumber = "96SRT001239",
                CashUuId = "00010002"
            }, "2348923409234", "").ToString();

            var noasd = CustomRTServer.CreatePrintSignature01(4, 5, "48548580asdjföekjfölavloöj==", DateTime.UtcNow, new QueueIdentification
            {
                RTServerSerialNumber = "96SRT001239",
                CashUuId = "00010002"
            }, "", "").ToString();


            var nolotteria = CustomRTServer.CreatePrintSignature01(4, 5, "48548580asdjföekjfölavloöj==", DateTime.UtcNow, new QueueIdentification
            {
                RTServerSerialNumber = "96SRT001239",
                CashUuId = "00010002"
            }, "", "823482382").ToString();
        }
    }
}