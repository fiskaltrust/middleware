using fiskaltrust.ifPOS.v1.de;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Tests.Helpers
{
#if WCF
    [System.ServiceModel.ServiceBehavior(InstanceContextMode = System.ServiceModel.InstanceContextMode.Single)]
#endif
    public class DummyDESSCD : ifPOS.v1.de.IDESSCD
    {
        public async Task<ExportDataResponse> ExportDataAsync() => await Task.FromResult(new ExportDataResponse());

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request) => await Task.FromResult(new FinishTransactionResponse { StartTransactionTimeStamp = DateTime.Now, TimeStamp = DateTime.Now });

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request) => await Task.FromResult(new StartTransactionResponse { TimeStamp = DateTime.Now });

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request) => await Task.FromResult(new UpdateTransactionResponse { TimeStamp = DateTime.Now });

        public async Task<TseInfo> GetTseInfoAsync() => await Task.FromResult(new TseInfo());

        public async Task<TseState> SetTseStateAsync(TseState state) => await Task.FromResult(new TseState());

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request) => await Task.FromResult(new RegisterClientIdResponse());

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request) => await Task.FromResult(new UnregisterClientIdResponse());

        public async Task ExecuteSetTseTimeAsync() => await Task.CompletedTask;

        public async Task ExecuteSelfTestAsync() => await Task.CompletedTask;

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => await Task.FromResult(new StartExportSessionResponse());

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => await Task.FromResult(new StartExportSessionResponse());

        public async Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => await Task.FromResult(new StartExportSessionResponse());

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request) => await Task.FromResult(new ExportDataResponse());

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => await Task.FromResult(new EndExportSessionResponse());

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse());
    }
}
