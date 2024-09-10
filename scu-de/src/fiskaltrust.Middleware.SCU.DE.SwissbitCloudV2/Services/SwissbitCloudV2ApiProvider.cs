using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services
{
    public sealed class SwissbitCloudV2ApiProvider : ISwissbitCloudV2ApiProvider, IDisposable
    {
        private const int EXPORT_TIMEOUT_MS = 18000 * 1000;

        //private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly HttpClientWrapper _httpClient;
        //private readonly JsonSerializerSettings _serializerSettings;


        public SwissbitCloudV2ApiProvider(HttpClientWrapper httpClientWrapper)
        {
            //_configuration = configuration;
            _httpClient = httpClientWrapper;
            /*_serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };*/
        }

        public Task CreateClientAsync(ClientDto client) => throw new NotImplementedException();
        public void Dispose() => _httpClient?.Dispose();
    }
}
