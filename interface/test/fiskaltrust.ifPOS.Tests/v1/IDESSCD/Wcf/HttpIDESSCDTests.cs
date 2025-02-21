#if WCF

using fiskaltrust.ifPOS.Tests.Helpers;
using System.ServiceModel;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using Newtonsoft.Json;
using System.Text;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;

namespace fiskaltrust.ifPOS.Tests.v1.IDESSCD
{
    public class HttpIDESSCDTests : IDESSCDTests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public HttpIDESSCDTests()
        {
            _url = $"http://localhost:8080/scu/{Guid.NewGuid()}";
        }

        ~HttpIDESSCDTests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v1.de.IDESSCD CreateClient() => WcfHelper.GetRestProxy<ifPOS.v1.de.IDESSCD>(_url);

        protected override void StartHost() => _serviceHost = WcfHelper.StartRestHost<ifPOS.v1.de.IDESSCD>(_url, new DummyDESSCD());

        protected override void StopHost() => _serviceHost.Close();

        [Test]
        public async Task StartTransactionV1_ShouldReturn()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestData = new StartTransactionRequest();
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var result = await httpClient.PostAsync(new Uri(_url + "/v1/starttransaction"), content);
                    result.EnsureSuccessStatusCode();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Test]
        public async Task UpdateTransactionV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new UpdateTransactionRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/updatetransaction"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task FinishTransactionV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new FinishTransactionRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/finishtransaction"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task GetTseInfoV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(new Uri(_url + "/v1/tseinfo"));
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task SetTseStateV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new TseState();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/tsestate"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task RegisterClientIdV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new RegisterClientIdRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/registerclientid"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task UnregisterClientIdV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new UnregisterClientIdRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/unregisterclientid"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task ExecuteSetTseTimeV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/executesettsetime"), null);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task ExecuteSelfTestV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/executeselftest"), null);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task StartExportSessionV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/startexportsession"), null);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task StartExportSessionByTimeStampV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new StartExportSessionByTimeStampRequest()
                {
                    From = DateTime.UtcNow.AddDays(-1.0),
                    To = DateTime.UtcNow.AddDays(1.0)
                };
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/startexportsessionbytimestamp"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task StartExportSessionByTransactionV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new StartExportSessionByTransactionRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/startexportsessionbytransaction"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task ExportDataV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new ExportDataRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/exportdata"), content);
                result.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task EndExportSessionV1_ShouldReturn()
        {
            using (var httpClient = new HttpClient())
            {
                var requestData = new EndExportSessionRequest();
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/endexportsession"), content);
                result.EnsureSuccessStatusCode();
            }
        }
    }
}

#endif