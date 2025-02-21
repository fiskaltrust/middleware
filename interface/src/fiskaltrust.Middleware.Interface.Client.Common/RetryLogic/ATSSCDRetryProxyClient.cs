using fiskaltrust.ifPOS.v1.at;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public class ATSSCDRetryProxyClient : IATSSCD
    {
        private readonly IRetryPolicyHandler<IATSSCD> _retryPolicyHelper;

        public ATSSCDRetryProxyClient(IRetryPolicyHandler<IATSSCD> retryPolicyHelper) => _retryPolicyHelper = retryPolicyHelper;

        [Obsolete]
        public byte[] Certificate() => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Certificate())).Result;

        public async Task<CertificateResponse> CertificateAsync() => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.CertificateAsync());

        [Obsolete]
        public IAsyncResult BeginCertificate(AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginCertificate(callback, state))).Result;

        [Obsolete]
        public byte[] EndCertificate(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndCertificate(result))).Result;

        [Obsolete]
        public string Echo(string message) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Echo(message))).Result;

        public async Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.EchoAsync(echoRequest));

        [Obsolete]
        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginEcho(message, callback, state))).Result;

        [Obsolete]
        public string EndEcho(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndEcho(result))).Result;

        [Obsolete]
        public byte[] Sign(byte[] data) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Sign(data))).Result;

        public async Task<SignResponse> SignAsync(SignRequest signRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.SignAsync(signRequest));

        [Obsolete]
        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginSign(data, callback, state))).Result;

        [Obsolete]
        public byte[] EndSign(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndSign(result))).Result;

        [Obsolete]
        public string ZDA() => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.ZDA())).Result;

        public async Task<ZdaResponse> ZdaAsync() => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.ZdaAsync());

        [Obsolete]
        public IAsyncResult BeginZDA(AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginZDA(callback, state))).Result;

        [Obsolete]
        public string EndZDA(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndZDA(result))).Result;
    }
}
