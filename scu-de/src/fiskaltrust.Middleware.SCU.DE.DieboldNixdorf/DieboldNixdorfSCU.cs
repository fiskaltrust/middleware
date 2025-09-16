using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public class DieboldNixdorfSCU : IDESSCD, IDisposable
    {
        private const string LOG_TIME_FORMAT = "unixTime"; // 2020-05-29 SKE: We can use this as hardcoded value, because the time for DiboldTSE is always Unix 
        private const long MAX_SIGNATURES = 20_000_000; // 2020-05-29 SKE: We can use this as hardcoded value, because we don´t easily get the maxsignaturecount from the TSE
        private const string NO_EXPORT = "noexport-";
        private const string CACHE_DIR = "Cache";

        private readonly BackgroundSCUTasks _backgroundSCUTasks;
        private readonly string _timeAdminUser;
        private readonly string _timeAdminUserPin;
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;
        private readonly UtilityTseCommandsProvider _utilityTseCommandProvider;
        private readonly MaintenanceTseCommandProvider _maintenanceTseCommandsProvider;
        private readonly AuthenticationTseCommandProvider _authenticationTseCommandProvider;
        private readonly ISerialCommunicationQueue _serialCommunicationQueue;
        private readonly ExportTseCommandsProvider _exportTseCommandsProvider;
        private readonly StandardTseCommandsProvider _standardTseCommandsProvider;
        private readonly TransactionTseCommandsProvider _transactionTseCommandsProvider;
        private readonly ILogger<DieboldNixdorfSCU> _logger;
        private readonly string _transactionCache;
        private readonly DieboldNixdorfConfiguration _configuration;
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
        private readonly ConcurrentDictionary<ulong, DateTime> StartTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();

        private bool _disposed = false;
        private SlotInfo _slotInfo;
        private string _publicKey;
        private string _tseSerialNumber;
        private DateTime _nextSyncTime;

        public DieboldNixdorfSCU(
            ILogger<DieboldNixdorfSCU> logger,
            DieboldNixdorfConfiguration configuration,
            TseCommunicationCommandHelper tseCommunicationCommandHelper,
            BackgroundSCUTasks backgroundSCUTasks,
            UtilityTseCommandsProvider utilityTseCommandsProvider,
            StandardTseCommandsProvider standardTseCommandsProvider,
            TransactionTseCommandsProvider transactionTseCommandsProvider,
            ExportTseCommandsProvider exportTseCommandsProvider,
            MaintenanceTseCommandProvider maintenanceTseCommandProvider,
            AuthenticationTseCommandProvider authenticationTseCommandProvider,
            ISerialCommunicationQueue serialCommunicationQueue)
        {
            _configuration = configuration;
            _backgroundSCUTasks = backgroundSCUTasks;
            _tseCommunicationHelper = tseCommunicationCommandHelper;
            _utilityTseCommandProvider = utilityTseCommandsProvider;
            _standardTseCommandsProvider = standardTseCommandsProvider;
            _transactionTseCommandsProvider = transactionTseCommandsProvider;
            _exportTseCommandsProvider = exportTseCommandsProvider;
            _maintenanceTseCommandsProvider = maintenanceTseCommandProvider;
            _authenticationTseCommandProvider = authenticationTseCommandProvider;
            _serialCommunicationQueue = serialCommunicationQueue;
            _logger = logger;
            _timeAdminUser = configuration.TimeAdminUser;
            _timeAdminUserPin = configuration.TimeAdminPin;

            var cacheDir = Path.Combine(configuration.ServiceFolder, CACHE_DIR);
            Directory.CreateDirectory(cacheDir);
            _transactionCache = Path.Combine(cacheDir, $"startedtransactions-{configuration.SscdId}.json");
            if (File.Exists(_transactionCache))
            {
                var currentTransactionCache = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, DateTime>>(File.ReadAllText(_transactionCache));
                if (currentTransactionCache != null)
                {
                    StartTransactionTimeStampCache = currentTransactionCache;
                }
            }
        }

        private void ThrowIfDeviceIsNotConnected()
        {
            if (!_tseCommunicationHelper.TseConnected)
            {
                throw new DieboldNixdorfException($"The TSE is not available.");
            }
        }

        private void ThrowIfDeviceIsNotAvailable()
        {
            ThrowIfDeviceIsNotConnected();
            try
            {
                var mfcStatus = _tseCommunicationHelper.GetMfcStatus();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Call to MFCStatus failed. We will try to reexecute a SelfTest because a failed mfcstate means that something is wrong with the device.");
                ExecuteSelfTestLogicAsync().Wait();
                _ = _tseCommunicationHelper.GetMfcStatus();
            }
        }

        private void EnsureInitialized(TseStates? expectedState = null)
        {
            var slotInfo = _maintenanceTseCommandsProvider.GetSlotInfo();
            if (expectedState.HasValue && expectedState != slotInfo.ToTseStates())
            {
                throw new DieboldNixdorfException($"Expected state to be {expectedState} but instead the TSE state was {slotInfo.ToTseStates()}.");
            }
        }

        private async Task ExecuteSelfTestLogicAsync()
        {
            await _backgroundSCUTasks.ExecuteSelfTestLogic();
            await UpdateTimeAsync(true);
        }

        private async Task LoadCacheableDataAsync()
        {
            _standardTseCommandsProvider.DisableAsb();
            if (_utilityTseCommandProvider.GetTimeUntilNextSelfTest() == 0)
            {
                await ExecuteSelfTestLogicAsync();
            }
            var mfcState = _tseCommunicationHelper.GetMfcStatus();

            if (_slotInfo == null)
            {
                _slotInfo = _maintenanceTseCommandsProvider.GetSlotInfo();
            }
            if (mfcState.IsInitialized)
            {
                if (_publicKey == null)
                {
                    _publicKey = _exportTseCommandsProvider.ExportPublicKey();
                }
                if (_tseSerialNumber == null)
                {
                    _tseSerialNumber = Convert.FromBase64String(_exportTseCommandsProvider.ExportSerialNumbers().First()).ToOctetString();
                }
            }
        }

        public async Task UpdateTimeAsync(bool force = false)
        {
            var currentTime = DateTime.UtcNow;
            if (currentTime > _nextSyncTime || force)
            {
                var mfcState = _tseCommunicationHelper.GetMfcStatus();
                if (mfcState.IsInitialized)
                {
                    await Task.Run(() =>
                    {
                        _authenticationTseCommandProvider.ExecuteAuthorized(_timeAdminUser, _timeAdminUserPin, () => _maintenanceTseCommandsProvider.UpdateTime());
                        var timeSyncInterval = _utilityTseCommandProvider.GetUpdateTimeInterval();
                        _nextSyncTime = currentTime.AddSeconds(timeSyncInterval);
                    });
                }
            }
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                await LoadCacheableDataAsync();
                var memoryInfo = _maintenanceTseCommandsProvider.GetMemoryInfo();
                var deviceInfo = _maintenanceTseCommandsProvider.GetDeviceInfo();
                var countryInfo = _standardTseCommandsProvider.GetCountryInfo();
                var mfcState = _tseCommunicationHelper.GetMfcStatus();
                _slotInfo = _maintenanceTseCommandsProvider.GetSlotInfo(); // We do reload this stuff to make sure we are getting the latest data
                var tseInfo = new TseInfo
                {
                    CurrentLogMemorySize = memoryInfo.Capacity - memoryInfo.FreeSpace,
                    MaxLogMemorySize = memoryInfo.Capacity,
                    CertificationIdentification = _slotInfo.TSEDescription,
                    SignatureAlgorithm = _slotInfo.SigAlgorithm,
                    CurrentState = mfcState.IsInitialized ? TseStates.Initialized : TseStates.Uninitialized,
                    MaxNumberOfSignatures = MAX_SIGNATURES,
                    LogTimeFormat = LOG_TIME_FORMAT,
                    SerialNumberOctet = _tseSerialNumber,
                    PublicKeyBase64 = _publicKey
                };

                if (countryInfo.HardwareId == DieboldNixdorfHardwareId.SingleTSE)
                {
                    var firmwareInfo = _standardTseCommandsProvider.GetFirmwareInfoForSingleTSE();
                    tseInfo.FirmwareIdentification = $@"Format: SingleTSE; Firmware Version: {firmwareInfo.FwMajorVersion}.{firmwareInfo.FwMinorVersion}.{firmwareInfo.FwBuildNo}; Loader Version: {firmwareInfo.LdrMajorVersion}.{firmwareInfo.LdrMinorVersion}.{firmwareInfo.LdrBuildNo}";
                }
                else if (countryInfo.HardwareId == DieboldNixdorfHardwareId.TSEConnectBox)
                {
                    var firmwareInfo = _standardTseCommandsProvider.GetFirmwareInfoForConnectBox();
                    tseInfo.FirmwareIdentification = $@"Format: ConnectBox; App Version: {firmwareInfo.AppMajorVersion}.{firmwareInfo.AppMinorVersion}.{firmwareInfo.AppBuildNo}; OsUpd Version: {firmwareInfo.OsUpdMajorVersion}.{firmwareInfo.OsUpdMinorVersion}.{firmwareInfo.OsUpdBuildVersion}; OsMain Version: {firmwareInfo.OsMainMajorVersion}.{firmwareInfo.OsMainMinorVersion}.{firmwareInfo.OsMainBuildNo}";
                }

                if (mfcState.IsInitialized)
                {
                    var clients = _authenticationTseCommandProvider.ExecuteAuthorized(_configuration.AdminUser, _configuration.AdminPin, () => _utilityTseCommandProvider.GetRegisteredClients());
                    var certificatesBase64 = _exportTseCommandsProvider.ExportCertificatesAsBase64();

                    try
                    {
                        var certs = certificatesBase64
                        .Select(x => new X509Certificate2(Convert.FromBase64String(x)))
                        .OrderByDescending(cert => cert.NotAfter)
                        .ToList();

                        var cert = certs.FirstOrDefault();
                        tseInfo.Info = new Dictionary<string, object>() { { "TseExpirationDate", cert.NotAfter.ToString("yyyy-MM-dd") } };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse certificate NotAfter date");
                    }


                    var numberOfTransactions = _utilityTseCommandProvider.GetNumberOfTransactions();
                    var numberOfClients = _utilityTseCommandProvider.GetNumberOfClients();
                    var startedTransactions = new List<long>();
                    foreach (var client in clients)
                    {
                        startedTransactions.AddRange(_utilityTseCommandProvider.GetStartedTransactions(client));
                    }
                    tseInfo.MaxNumberOfClients = numberOfClients.MaxNumClients;
                    tseInfo.CurrentNumberOfClients = numberOfClients.CurrentNumClients;
                    tseInfo.CurrentClientIds = clients;
                    tseInfo.CertificatesBase64 = certificatesBase64;
                    tseInfo.CurrentNumberOfStartedTransactions = startedTransactions.Count;
                    tseInfo.CurrentStartedTransactionNumbers = startedTransactions.Select(x => (ulong) x);
                    tseInfo.MaxNumberOfStartedTransactions = numberOfTransactions.MaxNumTransactions;
                    tseInfo.CurrentNumberOfSignatures = MAX_SIGNATURES - _slotInfo.AvailSig;
                }
                return tseInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(GetTseInfoAsync));
                throw;
            }
        }

        public async Task<TseState> SetTseStateAsync(TseState request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                await LoadCacheableDataAsync();
                _standardTseCommandsProvider.DisableAsb();
                var mfcState = _tseCommunicationHelper.GetMfcStatus();
                switch (request.CurrentState)
                {
                    case TseStates.Uninitialized:
                        break;
                    case TseStates.Initialized:
                        if (!mfcState.IsInitialized)
                        {
                            _authenticationTseCommandProvider.ExecuteAuthorized(_configuration.AdminUser, _configuration.AdminPin, () =>
                            {
                                _maintenanceTseCommandsProvider.RegisterClient(TseCommunicationCommandHelper.ManagementClientId);
                                _ = _maintenanceTseCommandsProvider.RunSelfTest();
                                _maintenanceTseCommandsProvider.Initialize("");
                                _maintenanceTseCommandsProvider.UpdateTime();
                            });
                        }
                        break;
                    case TseStates.Terminated:
                        _authenticationTseCommandProvider.ExecuteAuthorized(_configuration.AdminUser, _configuration.AdminPin, () => _maintenanceTseCommandsProvider.Disable());
                        break;
                    default:
                        break;
                }

                mfcState = _tseCommunicationHelper.GetMfcStatus();
                return new TseState
                {
                    CurrentState = mfcState.IsInitialized ? TseStates.Initialized : TseStates.Uninitialized
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                EnsureInitialized(TseStates.Initialized);
                await LoadCacheableDataAsync();
                await UpdateTimeAsync();
                var processData = Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty);
                var result = await CallWithUpdateTimeRetryAsync(() => _transactionTseCommandsProvider.StartTransaction(request.ClientId, processData, request.ProcessType ?? string.Empty));
                StartTransactionTimeStampCache.AddOrUpdate(result.TransactionNo, result.LogTime, (key, oldValue) => result.LogTime);

                File.WriteAllText(_transactionCache, JsonConvert.SerializeObject(StartTransactionTimeStampCache));

                return new StartTransactionResponse
                {
                    ClientId = request.ClientId,
                    TimeStamp = result.LogTime,
                    TseSerialNumberOctet = Convert.FromBase64String(result.SerialNoBase64).ToOctetString(),
                    TransactionNumber = result.TransactionNo,
                    SignatureData = new TseSignatureData()
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _slotInfo.SigAlgorithm,
                        SignatureBase64 = result.SignatureBase64,
                        SignatureCounter = result.SignatureCounter
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                EnsureInitialized(TseStates.Initialized);
                await LoadCacheableDataAsync();
                await UpdateTimeAsync();
                var processData = Convert.FromBase64String(request.ProcessDataBase64);
                var result = await CallWithUpdateTimeRetryAsync(() => _transactionTseCommandsProvider.UpdateTransaction(request.ClientId, request.TransactionNumber, processData, request.ProcessType));
                return new UpdateTransactionResponse
                {
                    ClientId = request.ClientId,
                    TransactionNumber = request.TransactionNumber,
                    ProcessType = request.ProcessType,
                    ProcessDataBase64 = request.ProcessDataBase64,
                    TimeStamp = result.LogTime,
                    TseSerialNumberOctet = _tseSerialNumber,
                    SignatureData = new TseSignatureData()
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _slotInfo.SigAlgorithm,
                        SignatureBase64 = result.SignatureBase64,
                        SignatureCounter = result.SignatureCounter
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(UpdateTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                EnsureInitialized(TseStates.Initialized);
                await LoadCacheableDataAsync();
                await UpdateTimeAsync();
                var processData = Convert.FromBase64String(request.ProcessDataBase64);
                var result = await CallWithUpdateTimeRetryAsync(() => _transactionTseCommandsProvider.FinishTransaction(request.ClientId, request.TransactionNumber, processData, request.ProcessType));
                StartTransactionTimeStampCache.TryRemove(request.TransactionNumber, out var startTransactionTimeStamp);
                File.WriteAllText(_transactionCache, JsonConvert.SerializeObject(StartTransactionTimeStampCache));

                return new FinishTransactionResponse
                {
                    ClientId = request.ClientId,
                    TransactionNumber = request.TransactionNumber,
                    ProcessType = request.ProcessType,
                    ProcessDataBase64 = request.ProcessDataBase64,
                    StartTransactionTimeStamp = startTransactionTimeStamp,
                    TimeStamp = result.LogTime,
                    TseTimeStampFormat = LOG_TIME_FORMAT,
                    TseSerialNumberOctet = _tseSerialNumber,
                    SignatureData = new TseSignatureData()
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _slotInfo.SigAlgorithm,
                        SignatureBase64 = result.SignatureBase64,
                        SignatureCounter = result.SignatureCounter
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private async Task<T> CallWithUpdateTimeRetryAsync<T>(Func<T> method)
        {
            try
            {
                return method();
            }
            catch (DieboldNixdorfException ex) when (ex.Message == "E_TIME_NOT_SET")
            {
                await UpdateTimeAsync(true);
                return method();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _backgroundSCUTasks.Dispose();
                    _serialCommunicationQueue.Dispose();
                }
                _disposed = true;
            }
        }

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                EnsureInitialized(TseStates.Initialized);
                await LoadCacheableDataAsync();
                return _authenticationTseCommandProvider.ExecuteAuthorized(_configuration.AdminUser, _configuration.AdminPin, () =>
                {
                    _maintenanceTseCommandsProvider.RegisterClient(request.ClientId);
                    return new RegisterClientIdResponse
                    {
                        ClientIds = _utilityTseCommandProvider.GetRegisteredClients()
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(RegisterClientIdAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                EnsureInitialized(TseStates.Initialized);
                await LoadCacheableDataAsync();
                return _authenticationTseCommandProvider.ExecuteAuthorized(_configuration.AdminUser, _configuration.AdminPin, () =>
                {
                    _maintenanceTseCommandsProvider.DeregisterClient(request.ClientId);
                    return new UnregisterClientIdResponse
                    {
                        ClientIds = _utilityTseCommandProvider.GetRegisteredClients()
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(UnregisterClientIdAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task ExecuteSetTseTimeAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                EnsureInitialized(TseStates.Initialized);
                await UpdateTimeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(ExecuteSetTseTimeAsync));
                throw;
            }
        }

        public async Task ExecuteSelfTestAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await ExecuteSelfTestLogicAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(ExecuteSelfTestAsync));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                await LoadCacheableDataAsync();
                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = NO_EXPORT + Guid.NewGuid().ToString(),
                        TseSerialNumberOctet = _tseSerialNumber
                    };
                }
                if (request.Erase)
                {
                    _authenticationTseCommandProvider.LoginUser(_configuration.AdminUser, _configuration.AdminPin);
                }
                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                Task.Run(() =>
                {
                    try
                    {
                        var export = _exportTseCommandsProvider.ExportAll();
                        foreach (var tarFile in export)
                        {
                            File.WriteAllBytes(tokenId.ToString(), Convert.FromBase64String(tarFile));
                        }
                        SetExportState(tokenId, ExportState.Succeeded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background task to export data failed");
                        SetExportState(tokenId, ExportState.Failed, ex);
                    }
                }).FireAndForget();
                return new StartExportSessionResponse
                {
                    TokenId = tokenId.ToString(),
                    TseSerialNumberOctet = _tseSerialNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
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

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                await LoadCacheableDataAsync();
                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                Task.Run(() =>
                {
                    try
                    {
                        var export = _exportTseCommandsProvider.ExportByTimePeriod(request.From, request.To, request.ClientId);
                        foreach (var tarFile in export)
                        {
                            File.WriteAllBytes(tokenId.ToString(), Convert.FromBase64String(tarFile));
                        }
                        SetExportState(tokenId, ExportState.Succeeded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background task to export data failed");
                        SetExportState(tokenId, ExportState.Failed, ex);
                    }
                }).FireAndForget();
                return new StartExportSessionResponse
                {
                    TokenId = tokenId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionByTimeStampAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                await LoadCacheableDataAsync();
                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                Task.Run(() =>
                {
                    try
                    {
                        var export = _exportTseCommandsProvider.ExportByTransactionNoInterval(request.From, request.To, request.ClientId);
                        foreach (var tarFile in export)
                        {
                            File.WriteAllBytes(tokenId.ToString(), Convert.FromBase64String(tarFile));
                        }
                        SetExportState(tokenId, ExportState.Succeeded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background task to export data failed");
                        SetExportState(tokenId, ExportState.Failed, ex);
                    }
                }).FireAndForget();
                return new StartExportSessionResponse
                {
                    TokenId = tokenId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionByTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotAvailable();
                if (request.TokenId.StartsWith(NO_EXPORT))
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
                    throw new DieboldNixdorfException("The export failed to start. It needs to be retriggered");
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
                ThrowIfDeviceIsNotAvailable();
                if (request.TokenId.StartsWith(NO_EXPORT))
                {
                    return new EndExportSessionResponse
                    {
                        TokenId = request.TokenId,
                        IsErased = false,
                        IsValid = true
                    };
                }
                var tempFileName = request.TokenId;
                try
                {
                    var sessionResponse = new EndExportSessionResponse
                    {
                        TokenId = request.TokenId
                    };
                    return await Task.Run(() =>
                    {
                        using (var tempStream = File.OpenRead(tempFileName))
                        {
                            var sha256 = SHA256.Create().ComputeHash(tempStream);
                            if (_readStreamPointer[request.TokenId].ReadPointer == tempStream.Position && request.Sha256ChecksumBase64 == Convert.ToBase64String(sha256))
                            {
                                sessionResponse.IsValid = true;
                                if (request.Erase)
                                {
                                    var result = _exportTseCommandsProvider.DeleteExportedData(new FileInfo(tempFileName).Length);
                                    if (result == 2)
                                    {
                                        _logger.LogWarning("Failed to delete data from tse. Data not completely exported.");
                                    }
                                    else
                                    {
                                        sessionResponse.IsErased = true;
                                    }
                                }
                                return sessionResponse;
                            }
                        }
                        return sessionResponse;
                    });
                }
                finally
                {
                    if (request.Erase)
                    {
                        _authenticationTseCommandProvider.LogoutUser(_configuration.AdminPin);
                    }
                    try
                    {
                        if (File.Exists(tempFileName))
                        {
                            File.Delete(tempFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete file {0} after succesfull export.", tempFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(EndExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse
        {
            Message = request.Message
        });
    }
}