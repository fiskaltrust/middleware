using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop
{
    // TODO restructure to inteface
    public interface ICryptoVisionProxy
    {

        #region Maintenance and Time Synchronization

        public abstract Task<SeResult> SeInitializeAsync();
        public abstract Task<SeResult> SeUpdateTimeAsync();
        public abstract Task<SeResult> SeDisableSecureElementAsync();

        #endregion

        #region Input Functions

        public abstract Task<(SeResult, SeStartTransactionResult)> SeStartTransactionAsync(string clientId, byte[] processData, string processType);
        public abstract Task<(SeResult, SeTransactionResult)> SeUpdateTransactionAsync(string clientId, UInt32 transactionNumber, byte[] processData, string processType);
        public abstract Task<(SeResult, SeTransactionResult)> SeFinishTransactionAsync(string clientId, UInt32 transactionNumber, byte[] processData, string processType);

        #endregion

        #region Export Functions

        public abstract Task<SeResult> SeExportDataAsync(Stream stream, string clientId = null, int maximumNumberOfRecords = 0);
        public abstract Task<SeResult> SeExportTransactionDataAsync(Stream stream, uint transactionNumber, string clientId = null, int maximumNumberOfRecords = 0);
        public abstract Task<SeResult> SeExportTransactionRangeDataAsync(Stream stream, UInt32 startTransactionNumber, UInt32 endTransactionNumber, string clientId = null, int maximumNumberOfRecords = 0);
        public abstract Task<SeResult> SeExportDateRangeDataAsync(Stream stream, UInt64 startUnixTime, UInt64 endUnixTime, string clientId = null, int maximumNumberOfRecords = 0);
        public abstract Task<(SeResult, byte[])> SeExportCertificatesAsync();
        public abstract Task<(SeResult, byte[])> SeReadLogMessageAsync();
        public abstract Task<(SeResult, byte[])> SeExportSerialNumbersAsnyc();

        #endregion

        #region Utility Functions

        public abstract Task<SeResult> SeDeleteStoredDataAsync();

        #endregion

        #region Authentication

        public abstract Task<(SeResult, SeAuthenticationResult, Int16 remainingRetries)> SeAuthenticateUserAsync(string userId, byte[] pin);
        public abstract Task<SeResult> SeLogOutAsync(string userId);
        public abstract Task<(SeResult, SeAuthenticationResult)> SeUnblockUserAsync(string userId, byte[] puk, byte[] newPin);

        #endregion

        #region Configuration and Status Information

        #region GetConfigData

        public abstract Task<(SeResult, UInt32 timeSyncIntervalSeconds)> SeGetTimeSyncIntervalAsync();
        public abstract Task<(SeResult, UInt32 maxNumberOfClients)> SeGetMaxNumberOfClientsAsync();
        public abstract Task<(SeResult, UInt32 maxNumberOfTransactions)> SeGetMaxNumberOfTransactionsAsync();
        public abstract Task<(SeResult, SeUpdateVariant)> SeGetSupportedTransactionUpdateVariantAsync();
        public abstract Task<(SeResult, SeSyncVariant)> SeGetTimeSyncVariantAsync();
        public abstract Task<(SeResult, byte[] signatureAlgorithmOid)> SeGetSignatureAlgorithmAsync();
        public abstract Task<(SeResult, string certificationId)> SeGetCertificationIdAsync();

        #endregion

        #region GetStatus

        public abstract Task<(SeResult, SeLifeCycleState)> SeGetLifeCycleStateAsync();
        public abstract Task<(SeResult, UInt32)> SeGetCurrentNumberOfClientsAsync();
        public abstract Task<(SeResult, UInt32)> SeGetCurrentNumberOfTransactionsAsync();
        public abstract Task<(SeResult, UInt32[])> SeGetOpenTransactionsAsync();
        public abstract Task<(SeResult, UInt32)> SeGetTransactionCounterAsync();
        public abstract Task<(SeResult, UInt64)> SeGetTotalLogMemoryAsync();
        public abstract Task<(SeResult, UInt64)> SeGetAvailableLogMemoryAsync();

        #endregion

        #endregion

        #region Additional Commands

        public abstract Task<(SeResult, string deviceVersion, byte[] deviceUniqueId)> SeStartAsync();
        public abstract Task<(SeResult, bool adminPinInTransportState, bool adminPukInTransportState, bool timeAdminPinInTransportState, bool timeAdminPukInTransportState)> SeGetPinStatesAsync();
        public abstract Task<SeResult> SeInitializePinsAsync(byte[] adminPuk, byte[] adminPin, byte[] timeAdminPuk, byte[] timeAdminPin);
        public abstract Task<SeResult> SeInitializePinsAsync(string userId, byte[] userPuk);
        public abstract Task<SeResult> SeMapERStoKeyAsync(string clientId, byte[] serialNumber);
        public abstract Task<SeResult> SeDeactivateAsync();
        public abstract Task<SeResult> SeActivateAsync();
        public abstract Task<SeResult> SeExportMoreDataAsync(Stream stream, byte[] serialNumber, long previousSignatureCounter, long maxNumberOfRecords);
        public abstract Task<SeResult> SeDeleteDataUpToAsync(byte[] serialNumber, UInt32 signatureCounter);
        public abstract Task<(SeResult, byte[])> SeGetERSMappingsAsync();

        #region GetKeyData

        public abstract Task<(SeResult, UInt32)> SeGetSignatureCounterAsync(byte[] serialNumber);
        public abstract Task<(SeResult, UInt64 unixTime)> SeGetCertificateExpirationDateAsync(byte[] serialNumber);
        public abstract Task<(SeResult, byte[])> SeExportPublicKeyAsync(byte[] serialNumber);

        #endregion

        public abstract Task<(SeResult, UInt16)> SeGetWearIndicatorAsync();
        public abstract Task<(SeResult, byte[])> SeUpdateCertificateAsync(byte[] data);
        public abstract Task<(SeResult, byte[])> SeUpdateFirmwareAsync(byte[] data);
        public abstract Task<SeResult> SeShutdownAsync();



        #endregion

        public abstract Task ResetSeConnectionAsync();
        public abstract Task CloseSeConnectionAsync();
        public abstract void ReOpen();

    }
}
