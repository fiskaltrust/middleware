using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
{
    public class CustomRTServerClientTests
    {
        [Fact]
        public async Task GetRTInfoAsync_ShouldReturn_SerialNumber()
        {

            var customRTServerClient = new CustomRTServerClient(new CustomRTServerConfiguration { ServerUrl = "https://a3e3-88-116-45-202.ngrok-free.app/", Username = "0001ab05", Password = "admin" });

            _ = await customRTServerClient.InsertCashRegisterAsync("demo", "1010", "1000", "admin", "cf");
        }


        [Fact(Skip = "Currently not working since we don't have a cert.")]
        public async Task GetDailyStatusAsync()
        {

            var customRTServerClient = new CustomRTServerClient(new CustomRTServerConfiguration { ServerUrl = "https://a3e3-88-116-45-202.ngrok-free.app/", Username = "0001ab05", Password = "admin" });

            _ = await customRTServerClient.GetDailyStatusAsync("0001ab05");
        }

        [Fact(Skip = "Currently not working since we don't have a cert.")]
        public async Task CancelCashRegister()
        {

            var customRTServerClient = new CustomRTServerClient(new CustomRTServerConfiguration { ServerUrl = "https://a3e3-88-116-45-202.ngrok-free.app/", Username = "0001ab05", Password = "admin" });

            _ = await customRTServerClient.CancelCashRegisterAsync("0002ab77", "12345688909");
        }
    }
}