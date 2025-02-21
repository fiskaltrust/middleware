using fiskaltrust.ifPOS.v1.de;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.Tests.Helpers
{
    public class DummyDESSCD : IDESSCD
    {
        private Task<T> FromResult<T>(T result) => Task.Factory.StartNew(() => result);

        public async Task<TseInfo> GetTseInfoAsync() => await FromResult(new TseInfo());

        public async Task<TseState> SetTseStateAsync(TseState state) => await FromResult(new TseState());

        public Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request) => FromResult(new StartTransactionResponse()
        {
            TimeStamp = DateTime.UtcNow
        });

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request) => await FromResult(new UpdateTransactionResponse()
        {
            TimeStamp = DateTime.UtcNow
        });

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request) => await FromResult(new FinishTransactionResponse()
        {
            StartTransactionTimeStamp = DateTime.UtcNow,
            TimeStamp = DateTime.UtcNow
        });

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request) => await FromResult(new RegisterClientIdResponse());

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request) => await FromResult(new UnregisterClientIdResponse());

        public Task ExecuteSetTseTimeAsync() => Task.Factory.StartNew(() => { return; });

        public Task ExecuteSelfTestAsync() => Task.Factory.StartNew(() => { return; });

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => await FromResult(new StartExportSessionResponse());

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => await FromResult(new StartExportSessionResponse());

        public async Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => await FromResult(new StartExportSessionResponse());

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request) => await FromResult(new ExportDataResponse());

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request) => await FromResult(new EndExportSessionResponse());

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await FromResult(new ScuDeEchoResponse { Message = request.Message });
    }
}
