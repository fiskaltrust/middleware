#if WCF

using fiskaltrust.ifPOS.Tests.Helpers;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.Tests.v1.IPOS
{
    // If these tests are failing you have to execute the following command as an Administrator
    // netsh http add urlacl url=http://+:8008/ user=Everyone listen=yes
    // To add the url that is used for binding
    public class HttpIPOSV1Tests : IPOSV1Tests
    {
        private string _url;
        private ServiceHost _serviceHost;

        public HttpIPOSV1Tests()
        {
            _url = $"http://localhost:12080/pos/{Guid.NewGuid()}";
        }

        ~HttpIPOSV1Tests()
        {
            _serviceHost.Close();
            _serviceHost = null;
        }

        protected override ifPOS.v1.IPOS CreateClient() => WcfHelper.GetRestProxy<ifPOS.v1.IPOS>(_url);

        protected override void StartHost() => _serviceHost = WcfHelper.StartRestHost<ifPOS.v1.IPOS>(_url, new DummyPOSV1());

        protected override void StopHost() => _serviceHost.Close();

        [Test]
        public async Task SignV0_ShouldReturnSameQueueId_For_WebClient()
        {
            var queueId = Guid.NewGuid().ToString();
            using (var httpClient = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(new ReceiptRequest
                {
                    ftQueueID = queueId,
                    ftCashBoxID = "",
                    cbTerminalID = "",
                    cbReceiptReference = "",
                    cbReceiptMoment = DateTime.Now,
                    cbChargeItems = new ChargeItem[] { },
                    cbPayItems = new PayItem[] { },
                    ftReceiptCase = 100
                });
                var result = await httpClient.PostAsync(new Uri(_url + "/v0/sign"), new StringContent(json, Encoding.UTF8, "application/json"));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<ReceiptResponse>(content);
                response.ftQueueID.Should().Be(queueId);
            }
        }

        [Test]
        public async Task SignV1_ShouldReturnSameQueueId_For_WebClient()
        {
            var queueId = Guid.NewGuid().ToString();
            using (var httpClient = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(new ReceiptRequest
                {
                    ftQueueID = queueId,
                    ftCashBoxID = "",
                    cbTerminalID = "",
                    cbReceiptReference = "",
                    cbReceiptMoment = DateTime.Now,
                    cbChargeItems = new ChargeItem[] { },
                    cbPayItems = new PayItem[] { },
                    ftReceiptCase = 100
                });
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/sign"), new StringContent(json, Encoding.UTF8, "application/json"));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<ReceiptResponse>(content);
                response.ftQueueID.Should().Be(queueId);
            }
        }

        [Test]
        public async Task EchoV0_ShouldReturnSameMessage_For_WebClient()
        {
            var inMessage = Guid.NewGuid().ToString();
            using (var httpClient = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(inMessage);
                var result = await httpClient.PostAsync(new Uri(_url + "/v0/echo"), new StringContent(json, Encoding.UTF8, "application/json"));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<string>(content);
                response.Should().Be(inMessage);
            }
        }

        [Test]
        public async Task Echov1_ShouldReturnSameMessage_For_WebClient()
        {
            var inMessage = Guid.NewGuid().ToString();
            using (var httpClient = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(new EchoRequest
                {
                    Message = inMessage
                });
                var result = await httpClient.PostAsync(new Uri(_url + "/v1/echo"), new StringContent(json, Encoding.UTF8, "application/json"));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<EchoResponse>(content);
                response.Message.Should().Be(inMessage);
            }
        }

        [Test]
        public async Task JournalV0_ShouldReturnSameMessage_For_WebClient()
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.PostAsync(new Uri(_url + "/v0/journal?type=0&from=0&to=0"), new StringContent(""));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                content.Should().Be("{\"ftJournalType\":0,\"from\":0,\"to\":0}");
            }
        }
    }
}

#endif