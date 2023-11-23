using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Service;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.SerialNumbers;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision
{
    public class CryptoVisionSCU : IDESSCD, IDisposable
    {
        private const string _tseOpenTransaction = "Could not delete log files from TSE after successfully exporting them because the following transactions were " +
                                        "open: {OpenTransactions}. If these transactions are not used anymore and could not be closed automatically by a daily closing " +
                                            "receipt, please consider sending a fail-transaction-receipt to cancel them.";
        private int _tseIOTimeout = Constants.DEFAULT_TSE_IO_TIMEOUT;
        private string _devicePath;
        private string deviceFirmwareId;
        private byte[] _adminPin = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }; //12345678
        private byte[] _timeAdminPin = Encoding.UTF8.GetBytes("22222222");
        private byte[] deviceUniqueId;
        private byte[] tseSerialNumber;
        private bool _initialized = false;
        private readonly CryptoVisionConfiguration _configuration;
        private readonly ICryptoVisionProxy _proxy = null;
        private readonly ILogger<CryptoVisionSCU> _logger;
        // don't change this puk-default-values, all existing installations are initialized with this puk. in the moment they are not influenced by configuration
        private readonly byte[] _adminPuk = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a }; //123456789A
        private readonly byte[] _timeAdminPuk = Encoding.UTF8.GetBytes("something!");

        private readonly SemaphoreSlim _hwLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _proxyLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, string> publicKeyBase64Cache = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
        private readonly ConcurrentDictionary<ulong, DateTime> StartTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();
        private readonly ConcurrentDictionary<string, long> _splitExportLastSigCounter = new ConcurrentDictionary<string, long>();
        private readonly KeepAliveTimer _keepAliveTimer;

        private bool disposedValue;
        private bool _enableTarFileExport;
        private DateTime nextSyncTime;

        public TseInfo LastTseInfo { get; private set; }

        public CryptoVisionSCU(CryptoVisionConfiguration configuration, ICryptoVisionProxy cryptoVisionProxy, ILogger<CryptoVisionSCU> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _proxy = cryptoVisionProxy;
            _keepAliveTimer = new KeepAliveTimer(_logger);

            ValidateConfiguration();
            ReadConfiguration();
            if (_configuration.KeepAliveIntervalInSeconds.HasValue)
            {
                _keepAliveTimer.CreateTimer(_configuration.DevicePath, _configuration.KeepAliveIntervalInSeconds.Value * 1000);
            }
        }

        private void ThrowIfDeviceIsNotConnected()
        {
            if (!File.Exists(Path.Combine(_devicePath, Constants.TseIoFile)))
            {
                _initialized = false;
                throw new CryptoVisionException($"The TSE is not available. The file at {Path.Combine(_devicePath, Constants.TseIoFile)} is not available.");
            }
        }

        private async Task EnsureInitializedAsync(TseStates? expectedState = null)
        {
            if (!_initialized)
            {
                await LockingHelper.PerformWithLockAsync(_proxyLock, async () =>
                {
                    if (!_initialized)
                    {
                        try
                        {
                            await _proxy.ResetSeConnectionAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to ResetSeConnectionAsync");
                        }
                        await InitializeProxyAsync();
                        _initialized = true;
                    }
                });
            }
            try
            {
                (var seState, var tseState) = await _proxy.SeGetLifeCycleStateAsync();
                seState.ThrowIfError();
                if (expectedState.HasValue && expectedState != tseState.ToTseStates())
                {
                    throw new CryptoVisionException($"Expected state to be {expectedState} but instead the TSE state was {tseState.ToTseStates()}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read lifecyclestate now");
                _initialized = false;
                throw;
            }
        }

        private async Task UpdateTimeAsync(ICryptoVisionProxy proxy, bool force = false)
        {
            var currentTime = DateTime.UtcNow;
            if (currentTime > nextSyncTime || force)
            {
                (var result, _, _, var timeAdminInTransportState, _) = await proxy.SeGetPinStatesAsync();
                result.ThrowIfError();
                if (!timeAdminInTransportState)
                {
                    await PerformAuthorizedAsync(proxy, Constants.TIMEADMINNAME, _timeAdminPin, async () => (await proxy.SeUpdateTimeAsync()).ThrowIfError());
                    (var timeSyncIntervalResult, var timeSyncInterval) = await proxy.SeGetTimeSyncIntervalAsync();
                    timeSyncIntervalResult.ThrowIfError();
                    nextSyncTime = currentTime.AddSeconds(timeSyncInterval);
                }
            }
        }

        private async Task InitializeProxyAsync()
        {
            try
            {
                _logger.LogDebug("Try to initialize CryptoVisionProxy with devicePath {0}", _devicePath);
                SeResult seResult;
                (seResult, deviceFirmwareId, deviceUniqueId) = await _proxy.SeStartAsync();

                seResult.ThrowIfError();
                await ReadTseInfoAsync(_proxy);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "CryptoVision TSE not accessable at {devicePath}", _devicePath);
                LastTseInfo = new TseInfo();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CryptoVisionProxy not initialized");
                LastTseInfo = new TseInfo();
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_configuration.DevicePath))
            {
                _logger.LogError("devicePath for connecting to the TSE not provided in configuration");
                throw new Exception("devicePath for connecting to the TSE not provided");
            }
        }

        private void ReadConfiguration()
        {
            _devicePath = _configuration.DevicePath.ToString();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_devicePath.EndsWith(":"))
                {
                    _devicePath += "\\";
                }
            }
            _tseIOTimeout = _configuration.TseIOTimeout;
            _enableTarFileExport = _configuration.EnableTarFileExport;
            if (!string.IsNullOrEmpty(_configuration.AdminPin))
            {
                _adminPin = Encoding.UTF8.GetBytes(_configuration.AdminPin);
            }
            if (!string.IsNullOrEmpty(_configuration.TimeAdminPin))
            {
                _timeAdminPin = Encoding.UTF8.GetBytes(_configuration.TimeAdminPin);
            }
        }

        private async Task<DateTime> GetStartTransactionTimeStamp(ICryptoVisionProxy proxy, ulong transactionNumber)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    (await proxy.SeExportTransactionDataAsync(ms, (uint) transactionNumber)).ThrowIfError();
                    ms.Position = 0;
                    var startTransactionLogMessageMoment =
                        LogParser.GetLogsFromTarStream(ms)
                        .OfType<TransactionLogMessage>()
                        .First(l => l.OperationType.ToLower().Contains("start"))
                        .LogTime;
                    return startTransactionLogMessageMoment;
                }
            }
            catch (Exception ex)
            {
                await proxy.ResetSeConnectionAsync();
                _logger.LogWarning(ex, "StartTransactionTimeStamp for TransactionNumber {transactionNumber} not readable from TarExport. Fallback is now {DateTimeUtcNow}.", transactionNumber, DateTime.UtcNow);
                return DateTime.UtcNow;
            }
        }

        private string GetPublicKeyBase64(ICryptoVisionProxy proxy, byte[] serialNumber)
        {
            var serialNumberOctet = serialNumber.ToOctetString();
            if (publicKeyBase64Cache.ContainsKey(serialNumberOctet))
            {
                return publicKeyBase64Cache[serialNumberOctet];
            }
            else
            {
                (var result, var publicKey) = proxy.SeExportPublicKeyAsync(serialNumber).GetAwaiter().GetResult();
                result.ThrowIfError();
                var publicKeyBase64 = publicKey.ToBase64String();
                publicKeyBase64Cache.AddOrUpdate(serialNumberOctet, publicKeyBase64, (key, oldValue) => publicKeyBase64);
                return publicKeyBase64;
            }
        }

        private async Task ReadTseInfoAsync(ICryptoVisionProxy proxy)
        {
            if (proxy == null)
            {
                throw new CryptoVisionException();
            }
            var tseInfo = new TseInfo
            {
                Info = null
            };
            // TODO add static number provided by vendor 
            tseInfo.MaxNumberOfSignatures = 0;
            SeResult lastResult;
            ulong totalLogMemory;
            (lastResult, totalLogMemory) = await proxy.SeGetTotalLogMemoryAsync();
            lastResult.ThrowIfError();
            tseInfo.MaxLogMemorySize = (long) totalLogMemory;

            ulong availableLogMemory;
            (lastResult, availableLogMemory) = await proxy.SeGetAvailableLogMemoryAsync();
            lastResult.ThrowIfError();
            tseInfo.CurrentLogMemorySize = tseInfo.MaxLogMemorySize - (long) availableLogMemory;

            tseInfo.FirmwareIdentification = deviceFirmwareId;
            (lastResult, tseInfo.CertificationIdentification) = await proxy.SeGetCertificationIdAsync();
            lastResult.ThrowIfError();

            SeLifeCycleState lifeCycleState;
            (lastResult, lifeCycleState) = await proxy.SeGetLifeCycleStateAsync();
            lastResult.ThrowIfError();
            tseInfo.CurrentState = lifeCycleState.ToTseStates();

            if (tseInfo.CurrentState == TseStates.Initialized && lifeCycleState == SeLifeCycleState.lcsNoTime)
            {
                await UpdateTimeAsync(proxy, true);
            }

            if (tseInfo.CurrentState == TseStates.Initialized || tseInfo.CurrentState == TseStates.Terminated)
            {
                byte[] serialNumbersBytes;
                (lastResult, serialNumbersBytes) = await proxy.SeExportSerialNumbersAsnyc();
                lastResult.ThrowIfError();
                tseSerialNumber = SerialNumberParser.GetSerialNumbers(serialNumbersBytes).First(snr => snr.IsUsedForTransactionLogs).SerialNumber;
                tseInfo.SerialNumberOctet = tseSerialNumber.ToOctetString();

                byte[] certificateTarFile;
                (lastResult, certificateTarFile) = await proxy.SeExportCertificatesAsync();
                lastResult.ThrowIfError();

                var certificates = LogParser.GetCertificatesFromByteArray(certificateTarFile);
                tseInfo.CertificatesBase64 = certificates.Select(x => Convert.ToBase64String(x.Item1)).ToList();

                byte[] ersMappingsBytes;
                (lastResult, ersMappingsBytes) = await proxy.SeGetERSMappingsAsync();
                lastResult.ThrowIfError();
                tseInfo.CurrentClientIds = ERSMappingHelper.ERSMappingsAsString(ersMappingsBytes);
            }
            else
            {
                tseInfo.CertificatesBase64 = Array.Empty<string>();
                tseInfo.CurrentClientIds = Array.Empty<string>();
            }


            if (tseInfo.CurrentState == TseStates.Initialized)
            {
                byte[] publicKeyBytes;
                (lastResult, publicKeyBytes) = await proxy.SeExportPublicKeyAsync(tseSerialNumber);
                lastResult.ThrowIfError();
                tseInfo.PublicKeyBase64 = Convert.ToBase64String(publicKeyBytes);

                SeSyncVariant timeSyncVariant;
                (lastResult, timeSyncVariant) = await proxy.SeGetTimeSyncVariantAsync();
                lastResult.ThrowIfError();
                tseInfo.LogTimeFormat = timeSyncVariant.ToLogTimeFormat();

                byte[] signatureAlgorithm;
                (lastResult, signatureAlgorithm) = await proxy.SeGetSignatureAlgorithmAsync();
                lastResult.ThrowIfError();
                tseInfo.SignatureAlgorithm = SignatureAlgorithm.NameFromOid(LogParser.GetSignaturAlgorithmOid(signatureAlgorithm).First());

                (lastResult, tseInfo.MaxNumberOfClients) = await proxy.SeGetMaxNumberOfClientsAsync();
                lastResult.ThrowIfError();

                (lastResult, tseInfo.CurrentNumberOfClients) = await proxy.SeGetCurrentNumberOfClientsAsync();
                lastResult.ThrowIfError();

                (lastResult, tseInfo.MaxNumberOfStartedTransactions) = await proxy.SeGetMaxNumberOfTransactionsAsync();
                lastResult.ThrowIfError();

                (lastResult, tseInfo.CurrentNumberOfStartedTransactions) = await proxy.SeGetCurrentNumberOfTransactionsAsync();
                lastResult.ThrowIfError();

                uint[] openTransactions;
                (lastResult, openTransactions) = await proxy.SeGetOpenTransactionsAsync();
                lastResult.ThrowIfError();
                tseInfo.CurrentStartedTransactionNumbers = openTransactions.Select(t => (ulong) t);

                (lastResult, tseInfo.CurrentNumberOfSignatures) = await proxy.SeGetSignatureCounterAsync(tseSerialNumber);
                lastResult.ThrowIfError();

            }
            else
            {
                tseInfo.CurrentStartedTransactionNumbers = Array.Empty<ulong>();
            }

            LastTseInfo = tseInfo;
        }

        private async Task InitializeTseAsync()
        {
            SeResult result;
            bool adminPinInTransportState;
            bool adminPukInTransportState;
            bool timeAdminPinInTransportState;
            bool timeAdminPukInTransportState;
            (result, adminPinInTransportState, adminPukInTransportState, timeAdminPinInTransportState, timeAdminPukInTransportState) = await _proxy.SeGetPinStatesAsync();
            result.ThrowIfError();

            // check if we are in V1 or V2 and call the appropiate method
            var releases = new List<string> { "240346", "425545", "793041" };
            var isV1Hardware = releases.Any(release => deviceFirmwareId.Contains(release));
            if (isV1Hardware)
            {
                if (adminPinInTransportState || adminPukInTransportState || timeAdminPinInTransportState || timeAdminPukInTransportState)
                {
                    (await _proxy.SeInitializePinsAsync(_adminPuk, _adminPin, _timeAdminPuk, _timeAdminPin)).ThrowIfError();
                }
            }
            else
            {

                if (adminPukInTransportState)
                {
                    (await _proxy.SeInitializePinsAsync(Constants.ADMINNAME, _adminPuk)).ThrowIfError();
                }

                if (timeAdminPukInTransportState)
                {
                    (await _proxy.SeInitializePinsAsync(Constants.TIMEADMINNAME, _timeAdminPuk)).ThrowIfError();
                }

                if (adminPinInTransportState)
                {
                    (var seResult, var unblockResult) = await _proxy.SeUnblockUserAsync(Constants.ADMINNAME, _adminPuk, _adminPin);
                    seResult.ThrowIfError();
                    if(unblockResult != SeAuthenticationResult.authenticationOk)
                    {
                        throw new CryptoVisionException("Failed to unblock user " + Constants.ADMINNAME);
                    }

                }

                if (timeAdminPinInTransportState)
                {
                    (var seResult, var unblockResult) = await _proxy.SeUnblockUserAsync(Constants.TIMEADMINNAME, _timeAdminPuk, _timeAdminPin);
                    seResult.ThrowIfError();
                    if (unblockResult != SeAuthenticationResult.authenticationOk)
                    {
                        throw new CryptoVisionException("Failed to unblock user " + Constants.TIMEADMINNAME);
                    }
                }
            }

            SeAuthenticationResult authenticationResult;
            (result, authenticationResult, _) = await _proxy.SeAuthenticateUserAsync(Constants.ADMINNAME, _adminPin);
            result.ThrowIfError();
            if (authenticationResult != SeAuthenticationResult.authenticationOk)
            {
                throw new CryptoVisionException();
            }

            SeLifeCycleState lifeCycleState;
            (result, lifeCycleState) = await _proxy.SeGetLifeCycleStateAsync();
            result.ThrowIfError();

            if (lifeCycleState == SeLifeCycleState.lcsNotInitialized)
            {
                (await _proxy.SeInitializeAsync()).ThrowIfError();
            }
            (await _proxy.SeUpdateTimeAsync()).ThrowIfError();
            (await _proxy.SeLogOutAsync(Constants.ADMINNAME)).ThrowIfError();
            await _proxy.ResetSeConnectionAsync();
            await ReadTseInfoAsync(_proxy);
        }

        private async Task PerformAuthorizedAsync(ICryptoVisionProxy proxy, string username, byte[] pin, Func<Task> method)
        {
            try
            {
                await proxy.SeAuthenticateUserAsync(username, pin);
                await method();
            }
            finally
            {
                await proxy.SeLogOutAsync(username);
            }
        }

        public async Task ExecuteSelfTestAsync() => await Task.CompletedTask;

        public async Task ExecuteSetTseTimeAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    await UpdateTimeAsync(_proxy, true);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(ExecuteSetTseTimeAsync));
                throw;
            }
        }

        public async Task<TseState> SetTseStateAsync(TseState request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync();
                    switch (request.CurrentState)
                    {
                        case TseStates.Initialized:
                        {
                            (var result, var lifeCycleState) = await _proxy.SeGetLifeCycleStateAsync();
                            if (lifeCycleState == SeLifeCycleState.lcsDisabled)
                            {
                                throw new CryptoVisionException("It is not possible to initialize a terminated TSE.");
                            }
                            if (lifeCycleState == SeLifeCycleState.lcsNotInitialized)
                            {
                                await InitializeTseAsync();
                            }
                        };
                        break;
                        case TseStates.Terminated:
                        {
                            if (LastTseInfo.CurrentState == TseStates.Initialized)
                            {
                                await UpdateTimeAsync(_proxy, true);
                                await PerformAuthorizedAsync(_proxy, Constants.ADMINNAME, _adminPin, async () => (await _proxy.SeDisableSecureElementAsync()).ThrowIfError());
                                await _proxy.ResetSeConnectionAsync();
                                await ReadTseInfoAsync(_proxy);
                            }
                        };
                        break;
                        case TseStates.Uninitialized:
                        default:
                            break;
                    }

                    return new TseState { CurrentState = LastTseInfo.CurrentState };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(SetTseStateAsync));
                throw;
            }
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync();
                    await ReadTseInfoAsync(_proxy);
                    return LastTseInfo;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(GetTseInfoAsync));
                throw;
            }
        }

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync();
                    await UpdateTimeAsync(_proxy);
                    await _proxy.SeAuthenticateUserAsync(Constants.ADMINNAME, _adminPin);
                    var SeResult = await _proxy.SeMapERStoKeyAsync(request.ClientId, tseSerialNumber);
                    SeResult.ThrowIfError();
                    await _proxy.SeLogOutAsync(Constants.ADMINNAME);
                    await ReadTseInfoAsync(_proxy);
                    return new RegisterClientIdResponse { ClientIds = LastTseInfo.CurrentClientIds };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(RegisterClientIdAsync));
                throw;
            }
        }

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await Task.FromResult(new UnregisterClientIdResponse { ClientIds = LastTseInfo.CurrentClientIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(UnregisterClientIdAsync));
                throw;
            }
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    if (!LastTseInfo.CurrentClientIds.Contains(request.ClientId))
                    {
                        throw new CryptoVisionException($"The client with the id {request.ClientId} is not registered.");
                    }

                    await UpdateTimeAsync(_proxy);

                    (var result, var startTransactionResult) = await _proxy.SeStartTransactionAsync(request.ClientId, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType);
                    result.ThrowIfError();

                    var logTimeStamp = startTransactionResult.LogUnixTime.ToDateTime();

                    var response = new StartTransactionResponse
                    {
                        ClientId = request.ClientId,
                        TimeStamp = startTransactionResult.LogUnixTime.ToDateTime(),
                        TransactionNumber = startTransactionResult.TransactionNumber,
                        TseSerialNumberOctet = startTransactionResult.SerialNumber.ToOctetString(),
                        SignatureData = new TseSignatureData
                        {
                            PublicKeyBase64 = GetPublicKeyBase64(_proxy, startTransactionResult.SerialNumber),
                            SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                            SignatureBase64 = startTransactionResult.SignatureValue.ToBase64String(),
                            SignatureCounter = startTransactionResult.SignatureCounter
                        }
                    };

                    StartTransactionTimeStampCache.AddOrUpdate(startTransactionResult.TransactionNumber, logTimeStamp, (key, oldValue) => logTimeStamp);
                    return response;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(StartTransactionAsync));
                throw;
            }
        }

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    if (!LastTseInfo.CurrentClientIds.Contains(request.ClientId))
                    {
                        throw new CryptoVisionException($"The client with the id {request.ClientId} is not registered.");
                    }

                    await UpdateTimeAsync(_proxy);
                    (var result, var updateTransactionResult) = await _proxy.SeUpdateTransactionAsync(request.ClientId, (UInt32) request.TransactionNumber, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType);
                    if (result == SeResult.ErrorNoTransaction)
                    {
                        throw new CryptoVisionException($"The transaction with the number {request.TransactionNumber} is either not started or has been finished already. To fix this issue add the 0x0000000020000000  to the daily-closing.");
                    }
                    result.ThrowIfError();

                    return new UpdateTransactionResponse
                    {
                        ClientId = request.ClientId,
                        TransactionNumber = request.TransactionNumber,
                        ProcessType = request.ProcessType,
                        ProcessDataBase64 = request.ProcessDataBase64,
                        TimeStamp = updateTransactionResult.LogUnixTime.ToDateTime(),
                        TseSerialNumberOctet = updateTransactionResult.SerialNumber.ToOctetString(),
                        SignatureData = new TseSignatureData
                        {
                            PublicKeyBase64 = GetPublicKeyBase64(_proxy, updateTransactionResult.SerialNumber),
                            SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                            SignatureBase64 = updateTransactionResult.SignatureValue.ToBase64String(),
                            SignatureCounter = updateTransactionResult.SignatureCounter
                        },
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(UpdateTransactionAsync));
                throw;
            }
        }

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    if (!LastTseInfo.CurrentClientIds.Contains(request.ClientId))
                    {
                        throw new CryptoVisionException($"The client with the id {request.ClientId} is not registered.");
                    }

                    await UpdateTimeAsync(_proxy);
                    (var result, var finishTransactionResult) = await _proxy.SeFinishTransactionAsync(request.ClientId, (UInt32) request.TransactionNumber, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType);
                    if (result == SeResult.ErrorNoTransaction)
                    {
                        throw new CryptoVisionException($"The transaction with the number {request.TransactionNumber} is either not started or has been finished already.To fix this issue add the 0x0000000020000000  to the daily-closing.");
                    }
                    result.ThrowIfError();

                    var startTransactionTimeStamp = finishTransactionResult.LogUnixTime.ToDateTime();
                    if (!StartTransactionTimeStampCache.TryRemove(request.TransactionNumber, out startTransactionTimeStamp))
                    {
                        startTransactionTimeStamp = await GetStartTransactionTimeStamp(_proxy, request.TransactionNumber);
                    }
                    return new FinishTransactionResponse
                    {
                        ClientId = request.ClientId,
                        TransactionNumber = request.TransactionNumber,
                        ProcessType = request.ProcessType,
                        ProcessDataBase64 = request.ProcessDataBase64,
                        StartTransactionTimeStamp = startTransactionTimeStamp,
                        TimeStamp = finishTransactionResult.LogUnixTime.ToDateTime(),
                        TseTimeStampFormat = LastTseInfo?.LogTimeFormat,
                        TseSerialNumberOctet = finishTransactionResult.SerialNumber.ToOctetString(),
                        SignatureData = new TseSignatureData
                        {
                            PublicKeyBase64 = GetPublicKeyBase64(_proxy, finishTransactionResult.SerialNumber),
                            SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                            SignatureBase64 = finishTransactionResult.SignatureValue.ToBase64String(),
                            SignatureCounter = finishTransactionResult.SignatureCounter
                        },
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(FinishTransactionAsync));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                var exportId = Guid.NewGuid();
                if (!_enableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = Constants.NOEXPORT + exportId,
                        TseSerialNumberOctet = LastTseInfo.SerialNumberOctet
                    };
                }
                SetExportState(exportId, ExportState.Running);
                CacheExportAsync(exportId, request.ClientId, request.Erase).FireAndForget();

                return await Task.FromResult(new StartExportSessionResponse()
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = LastTseInfo.SerialNumberOctet
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(StartExportSessionAsync));
                throw;
            }
        }

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new NotImplementedException();

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new NotImplementedException();

        private async Task CacheExportAsync(Guid exportId, string clientId = null, bool eraseData = false)
        {
            await LockingHelper.PerformWithLock(_hwLock, async () =>
            {
                try
                {
                    await EnsureInitializedAsync();

                    if (eraseData)
                    {
                        await AuthenicateAdmin().ConfigureAwait(false);
                        SetEraseEnabledForExportState(exportId, ExportState.Running);
                    }
                    using (var tempStream = File.Open(exportId.ToString(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        (await _proxy.SeExportDataAsync(tempStream, clientId)).ThrowIfError();

                    }
                   SetExportState(exportId, ExportState.Succeeded);
                }
                catch (Exception ex)
                {
                    if (ex is CryptoVisionTimeoutException)
                    {
                        _logger.LogDebug("Requested export was too large to be processed by CryptoVision, splitting into multiple export requests.");
                        await StartExportMoreDataAsync(exportId, tseSerialNumber).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to execute {Operation} - TempFileName: {TempFileName}", nameof(CacheExportAsync), exportId.ToString());
                        SetExportState(exportId, ExportState.Failed, ex);
                    }
                }
            });
        }

        private async Task AuthenicateAdmin()
        {
            (var result, var authenticationResult, _) = await _proxy.SeAuthenticateUserAsync(Constants.ADMINNAME, _adminPin).ConfigureAwait(false);
            result.ThrowIfError();
            if (authenticationResult != SeAuthenticationResult.authenticationOk)
            {
                throw new CryptoVisionException();
            }
        }

        private async Task StartExportMoreDataAsync(Guid exportId, byte[] serialNumber)
        {
            try
            {
                var wait4Tse = 65000 - _configuration.TseIOTimeout;
                if (wait4Tse > 0)
                {
                    await Task.Delay(wait4Tse);
                }
                _proxy.ReOpen();
                _initialized = false;
                await EnsureInitializedAsync();
                (_, var currentNumberOfSignatures) = await _proxy.SeGetSignatureCounterAsync(tseSerialNumber);
                await CacheExportMoreDataAsync(exportId.ToString(), serialNumber, 0, 10000, currentNumberOfSignatures).ConfigureAwait(false);
                TarFileHelper.FinalizeTarFile(exportId.ToString());
                _logger.LogDebug("Finalized merged TAR file {fileName}.", exportId.ToString());
                SetExportState(exportId, ExportState.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - TempFileName: {TempFileName}", nameof(CacheExportMoreDataAsync), exportId.ToString());
                SetExportState(exportId, ExportState.Failed, ex);
            }
        }

        private async Task CacheExportMoreDataAsync(string targetFile, byte[] serialNumber, long previousSignatureCounter, long maxNumberOfRecords, uint currentNumberOfSignatures)
        {
            maxNumberOfRecords = CalcMaxNumberOfRecords(previousSignatureCounter, maxNumberOfRecords, currentNumberOfSignatures);
            long newPreviousSignatureCounter = -1;
            try
            {
                _logger.LogInformation($"Export total {currentNumberOfSignatures} partial Export from {previousSignatureCounter}, number of records: {maxNumberOfRecords}");
                using var stream = new MemoryStream();
                (await _proxy.SeExportMoreDataAsync(stream, serialNumber, previousSignatureCounter, maxNumberOfRecords)).ThrowIfError();
                stream.Position = 0;
                TarFileHelper.AppendTarStreamToTarFile(targetFile, stream);
                newPreviousSignatureCounter = GetLastExportedSignature(targetFile);
                _splitExportLastSigCounter.AddOrUpdate(targetFile, newPreviousSignatureCounter, (k, v) => { v = newPreviousSignatureCounter; return v; });
            }
            catch (CryptoVisionException ex)
            {
                if (!ex.Message.Equals("no data has been found for the provided clientID in the context of the export of stored data"))
                {
                    throw;
                }
            }
            if (newPreviousSignatureCounter == -1)
            {
                newPreviousSignatureCounter = previousSignatureCounter + maxNumberOfRecords;
            }
            if (newPreviousSignatureCounter < currentNumberOfSignatures)
            {
               await CacheExportMoreDataAsync(targetFile, serialNumber, newPreviousSignatureCounter, maxNumberOfRecords, currentNumberOfSignatures).ConfigureAwait(false);
            }
        }

        private long GetLastExportedSignature(string targetFile)
        {
            var lastlog = TarFileHelper.GetLastLogEntryFromTarFile(targetFile);

            //Unixt_1646299459_Sig-50307_Log - Tra_No - 24371_Start_Client - V8uVQZBZzkmULvYaA2sjQ.log
            var iSigStart = lastlog.IndexOf('-');
            var iSigEnd = lastlog.IndexOf('_', iSigStart);
            var lastSigCount = lastlog.Substring(iSigStart + 1, iSigEnd - iSigStart - 1);
            return long.Parse(lastSigCount);
        }

        private long CalcMaxNumberOfRecords(long previousSignatureCounter, long maxNumberOfRecords, uint currentNumberOfSignatures)
        {
            if ((previousSignatureCounter + maxNumberOfRecords) > currentNumberOfSignatures)
            {
                return currentNumberOfSignatures - previousSignatureCounter;
            }
            return maxNumberOfRecords;
        }

        private void SetEraseEnabledForExportState(Guid tokenId, ExportState exportState, Exception error = null)
        {
            _readStreamPointer.AddOrUpdate(tokenId.ToString(), new ExportStateData
            {
                ReadPointer = 0,
                State = exportState,
                EraseEnabled = true
            }, (key, value) =>
            {
                value.State = exportState;
                value.ReadPointer = 0;
                value.Error = error;
                value.EraseEnabled = true;
                return value;
            });
        }

        private void SetExportState(Guid tokenId, ExportState exportState, Exception error = null)
        {
            _readStreamPointer.AddOrUpdate(tokenId.ToString(), new ExportStateData
            {
                ReadPointer = 0,
                State = exportState
            }, (key, value) =>
            {
                value.State = exportState;
                value.ReadPointer = 0;
                value.Error = error;
                return value;
            });
        }

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
        {
            try
            {
                if (request.TokenId.StartsWith(Constants.NOEXPORT))
                {
                    return new ExportDataResponse
                    {
                        TokenId = request.TokenId,
                        TotalTarFileSizeAvailable = true,
                        TotalTarFileSize = 0,
                        TarFileEndOfFile = true
                    };
                }

                var tempFileName = request.TokenId;
                if (!_readStreamPointer.ContainsKey(request.TokenId))
                {
                    throw new CryptoVisionException("The export failed to start. It needs to be retriggered");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateData) && exportStateData.State == ExportState.Failed)
                {
                    throw exportStateData.Error;
                }

                if (exportStateData.State != ExportState.Succeeded || !File.Exists(tempFileName))
                {
                    return new ExportDataResponse
                    {
                        TokenId = request.TokenId,
                        TotalTarFileSize = -1,
                        TarFileEndOfFile = false,
                        TotalTarFileSizeAvailable = false
                    };
                }
                var exportDataResponse = new ExportDataResponse
                {
                    TokenId = request.TokenId
                };
                if (request.MaxChunkSize > 0)
                {
                    var chunkSize = request.MaxChunkSize;
                    using (var tempStream = File.OpenRead(tempFileName))
                    {
                        tempStream.Seek(exportStateData.ReadPointer, SeekOrigin.Begin);

                        if ((tempStream.Length - exportStateData.ReadPointer) < chunkSize)
                        {
                            chunkSize = (int) tempStream.Length - exportStateData.ReadPointer;
                        }
                        var buffer = new byte[chunkSize];
                        var len = await tempStream.ReadAsync(buffer, 0, buffer.Length);
                        exportDataResponse.TarFileByteChunkBase64 = Convert.ToBase64String(buffer.ToArray());
                        exportStateData.ReadPointer += len;
                    }
                }
                exportDataResponse.TotalTarFileSize = new FileInfo(tempFileName).Length;
                exportDataResponse.TotalTarFileSizeAvailable = exportDataResponse.TotalTarFileSize >= 0;
                exportDataResponse.TarFileEndOfFile = exportStateData.ReadPointer == exportDataResponse.TotalTarFileSize;
                return exportDataResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(ExportDataAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                if (request.TokenId.StartsWith(Constants.NOEXPORT))
                {
                    return new EndExportSessionResponse
                    {
                        TokenId = request.TokenId,
                        IsErased = false,
                        IsValid = true
                    };
                }
                if (!_readStreamPointer.ContainsKey(request.TokenId))
                {
                    throw new CryptoVisionException("The export failed to start. It needs to be retriggered");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateData) && exportStateData.State == ExportState.Failed)
                {
                    throw exportStateData.Error;
                }

                var tempFileName = request.TokenId;
                return await LockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    try
                    {
                        await EnsureInitializedAsync();
                        var sessionResponse = new EndExportSessionResponse
                        {
                            TokenId = request.TokenId
                        };

                        using (var tempStream = File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var sha256 = SHA256.Create().ComputeHash(tempStream);
                            if (tempStream.Position != exportStateData.ReadPointer)
                            {
                                throw new CryptoVisionException($"The fetched export doesn´t contain all data. Please call {nameof(ExportDataAsync)} to fetch all data.");
                            }
                            sessionResponse.IsValid = request.Sha256ChecksumBase64 == Convert.ToBase64String(sha256);
                        }

                        await ReadTseInfoAsync(_proxy);

                        if (sessionResponse.IsValid && request.Erase)
                        {
                            if (LastTseInfo.CurrentStartedTransactionNumbers.Any())
                            {
                                var list = string.Join(",", LastTseInfo.CurrentStartedTransactionNumbers.ToArray());
                                _logger.LogWarning(_tseOpenTransaction, list);
                            }else
                            {
                                if (exportStateData.EraseEnabled)
                                {
                                    try
                                    {
                                        await DeleteData(request);
                                        sessionResponse.IsErased = true;
                                    }
                                    catch (CryptoVisionNotAuthenticatedException)
                                    {
                                        await AuthenicateAdmin().ConfigureAwait(false);
                                        await DeleteData(request);
                                        sessionResponse.IsErased = true;
                                    }
                                }
                                else
                                {
                                    sessionResponse.IsErased = false;
                                }
                            }
                        }
                        return sessionResponse;
                    }
                    finally
                    {
                        try
                        {
                            if (request.Erase && exportStateData.EraseEnabled)
                            {
                                await _proxy.SeLogOutAsync(Constants.ADMINNAME);
                            }
                            try
                            {
                                if (File.Exists(tempFileName))
                                {
                                    File.Delete(tempFileName);
                                }
                            }
                            catch { }
                        }
                        catch { }

                        if (_splitExportLastSigCounter.ContainsKey(request.TokenId))
                        {
                            _ = _splitExportLastSigCounter.TryRemove(request.TokenId, out _);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(EndExportSessionAsync));
                throw;
            }
        }

        private async Task DeleteData(EndExportSessionRequest request)
        {
            if (_splitExportLastSigCounter.ContainsKey(request.TokenId))
            {
                _splitExportLastSigCounter.TryGetValue(request.TokenId, out var lastsigCount);
                (await _proxy.SeDeleteDataUpToAsync(tseSerialNumber, (uint) lastsigCount)).ThrowIfError();
            }
            else
            {
                (await _proxy.SeDeleteStoredDataAsync()).ThrowIfError();
            }
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse { Message = request.Message });

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _hwLock?.Dispose();
                _proxy.CloseSeConnectionAsync().Wait();
                _keepAliveTimer.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
