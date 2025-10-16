using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Localization.QueueDE.IntegrationTest
{
    public class InMemorySCU : IDESSCD
    {
        private HashSet<string> _registeredClients { get; set; } = new HashSet<string>();
        private TseState _tseState = new TseState() { CurrentState = TseStates.Uninitialized };
        public bool ShouldFail { get; set; } = false;
        public ulong[] OpenTans { get; set; } = new ulong[0];
        public DateTime? LastSelfTest { get; private set; }
        public DateTime? LastErase { get; private set; }

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request) => ShouldFail ? throw new Exception("ShouldFail") : await Task.FromResult(new FinishTransactionResponse
        {
            SignatureData = new TseSignatureData { },
            ProcessDataBase64 = "MEQCIAy4P9k+7x9saDO0uRZ4El8QwN+qTgYiv1DIaJIMWRiuAiAt+saFDGjK2Yi5Cxgy7PprXQ5O0seRgx4ltdpW9REvwA==",
            ProcessType = request.ProcessType,
            TransactionNumber = 0
        });

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request) => ShouldFail ? throw new Exception("ShouldFail") : await Task.FromResult(new StartTransactionResponse { 
        
            TransactionNumber = 0,
            SignatureData = new TseSignatureData { },
        });

        public Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request) => Task.FromResult(new UpdateTransactionResponse
        {
            TransactionNumber = 0,
            SignatureData = new TseSignatureData { },
        });

        public async Task<TseInfo> GetTseInfoAsync() => ShouldFail ? throw new Exception("ShouldFail") : await Task.FromResult(new TseInfo
        {
            CertificationIdentification = "BSI-TK-0000-0000",
            CurrentState = _tseState.CurrentState,
            CurrentStartedTransactionNumbers = OpenTans

        });

        public async Task<TseState> SetTseStateAsync(TseState state)
        {
            if (ShouldFail)
            {
                throw new Exception("ShouldFail");
            }
            _tseState = state;
            return await Task.FromResult(_tseState);
        }

        public Task ExecuteSetTseTimeAsync() => throw new NotImplementedException();

        public Task ExecuteSelfTestAsync()
        {
            LastSelfTest = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task<StartExportSessionResponse> StartExportSessionAsync() => ShouldFail ? throw new Exception("ShouldFail") : Task.FromResult(new StartExportSessionResponse
        {
            TokenId = Guid.NewGuid().ToString(),
            TseSerialNumberOctet = ""
        });

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new NotImplementedException();

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new NotImplementedException();

        public Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request) => ShouldFail ? throw new Exception("ShouldFail") : Task.FromResult(new ExportDataResponse
        {
            TokenId = Guid.NewGuid().ToString(),
            TarFileByteChunkBase64 = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            TarFileEndOfFile = true,
            TotalTarFileSize = Guid.NewGuid().ToByteArray().Length,
            TotalTarFileSizeAvailable = true
        });

        public virtual Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            if (request.Erase)
            {
                LastErase = DateTime.UtcNow;
            }

            return ShouldFail ? throw new Exception("ShouldFail") : Task.FromResult(new EndExportSessionResponse
            {
                TokenId = Guid.NewGuid().ToString(),
                IsErased = request.Erase,
                IsValid = true
            });
        }

        public Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            _registeredClients.Add(request.ClientId);

            return Task.FromResult(new RegisterClientIdResponse { ClientIds = _registeredClients });
        }

        public Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request)
        {
            _registeredClients.Remove(request.ClientId);

            return Task.FromResult(new UnregisterClientIdResponse { ClientIds = _registeredClients });
        }

        public Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request) => ShouldFail ? throw new Exception("ShouldFail") : Task.FromResult(new StartExportSessionResponse
        {
            TokenId = Guid.NewGuid().ToString(),
            TseSerialNumberOctet = ""
        });

        public Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => throw new NotImplementedException();
    }
}
