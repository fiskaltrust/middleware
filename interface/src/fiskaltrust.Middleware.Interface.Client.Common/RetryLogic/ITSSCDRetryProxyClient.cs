using fiskaltrust.ifPOS.v1.it;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public class ITSSCDRetryProxyClient : IITSSCD
    {
        private readonly IRetryPolicyHandler<IITSSCD> _retryPolicyHelper;
        public ITSSCDRetryProxyClient(IRetryPolicyHandler<IITSSCD> retryPolicyHelper) => _retryPolicyHelper = retryPolicyHelper;

        public async Task<DeviceInfo> GetDeviceInfoAsync() => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.GetDeviceInfoAsync());

        public async Task<ScuItEchoResponse> EchoAsync(ScuItEchoRequest request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.EchoAsync(request));

        public async Task<FiscalReceiptResponse> FiscalReceiptInvoiceAsync(FiscalReceiptInvoice request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.FiscalReceiptInvoiceAsync(request));

        public async Task<FiscalReceiptResponse> FiscalReceiptRefundAsync(FiscalReceiptRefund request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.FiscalReceiptRefundAsync(request));

        public async Task<DailyClosingResponse> ExecuteDailyClosingAsync(DailyClosingRequest request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.ExecuteDailyClosingAsync(request));

        public async Task<Response> NonFiscalReceiptAsync(NonFiscalRequest request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.NonFiscalReceiptAsync(request));

        public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)=> await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.ProcessReceiptAsync(request));
        public async Task<RTInfo> GetRTInfoAsync()=> await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.GetRTInfoAsync());
    }
}
