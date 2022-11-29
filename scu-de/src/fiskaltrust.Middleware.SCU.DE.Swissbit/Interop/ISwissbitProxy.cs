using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Models;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop
{
    public interface ISwissbitProxy : IDisposable
    {
        public IntPtr Context { get; }

        public Task InitAsync();
        public Task<bool> UpdateFirmwareAsync(bool firmwareUpdateEnabled);
        public Task CleanupAsync(bool throwException = false);
        public Task<string> GetVersionAsync();
        public Task<string> GetSignatureAlgorithmAsync();
        public Task<string> GetLogTimeFormatAsync();
        public Task<bool> HasValidTimeAsync();
        public Task<bool> HasPassedSelfTestAsync();
        Task<TseStates> GetInitializationState();
        public Task<TseStatusInformation> GetTseStatusAsync();
        public Task TseSetupAsync(byte[] credentialSeed, byte[] adminPuk, byte[] adminPin, byte[] timeAdminPin);
        public Task TseDecommissionAsync();
        public Task TseUpdateTimeAsync();
        public Task TseRunSelfTestAsnyc(bool throwException = true);
        public Task TseRegisterClientAsync(string clientId);
        public Task TseDeregisterClientAsync(string clientId);
        public Task<List<string>> TseGetRegisteredClientsAsync();
        public Task UserLoginAsync(WormUserId id, byte[] pin);
        public Task UserLogoutAsync(WormUserId id);
        public Task<TransactionResponse> TransactionStartAsync(string clientId, byte[] processData, string processType);
        public Task<TransactionResponse> TransactionUpdateAsync(string clientId, UInt64 transactionNumber, byte[] processData, string processType);
        public Task<TransactionResponse> TransactionFinishAsync(string clientId, UInt64 transactionNumber, byte[] processData, string processType);
        public Task<List<ulong>> GetStartedTransactionsAsync(string clientId);
        public Task ExportTarAsync(System.IO.Stream stream);
        public Task ExportTarFilteredTimeAsync(System.IO.Stream stream, UInt64 startDateUnixTime, UInt64 endDateUnixTime, string clientId);
        public Task ExportTarFilteredTransactionAsync(System.IO.Stream stream, UInt64 startTransactionNumber, UInt64 endTransactionNumber, string clientId);
        public Task<byte[]> GetLogMessageCertificateAsync();
        public Task DeleteStoredDataAsync();
    }
}
