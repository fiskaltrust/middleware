using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Exceptions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;


namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public sealed class SwissbitSCU : IDESSCD, IDisposable
    {
        private const string NO_EXPORT = "noexport-";
        private const string TSE_INFO_DAT = "TSE_INFO.DAT";

        private readonly SemaphoreSlim _hwLock = new SemaphoreSlim(1, 1);
        private readonly object _proxyLock = new object();
        private readonly Timer _selftestTimer;
        private readonly SwissbitSCUConfiguration _configuration;
        private readonly INativeFunctionPointerFactory _nativeFunctionPointerFactory;
        private readonly ILogger<SwissbitSCU> _logger;
        private readonly LockingHelper _lockingHelper;
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
        private readonly ConcurrentDictionary<ulong, DateTime> _startTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();

        private byte[] _certificatesBytes = null;
        private TimeSpan _selftestInterval = TimeSpan.FromHours(24);
        private uint _hwSelftestIntervalSeconds = 0;
        private ISwissbitProxy _proxy = null;
        private DateTime _nextSyncTime;
        private ulong _lastTransactionNr = 0;

        // Never change these values, as all existing installations are depending on them
        private readonly byte[] _adminPuk = Encoding.ASCII.GetBytes("123456");
        private readonly byte[] _seed = Encoding.ASCII.GetBytes("SwissbitSwissbit");

        public TseInfo LastTseInfo { get; private set; }

        public SwissbitSCU(SwissbitSCUConfiguration configuration, INativeFunctionPointerFactory nativeFunctionPointerFactory, ILogger<SwissbitSCU> logger, LockingHelper lockingHelper)
        {
            _configuration = configuration;
            _nativeFunctionPointerFactory = nativeFunctionPointerFactory;
            _logger = logger;
            _lockingHelper = lockingHelper;

            EnsureDevicePathIsCorrect();

            _selftestTimer = new Timer(SelftestCallback, null, Timeout.Infinite, Timeout.Infinite);

            try
            {
                _ = GetProxy();
            }
            catch (NativeLibraryException se)
            {
                _logger.LogCritical(se, "This issue can occur if the Visual Studio C++ 2015 Redistributable was not found on this system. Please install it and try again.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occured while initializing the Swissbit SCU.");
                throw;
            }
        }

        private ISwissbitProxy GetProxy([CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _logger.LogTrace("GetProxy called from {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
            lock (_proxyLock)
            {
                _logger.LogTrace("Obtained proxy lock for {CallerMemberName}:{CallerLineNumber}.", memberName, sourceLineNumber);
                if (_proxy == null)
                {
                    try
                    {
                        //maybe move to proxy
                        if (!Directory.Exists(_configuration.DevicePath))
                        {
                            throw new SwissbitException($"The Swissbit TSE cannot be found at {_configuration.DevicePath}.");
                        }

                        _logger.LogDebug("Try to initialize SwissbitProxy with devicePath {_configuration.DevicePath}", _configuration.DevicePath);
                        _proxy = new SwissbitProxy(_configuration.DevicePath, Encoding.ASCII.GetBytes(_configuration.AdminPin), Encoding.ASCII.GetBytes(_configuration.TimeAdminPin), _nativeFunctionPointerFactory, _lockingHelper, _logger);
                        ReadTseInfoAsync(_proxy, true).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SwissbitProxy not initialized");
                        LastTseInfo = new TseInfo();
                    }
                }
            }
            return _proxy;
        }

        private void ThrowIfDeviceIsNotConnected()
        {
            if (!File.Exists(Path.Combine(_configuration.DevicePath, TSE_INFO_DAT)))
            {
                throw new SwissbitException($"The TSE is not available. The file at {Path.Combine(_configuration.DevicePath, TSE_INFO_DAT)} is not available.");
            }
        }

        private async Task EnsureInitializedAsync(TseStates? expectedState = null)
        {
            try
            {
                var tseState = await GetProxy().GetInitializationState();
                if (expectedState.HasValue && expectedState != tseState)
                {
                    throw new SwissbitException($"Expected state to be {expectedState} but instead the TSE state was {tseState}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read lifecyclestate now");
                DestroyProxy();
                throw;
            }
        }

        private void DestroyProxy()
        {
            try
            {
                _proxy?.Dispose();
            }
            finally
            {
                _proxy = null;
            }
        }

        private void EnsureDevicePathIsCorrect()
        {
            if (string.IsNullOrEmpty(_configuration.DevicePath))
            {
                _logger.LogError("devicePath for connecting to the TSE not provided in configuration");
                throw new Exception("devicePath for connecting to the TSE not provided");
            }

            if (_configuration.DevicePath.Length == 1)
            {
                // Only drive letter provided
                _configuration.DevicePath += ":";
            }
        }

        private async Task UpdateTimeAsync(ISwissbitProxy proxy, bool force = false)
        {
            if (!await proxy.HasPassedSelfTestAsync())
            {
                await SelftestAsync(proxy, true);
            }

            var currentTime = DateTime.UtcNow;
            if (!force && await proxy.HasValidTimeAsync())
            {
                return;
            }

            var tseStatus = await proxy.GetTseStatusAsync();
            if (!tseStatus.IsCtssInterfaceActive)
            {
                _logger.LogWarning("Tried to call UpdateTime, but the Ctss Interface hasn´t been activated.");
            }
            else if (!tseStatus.HasChangedTimeAdminPin)
            {
                _logger.LogWarning("Tried to call UpdateTime, but the TimeAdmin Pin has never been changed.");
            }
            else
            {
                await proxy.TseUpdateTimeAsync();
                _nextSyncTime = currentTime.AddSeconds(tseStatus.MaxTimeSynchronizationDelay);
            }
        }

        private async void SelftestCallback(object state)
        {
            try
            {
                _logger.LogDebug("Self test timer hit, starting self test...");
                await _lockingHelper.PerformWithLock(_hwLock, async () => await SelftestAsync(GetProxy(), true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SelftestCallback failed");
            }
        }

        private async Task SelftestAsync(ISwissbitProxy proxy, bool force = false)
        {
            if (!force && await proxy.HasPassedSelfTestAsync())
            {
                _logger.LogDebug("Self test already passed, skipping.");
                return;
            }

            if (proxy.Context.ToInt64() == 0x00)
            {
                _logger.LogWarning("Performing SelfTest is impossible because the proxy has an invalid state.");
            }
            else
            {
                _logger.LogDebug("Reading self test status..");
                var tseStatus = await proxy.GetTseStatusAsync();
                _logger.LogDebug("Received TSE status information: {TseStatus}", JsonConvert.SerializeObject(tseStatus));

                _logger.LogDebug("Starting self test..");
                await proxy.TseRunSelfTestAsnyc(tseStatus.initializationState == WormInitializationState.WORM_INIT_INITIALIZED);
                tseStatus = await proxy.GetTseStatusAsync();
                _logger.LogDebug("Received new TSE status information: {TseStatus}", JsonConvert.SerializeObject(tseStatus));
                _hwSelftestIntervalSeconds = tseStatus.TimeUntilNextSelfTest;
                _selftestInterval = TimeSpan.FromSeconds(_hwSelftestIntervalSeconds * 0.80);
                _selftestTimer.Change(_selftestInterval, _selftestInterval);
            }
        }

        private async Task<DateTime> GetStartTransactionTimeStamp(ISwissbitProxy proxy, ulong transactionNumber)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    await proxy.ExportTarFilteredTransactionAsync(ms, transactionNumber, transactionNumber, string.Empty);

                    ms.Position = 0;
                    var startTransactionLogMessageMoment =
                        LogParser.GetLogsFromTarStream(ms)
                        .OfType<TransactionLogMessage>()
                        .Where(l => l.TransactionNumber == transactionNumber)
                        .First(l => l.OperationType.ToLower().Contains("start"))
                        .LogTime;
                    return startTransactionLogMessageMoment;
                }
            }
            catch (Exception ex)
            {
                var unknownStartTransactionMoment = DateTime.UtcNow;
                _logger.LogWarning(ex, "StartTransactionTimeStamp for TransactionNumber {transactionNumber} not readable from TarExport. Fallback is now {DateTimeUtcNow}.", transactionNumber, unknownStartTransactionMoment);
                return unknownStartTransactionMoment;
            }
        }

        private async Task<byte[]> GetCertificatesAsync(ISwissbitProxy proxy)
        {
            // returns a complete certificate chain as a PEM-file. the first certificate in the chain can be used to verify the signatures of the log-messages.

            if (_certificatesBytes == null)
            {
                _certificatesBytes = await proxy.GetLogMessageCertificateAsync();
            }
            return _certificatesBytes;
        }

        private async Task<List<string>> GetCertificateBase64ListAsync(ISwissbitProxy proxy)
        {
            // TODO split certificate chain

            return new List<string> { Convert.ToBase64String(await GetCertificatesAsync(proxy)) };
        }

        private async Task ReadTseInfoAsync(ISwissbitProxy proxy, bool initLibrary = false)
        {
            if (initLibrary)
            {
                try
                {
                    _logger.LogDebug("Cleaning up WORM library before initializing it..");
                    await proxy.CleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Cleanup was not successful.");
                }
                _logger.LogDebug("Initializing WORM library..");
                await proxy.InitAsync();
                await SelftestAsync(proxy);
                await UpdateTimeAsync(proxy);

                if (await proxy.UpdateFirmwareAsync(_configuration.EnableFirmwareUpdate))
                {
                    await SelftestAsync(proxy);
                }
                await UpdateTimeAsync(GetProxy());
            }

            var status = await proxy.GetTseStatusAsync();
            var tseInfo = new TseInfo()
            {
                Info = null
            };

            const long blockSize = 0x200; //512 byte


            var softwareVersion = ConvertToVersion((int) status.SoftwareVersion);
            var hardwareVersion = ConvertToVersion((int) status.HardwareVersion);

            tseInfo.MaxNumberOfClients = status.MaxRegisteredClients;
            tseInfo.MaxNumberOfStartedTransactions = status.MaxStartedTransactions;
            tseInfo.MaxNumberOfSignatures = status.MaxSignatures;
            tseInfo.MaxLogMemorySize = status.CapacityInBlocks * blockSize;
            tseInfo.CurrentState = status.initializationState.ToTseStates();
            tseInfo.FirmwareIdentification = $@"Formatfactor: {status.FormFactor}; Software Version: {softwareVersion}; Hardware Version: {hardwareVersion}";
            tseInfo.CertificationIdentification = status.TseDescription;
            tseInfo.SignatureAlgorithm = await proxy.GetSignatureAlgorithmAsync();
            tseInfo.LogTimeFormat = await proxy.GetLogTimeFormatAsync();
            tseInfo.SerialNumberOctet = status.TseSerialNumber.ToOctetString();
            tseInfo.PublicKeyBase64 = Convert.ToBase64String(status.TsePublicKey);
            if (tseInfo.CurrentState == TseStates.Initialized)
            {
                tseInfo.CurrentNumberOfClients = status.RegisteredClients;
                tseInfo.CurrentNumberOfStartedTransactions = status.StartedTransactions;
                tseInfo.CurrentNumberOfSignatures = status.CreatedSignatures;
                tseInfo.CurrentLogMemorySize = status.SizeInBlocks * blockSize;
                try
                {
                    await InitializeExportedDataAsync(proxy, tseInfo);
                }
                catch (SwissbitException ex) when (ex.Error == WormError.WORM_ERROR_NOT_ALLOWED_EXPORT_IN_PROGRESS)
                {
                    _logger.LogWarning(ex, "A filtered export is currently running. Performing SelfTest to correct behavior.");
                    await SelftestAsync(proxy, true);
                    await UpdateTimeAsync(proxy);
                    await InitializeExportedDataAsync(proxy, tseInfo);
                }
            }
            else
            {
                tseInfo.CurrentClientIds = Array.Empty<string>();
                tseInfo.CurrentStartedTransactionNumbers = Array.Empty<ulong>();
                tseInfo.CertificatesBase64 = Array.Empty<string>();
            }
            LastTseInfo = tseInfo;
        }

        private async Task InitializeExportedDataAsync(ISwissbitProxy proxy, TseInfo tseInfo)
        {
            tseInfo.CertificatesBase64 = await GetCertificateBase64ListAsync(proxy);
            tseInfo.CurrentClientIds = await proxy.TseGetRegisteredClientsAsync();
            tseInfo.CurrentStartedTransactionNumbers = await proxy.GetStartedTransactionsAsync(string.Empty);
        }

        private static Version ConvertToVersion(int versionBytes)
        {
            var major = (versionBytes >> 16) & 0xffff;
            var minor = (versionBytes >> 8) & 0xff;
            var patch = versionBytes & 0xff;
            return new Version(major, minor, patch);
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync();
                    try
                    {
                        await ReadTseInfoAsync(GetProxy());
                        return LastTseInfo;
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(GetTseInfoAsync));
                throw;
            }
        }

        public async Task<TseState> SetTseStateAsync(TseState state)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync();
                    try
                    {
                        var tseStatus = await GetProxy().GetTseStatusAsync();
                        switch (state.CurrentState)
                        {
                            case TseStates.Uninitialized:
                                break;
                            case TseStates.Initialized:
                                if (tseStatus.initializationState == WormInitializationState.WORM_INIT_DECOMMISSIONED)
                                {
                                    throw new SwissbitException("It is not possible to initialize a terminated TSE.");
                                }
                                else if (tseStatus.initializationState == WormInitializationState.WORM_INIT_UNINITIALIZED)
                                {
                                    await GetProxy().TseSetupAsync(_seed, _adminPuk, Encoding.ASCII.GetBytes(_configuration.AdminPin), Encoding.ASCII.GetBytes(_configuration.TimeAdminPin));
                                    await SelftestAsync(GetProxy());
                                    await UpdateTimeAsync(GetProxy());
                                }
                                break;
                            case TseStates.Terminated:
                                if (tseStatus.initializationState == WormInitializationState.WORM_INIT_INITIALIZED)
                                {
                                    await SelftestAsync(GetProxy());
                                    await UpdateTimeAsync(GetProxy());
                                    await GetProxy().TseDecommissionAsync();
                                }
                                break;
                            default:
                                break;
                        }

                        await ReadTseInfoAsync(GetProxy());
                        return new TseState { CurrentState = LastTseInfo.CurrentState };
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(SetTseStateAsync));
                throw;
            }
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    try
                    {
                        await EnsureInitializedAsync(TseStates.Initialized);
                        await UpdateTimeAsync(GetProxy());

                        var tseResponse = await GetProxy().TransactionStartAsync(request.ClientId, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType ?? string.Empty);

                        var logTimeStamp = tseResponse.LogTime.LinuxTimestampToDateTime();
                        var response = new StartTransactionResponse()
                        {
                            ClientId = request.ClientId,
                            TimeStamp = logTimeStamp,
                            TseSerialNumberOctet = tseResponse.SerialNumber.ToOctetString(),
                            TransactionNumber = tseResponse.TransactionNumber,
                            SignatureData = new TseSignatureData()
                            {
                                PublicKeyBase64 = LastTseInfo?.PublicKeyBase64,
                                SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                                SignatureBase64 = tseResponse.SignatureBase64,
                                SignatureCounter = tseResponse.SignatureCounter
                            }
                        };

                        _startTransactionTimeStampCache.AddOrUpdate(tseResponse.TransactionNumber, logTimeStamp, (key, oldValue) => logTimeStamp);

                        return response;
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
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
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    try
                    {
                        await EnsureInitializedAsync(TseStates.Initialized);
                        await UpdateTimeAsync(GetProxy());

                        var tseResponse = await GetProxy().TransactionUpdateAsync(request.ClientId, (UInt64) request.TransactionNumber, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType ?? string.Empty);

                        if (tseResponse.TransactionNumber != request.TransactionNumber)
                        {
                            throw new SwissbitException();
                        }

                        var response = new UpdateTransactionResponse()
                        {
                            ClientId = request.ClientId,
                            TransactionNumber = tseResponse.TransactionNumber,
                            ProcessType = request.ProcessType,
                            ProcessDataBase64 = request.ProcessDataBase64,
                            TimeStamp = tseResponse.LogTime.LinuxTimestampToDateTime(),
                            TseSerialNumberOctet = tseResponse.SerialNumber.ToOctetString(),
                            SignatureData = new TseSignatureData()
                            {
                                PublicKeyBase64 = LastTseInfo?.PublicKeyBase64,
                                SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                                SignatureBase64 = tseResponse.SignatureBase64,
                                SignatureCounter = tseResponse.SignatureCounter
                            }
                        };

                        return response;
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
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
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    try
                    {
                        await EnsureInitializedAsync(TseStates.Initialized);
                        await UpdateTimeAsync(GetProxy());

                        var tseResponse = await GetProxy().TransactionFinishAsync(request.ClientId, (UInt64) request.TransactionNumber, Convert.FromBase64String(request.ProcessDataBase64 ?? string.Empty), request.ProcessType ?? string.Empty);

                        if (tseResponse.TransactionNumber != request.TransactionNumber)
                        {
                            throw new SwissbitException();
                        }

                        var startTransactionTimeStamp = tseResponse.LogTime.LinuxTimestampToDateTime();
                        if (!_startTransactionTimeStampCache.TryRemove(tseResponse.TransactionNumber, out startTransactionTimeStamp))
                        {
                            // If the TSE log memory is too full, this call takes too long, and transactions cannot be canceled anymore - basically creating a deadlock.
                            // Thus, we skip reading the timestamp of the start-transaction and fall-back to the one of the finish-transaction.
                            if (!IsCancellationTransaction(request) || LastTseInfo?.CurrentLogMemorySize < _configuration.TooLargeToExportThreshold)
                            {
                                startTransactionTimeStamp = await GetStartTransactionTimeStamp(GetProxy(), request.TransactionNumber);
                            }
                            else
                            {
                                _logger.LogWarning("Could not set StartTransactionTimestamp, as the TSE's log storage is too full to extract it. Used finish-transaction log time as fallback value.");
                            }
                        }

                        var response = new FinishTransactionResponse()
                        {
                            ClientId = request.ClientId,
                            TransactionNumber = tseResponse.TransactionNumber,
                            ProcessType = request.ProcessType,
                            ProcessDataBase64 = request.ProcessDataBase64,
                            StartTransactionTimeStamp = startTransactionTimeStamp,
                            TimeStamp = tseResponse.LogTime.LinuxTimestampToDateTime(),
                            TseTimeStampFormat = LastTseInfo?.LogTimeFormat,
                            TseSerialNumberOctet = tseResponse.SerialNumber.ToOctetString(),
                            SignatureData = new TseSignatureData()
                            {
                                PublicKeyBase64 = LastTseInfo?.PublicKeyBase64,
                                SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                                SignatureBase64 = tseResponse.SignatureBase64,
                                SignatureCounter = tseResponse.SignatureCounter
                            }
                        };
                        _lastTransactionNr = tseResponse.TransactionNumber;

                        return response;
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(FinishTransactionAsync));
                throw;
            }
        }

        private bool IsCancellationTransaction(FinishTransactionRequest request)
        {
            if (string.IsNullOrEmpty(request.ProcessDataBase64))
            {
                return false;
            }
            var processData = Encoding.UTF8.GetString(Convert.FromBase64String(request.ProcessDataBase64));
            return request.ProcessType == "Kassenbeleg-V1" && processData.StartsWith("AVBelegabbruch^");
        }

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    try
                    {
                        await UpdateTimeAsync(GetProxy());

                        await GetProxy().TseRegisterClientAsync(request.ClientId);

                        LastTseInfo.CurrentClientIds = await GetProxy().TseGetRegisteredClientsAsync();

                        return new RegisterClientIdResponse { ClientIds = LastTseInfo.CurrentClientIds };
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
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
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    try
                    {
                        await UpdateTimeAsync(GetProxy());

                        await GetProxy().TseDeregisterClientAsync(request.ClientId);

                        LastTseInfo.CurrentClientIds = await GetProxy().TseGetRegisteredClientsAsync();

                        return new UnregisterClientIdResponse { ClientIds = LastTseInfo.CurrentClientIds };
                    }
                    catch
                    {
                        DestroyProxy();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(UnregisterClientIdAsync));
                throw;
            }
        }

        public async Task ExecuteSetTseTimeAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    await EnsureInitializedAsync(TseStates.Initialized);
                    await UpdateTimeAsync(GetProxy(), true);
                });
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
                await SelftestAsync(GetProxy(), true);
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
                ThrowIfDeviceIsNotConnected();
                var exportId = Guid.NewGuid();
                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = NO_EXPORT + exportId,
                        TseSerialNumberOctet = LastTseInfo.SerialNumberOctet
                    };
                }

                await UpdateTimeAsync(GetProxy());
                SetExportState(exportId, ExportState.Running);

                if (_configuration.ChunkExportTransactionCount > 0)
                {
                    if (_lastTransactionNr == 0)
                    {
                        _logger.LogWarning("Before executing a partial export a daily closing has to be made.");
                        throw new Exception("Missing Daily Closing.");
                    };
                    CacheExportIncrementalAsync(exportId, 0, _configuration.ChunkExportTransactionCount, (long) _lastTransactionNr).FireAndForget();
                }
                else
                {
                    CacheExportAsync(exportId, request.ClientId, request.Erase).FireAndForget();
                }

                return new StartExportSessionResponse()
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = LastTseInfo.SerialNumberOctet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(StartExportSessionAsync));
                throw;
            }
        }

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new NotImplementedException();

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new NotImplementedException();

        private async Task CacheExportAsync(Guid exportId, string clientId = null, bool erase = false)
        {
            await _lockingHelper.PerformWithLock(_hwLock, async () =>
            {
                try
                {
                    await EnsureInitializedAsync();
                    if (erase)
                    {
                        await GetProxy().UserLoginAsync(WormUserId.WORM_USER_ADMIN, Encoding.ASCII.GetBytes(_configuration.AdminPin));
                        SetEraseEnabledForExportState(exportId, ExportState.Running);
                    }

                    using (var tempStream = File.Open(exportId.ToString(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        await GetProxy().ExportTarAsync(tempStream);
                    }
                    SetExportState(exportId, ExportState.Succeeded);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - TempFileName: {TempFileName}, ClientId: {ClientId}", nameof(CacheExportAsync), exportId.ToString(), clientId);
                    SetExportState(exportId, ExportState.Failed, ex);
                }
            });
        }

        private async Task CacheExportIncrementalAsync(Guid exportId, long startTransactionNr, long maxNumberOfRecords, long currentNumberOfTransactions)
        {
            var endTransactionNr = LastNumberOfRecords(startTransactionNr, maxNumberOfRecords, currentNumberOfTransactions);
            long newStartTransactionNr = -1;

            await _lockingHelper.PerformWithLock(_hwLock, async () =>
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        await GetProxy().ExportTarFilteredTransactionAsync(stream, (ulong) startTransactionNr, (ulong) endTransactionNr, null);
                        stream.Position = 0;
                        TarFileHelper.AppendTarStreamToTarFile(exportId.ToString(), stream);
                        _logger.LogInformation($"Export total {currentNumberOfTransactions}. Partial Export from TransactionNr: {startTransactionNr} to TransactionNr: {endTransactionNr}");
                    }
                    newStartTransactionNr = GetLastExportedTransaction(exportId.ToString()) + 1;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Equals("Filtered Export: no matching entries, export would be empty. "))
                    {
                        _logger.LogError(ex, "Failed to execute {Operation} - TempFileName: {TempFileName}", nameof(CacheExportIncrementalAsync), exportId.ToString());
                        SetExportState(exportId, ExportState.Failed, ex);
                        throw;
                    }
                    _logger.LogInformation($"No Data: Export total {currentNumberOfTransactions}. Partial Export from TransactionNr: {startTransactionNr} to TransactionNr: {endTransactionNr}");

                }
            });

            if (newStartTransactionNr == -1)
            {
                newStartTransactionNr = startTransactionNr + maxNumberOfRecords;
            }
            if (newStartTransactionNr <= currentNumberOfTransactions)
            {
                await CacheExportIncrementalAsync(exportId, newStartTransactionNr, maxNumberOfRecords, currentNumberOfTransactions).ConfigureAwait(false);
            }
            else
            {
                TarFileHelper.FinalizeTarFile(exportId.ToString());
                _logger.LogDebug("Finalized merged TAR file {fileName}.", exportId.ToString());
                SetExportState(exportId, ExportState.Succeeded);
            }
        }

        private long GetLastExportedTransaction(string targetFile)
        {
            var lastlog = TarFileHelper.GetLastLogEntryFromTarFile(targetFile);

            //Unixt_1687767355_Sig-105945_Log-Tra_No-50549_Finish_Client-6qmZrXEtrkuBhSOA7Oi39g.log
            var iSigStart = lastlog.IndexOf("Tra_No-")+7;
            var iSigEnd = lastlog.IndexOf('_', iSigStart);
            var lastSigCount = lastlog.Substring(iSigStart, iSigEnd - iSigStart);
            return long.Parse(lastSigCount);
        }

        private long LastNumberOfRecords(long previousSignatureCounter, long maxNumberOfRecords, long currentNumberOfSignatures)
        {
            if ((previousSignatureCounter + maxNumberOfRecords) > currentNumberOfSignatures)
            {
                return currentNumberOfSignatures;
            }
            return previousSignatureCounter + maxNumberOfRecords - 1;
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
                    throw new SwissbitException("The export failed to start. It needs to be retriggered");
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
                if (request.TokenId.StartsWith(NO_EXPORT))
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
                    throw new SwissbitException("The export failed to start. It needs to be retriggered");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateData) && exportStateData.State == ExportState.Failed)
                {
                    throw exportStateData.Error;
                }

                var tempFileName = request.TokenId;
                return await _lockingHelper.PerformWithLock(_hwLock, async () =>
                {
                    try
                    {
                        var sessionResponse = new EndExportSessionResponse
                        {
                            TokenId = request.TokenId
                        };
                        using (var tempStream = File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var sha256 = SHA256.Create().ComputeHash(tempStream);
                            if (tempStream.Position != exportStateData.ReadPointer)
                            {
                                throw new SwissbitException($"The fetched export doesn´t contain all data. Please call {nameof(ExportDataAsync)} to fetch all data.");
                            }
                            sessionResponse.IsValid = request.Sha256ChecksumBase64 == Convert.ToBase64String(sha256);
                        }
                        if (exportStateData.EraseEnabled)
                        {
                            if (sessionResponse.IsValid && request.Erase)
                            {
                                var status = await GetStatusAndCheckOpenTransactions();
                                if (status.StartedTransactions == 0)
                                {
                                    await GetProxy().DeleteStoredDataAsync();
                                    sessionResponse.IsErased = true;
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
                                await GetProxy().UserLogoutAsync(WormUserId.WORM_USER_ADMIN);
                            }
                            try
                            {
                                if (File.Exists(tempFileName) && !_configuration.StoreTemporaryFile)
                                {
                                    File.Delete(tempFileName);
                                }
                            }
                            catch { }
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation}", nameof(EndExportSessionAsync));
                throw;
            }
        }

        private async Task<TseStatusInformation> GetStatusAndCheckOpenTransactions()
        {
            var status = await GetProxy().GetTseStatusAsync();
            if (status.StartedTransactions > 0)
            {
                var tseOpenTransaction = await GetProxy().GetStartedTransactionsAsync(string.Empty);
                var list = string.Join(", ", tseOpenTransaction.ToArray());
                _logger.LogWarning("Could not delete log files from TSE after successfully exporting them because the following transactions were " +
                    "open: {OpenTransactions}. If these transactions are not used anymore and could not be closed automatically by a daily closing " +
                    "receipt, please consider sending a fail-transaction-receipt to cancel them.", list);
            }
            return status;
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse { Message = request.Message });

        public void Dispose() => _proxy?.Dispose();
    }
}
