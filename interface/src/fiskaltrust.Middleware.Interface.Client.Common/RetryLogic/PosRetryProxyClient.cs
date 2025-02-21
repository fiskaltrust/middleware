using fiskaltrust.ifPOS.v1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public class PosRetryProxyClient : IPOS
    {
        private readonly IRetryPolicyHandler<IPOS> _retryPolicyHelper;

        public PosRetryProxyClient(IRetryPolicyHandler<IPOS> retryPolicyHelper) => _retryPolicyHelper = retryPolicyHelper;

        [Obsolete]
        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginEcho(message, callback, state))).Result;

        [Obsolete]
        public IAsyncResult BeginJournal(long ftJournalType, long from, long to, AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginJournal(ftJournalType, from, to, callback, state))).Result;

        [Obsolete]
        public IAsyncResult BeginSign(ifPOS.v0.ReceiptRequest data, AsyncCallback callback, object state) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.BeginSign(data, callback, state))).Result;

        [Obsolete]
        public string Echo(string message) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Echo(message))).Result;

        public async Task<EchoResponse> EchoAsync(EchoRequest message) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.EchoAsync(message));

        [Obsolete]
        public string EndEcho(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndEcho(result))).Result;

        [Obsolete]
        public Stream EndJournal(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndJournal(result))).Result;

        [Obsolete]
        public ifPOS.v0.ReceiptResponse EndSign(IAsyncResult result) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.EndSign(result))).Result;

        public Stream Journal(long ftJournalType, long from, long to) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Journal(ftJournalType, from, to))).Result;

        public async IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request)
        {
            var result = await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.JournalAsync(request)));
            await foreach (var item in result)
            {
                yield return item;
            }
        }

        [Obsolete]
        public ifPOS.v0.ReceiptResponse Sign(ifPOS.v0.ReceiptRequest data) => _retryPolicyHelper.RetryFuncAsync(async (proxy) => await Task.FromResult(proxy.Sign(data))).Result;

        public async Task<ReceiptResponse> SignAsync(ReceiptRequest request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.SignAsync(request));
    }
}
