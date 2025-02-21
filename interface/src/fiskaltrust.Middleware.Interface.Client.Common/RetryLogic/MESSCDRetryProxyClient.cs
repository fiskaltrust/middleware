using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.ifPOS.v1.me;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Common.RetryLogic
{
    public class MESSCDRetryProxyClient : IMESSCD
    {
        private readonly IRetryPolicyHandler<IMESSCD> _retryPolicyHelper;
        public MESSCDRetryProxyClient(IRetryPolicyHandler<IMESSCD> retryPolicyHelper) => _retryPolicyHelper = retryPolicyHelper;

        public async Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.EchoAsync(request));

        public async Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.RegisterCashDepositAsync(registerCashDepositRequest));

        public async Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashDepositRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.RegisterCashWithdrawalAsync(registerCashDepositRequest));
        
        public async Task<ComputeIICResponse> ComputeIICAsync(ComputeIICRequest computeIICRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.ComputeIICAsync(computeIICRequest));

        public async Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.RegisterInvoiceAsync(registerInvoiceRequest));

        public async Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTcrRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.RegisterTcrAsync(registerTcrRequest));

        public async Task UnregisterTcrAsync(UnregisterTcrRequest unregisterTCRRequest) => await _retryPolicyHelper.RetryFuncAsync(async (proxy) => await proxy.UnregisterTcrAsync(unregisterTCRRequest));
    }
}
