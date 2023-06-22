using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.DisabledSCU
{
    public class DisabledException : Exception
    {
        public DisabledException() : base("This SCU is configured as temporarily disabled.")
        {
        }
    }

    public class DisabledSCU : IDESSCD
    {
        public Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => throw new DisabledException();
        public Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => throw new DisabledException();
        public Task ExecuteSelfTestAsync() => throw new DisabledException();
        public Task ExecuteSetTseTimeAsync() => throw new DisabledException();
        public Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request) => throw new DisabledException();
        public Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request) => throw new DisabledException();
        public Task<TseInfo> GetTseInfoAsync() => throw new DisabledException();
        public Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request) => throw new DisabledException();
        public Task<TseState> SetTseStateAsync(TseState state) => throw new DisabledException();
        public Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => throw new DisabledException();
        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new DisabledException();
        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new DisabledException();
        public Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request) => throw new DisabledException();
        public Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request) => throw new DisabledException();
        public Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request) => throw new DisabledException();
    }
}