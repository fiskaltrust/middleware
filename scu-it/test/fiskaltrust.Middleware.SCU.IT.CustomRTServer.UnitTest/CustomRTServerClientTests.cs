using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
{
    public class CustomRTServerClientTests
    {
        private static readonly Uri _serverUri = new Uri("https://f51f-88-116-45-202.ngrok-free.app");
        private readonly CustomRTServerConfiguration _config = new CustomRTServerConfiguration { ServerUrl = _serverUri.ToString(), Username = "0001ab05", Password = "admin" };

        [Fact(Skip = "Needs device")]
        public async Task GetRTInfoAsync_ShouldReturn_SerialNumber()
        {
            var customRTServerClient = new CustomRTServerClient(_config);

            _ = await customRTServerClient.InsertCashRegisterAsync("demo", "1010", "1000", "admin", "cf");
        }


        [Fact(Skip = "Needs device")]
        public async Task GetDailyStatusAsync()
        {
            var customRTServerClient = new CustomRTServerClient(_config);

            _ = await customRTServerClient.GetDailyStatusAsync("0001ab05");
        }

        [Fact(Skip = "Needs device")]
        public async Task GetDailyOpenAsync()
        {
            var customRTServerClient = new CustomRTServerClient(_config);

            var result = await customRTServerClient.InsertCashRegisterAsync("SKE_DEBUG_TEST", "ske0", "0003", "admin", "MTLFNC75A16E783N");

            var dailyStatus = await customRTServerClient.GetDailyStatusAsync(result.cashUuid);
            if (dailyStatus.cashStatus == "0")
            {
                _ = await customRTServerClient.GetDailyOpenAsync(result.cashUuid, DateTime.UtcNow);
                //var insertZ = await customRTServerClient.InsertZDocumentAsync(result.cashUuid, DateTime.UtcNow, long.Parse(data.numberClosure) + 1, data.grandTotalDB);
            }
            else
            {

                //await customRTServerClient.InsertFiscalDocumentAsync()
                //var insertZ2 = await customRTServerClient.InsertZDocumentAsync(result.cashUuid, DateTime.UtcNow, long.Parse(dailyStatus.numberClosure) + 1, dailyStatus.grandTotalDB);
            }
        }

        [Fact(Skip = "Currently not working since we don't have a cert.")]
        public async Task CancelCashRegister()
        {
            var customRTServerClient = new CustomRTServerClient(_config);

            _ = await customRTServerClient.CancelCashRegisterAsync("0002ab77", "12345688909");
        }
    }
}