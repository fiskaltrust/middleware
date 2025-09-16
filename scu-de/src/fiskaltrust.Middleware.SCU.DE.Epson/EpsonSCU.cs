using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Epson.Commands;
using fiskaltrust.Middleware.SCU.DE.Epson.Exceptions;
using fiskaltrust.Middleware.SCU.DE.Epson.Helpers;
using fiskaltrust.Middleware.SCU.DE.Epson.Helpers.ExceptionHelper;
using fiskaltrust.Middleware.SCU.DE.Epson.Models;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public class EpsonSCU : IDESSCD, IDisposable
    {
        private readonly ILogger<EpsonSCU> _logger;
        private readonly EpsonConfiguration _configuration;

        private bool _deviceInitialized;
        private string _publicKey;
        private string _tseSerialNumber;
        private string _signatureAlgorithm;
        private const string _logTimeFormat = "unixtime"; // 2020-05-29 SKE: We can use this as hardcoded value, because the time for DiboldTSE is always Unix 
        private const string _noExport = "noexport-";
        private const long _blockSize = 512;

        private readonly ConcurrentDictionary<ulong, DateTime> StartTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
        private readonly List<string> _loggedInClients = new List<string>();

        private bool disposed = false;
        private StorageInfoResult _storageInformation;
        private readonly AuthenticationCommandProvider _authenticationCommandProvider;
        private readonly TransactionCommandProvider _transactionCommandProvider;
        private readonly ConfigurationCommandProvider _configurationCommandProvider;
        private readonly OperationalCommandProvider _operationalCommandProvider;
        private readonly ExportCommandProvider _exportCommandProvider;

        public EpsonSCU(ILogger<EpsonSCU> logger, EpsonConfiguration configuration, OperationalCommandProvider operationalCommandProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _operationalCommandProvider = operationalCommandProvider;
            _exportCommandProvider = new ExportCommandProvider(_operationalCommandProvider);
            _authenticationCommandProvider = new AuthenticationCommandProvider(_operationalCommandProvider);
            _transactionCommandProvider = new TransactionCommandProvider(_operationalCommandProvider);
            _configurationCommandProvider = new ConfigurationCommandProvider(_operationalCommandProvider);
        }

        private void ThrowIfDeviceIsNotConnected()
        {
            if (_deviceInitialized && !_operationalCommandProvider.IsConnected)
            {
                _deviceInitialized = false;
                throw new EpsonException($"The TSE is not available. Unable to connect to endpoint at {_configuration.Host}:{_configuration.Port}.");
            }
        }

        private async Task InitializeConnectionAsync()
        {
            if (!_deviceInitialized)
            {
                await _operationalCommandProvider.OpenDeviceAsync();
                _deviceInitialized = true;
            }
        }

        private async Task EnsureInitializedAsync(TseStates? expectedState = null)
        {
            _storageInformation = await _operationalCommandProvider.GetStorageInformationAsync();
            while (!_storageInformation.TseInformation.HasPassedSelfTest && _storageInformation.TseInformation.TseInitializationState != GetStorageInfoResult.TseInitializationStateUninitialized)
            {
                await RunTSESelfTestAndReassignHostIfRequiredAsync();
                _storageInformation = await _operationalCommandProvider.GetStorageInformationAsync();
            }

            _publicKey = _storageInformation.TseInformation.TsePublicKey;
            _tseSerialNumber = Convert.FromBase64String(_storageInformation.TseInformation.SerialNumber).ToOctetString();
            _signatureAlgorithm = _storageInformation.TseInformation.SignatureAlgorithm;
            var tseState = GetTseState(_storageInformation.TseInformation.TseInitializationState);
            if (expectedState.HasValue && expectedState != tseState.CurrentState)
            {
                throw new EpsonException($"Expected state to be {expectedState} but instead the TSE state was {tseState}.");
            }
        }

        private async Task UpdateTimeAsync()
        {
            if (!_storageInformation.TseInformation.HasValidTime && _storageInformation.TseInformation.TseInitializationState == GetStorageInfoResult.TseInitializationStateInitialized)
            {
                await _authenticationCommandProvider.PerformAuthorizationAsync(_configuration.TimeAdminUser, _configuration.TimeAdminPin, _configuration.DefaultSharedSecret);
                await _configurationCommandProvider.UpdateTimeAsync(_configuration.TimeAdminUser, false);
                await _authenticationCommandProvider.LogOutForTimeAdmin(_configuration.TimeAdminUser);
                _loggedInClients.Clear();
            }
        }

        private async Task AuthenticateClientAsync(string clientId)
        {
            if (!_loggedInClients.Contains(clientId))
            {
                await _authenticationCommandProvider.PerformAuthorizationAsync(clientId, _configuration.TimeAdminPin, _configuration.DefaultSharedSecret);
                _loggedInClients.Add(clientId);
            }
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync(TseStates.Initialized);
                await UpdateTimeAsync();
                await AuthenticateClientAsync(request.ClientId);

                var result = await _transactionCommandProvider.StartTransactionAsync(request.ClientId, request.ProcessDataBase64, request.ProcessType);
                StartTransactionTimeStampCache.AddOrUpdate(result.TransactionNumber, result.LogTime, (key, oldValue) => result.LogTime);
                return new StartTransactionResponse
                {
                    TransactionNumber = result.TransactionNumber,
                    TimeStamp = result.LogTime,
                    ClientId = request.ClientId,
                    TseSerialNumberOctet = _tseSerialNumber,
                    SignatureData = new TseSignatureData
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _signatureAlgorithm,
                        SignatureCounter = result.SignatureCounter,
                        SignatureBase64 = result.Signature
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
                ThrowIfDeviceIsNotConnected();
                await EnsureInitializedAsync(TseStates.Initialized);
                await UpdateTimeAsync();
                await AuthenticateClientAsync(request.ClientId);

                var result = await _transactionCommandProvider.UpdateTransactionAsync((long)request.TransactionNumber, request.ClientId, request.ProcessDataBase64, request.ProcessType);
                return new UpdateTransactionResponse
                {
                    TransactionNumber = request.TransactionNumber,
                    TimeStamp = result.LogTime,
                    TseSerialNumberOctet = _tseSerialNumber,
                    ClientId = request.ClientId,
                    ProcessType = request.ProcessType,
                    ProcessDataBase64 = request.ProcessDataBase64,
                    SignatureData = new TseSignatureData
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _signatureAlgorithm,
                        SignatureCounter = result.SignatureCounter,
                        SignatureBase64 = result.Signature
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
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync(TseStates.Initialized);
                await UpdateTimeAsync();
                await AuthenticateClientAsync(request.ClientId);

                var result = await _transactionCommandProvider.FinishTransactionAsync((long)request.TransactionNumber, request.ClientId, request.ProcessDataBase64, request.ProcessType);
                StartTransactionTimeStampCache.TryRemove(request.TransactionNumber, out var startTransactionTimeStamp);
                return new FinishTransactionResponse
                {
                    TransactionNumber = request.TransactionNumber,
                    StartTransactionTimeStamp = startTransactionTimeStamp,
                    TimeStamp = result.LogTime,
                    TseTimeStampFormat = _logTimeFormat,
                    TseSerialNumberOctet = _tseSerialNumber,
                    ClientId = request.ClientId,
                    ProcessType = request.ProcessType,
                    ProcessDataBase64 = request.ProcessDataBase64,
                    SignatureData = new TseSignatureData
                    {
                        PublicKeyBase64 = _publicKey,
                        SignatureAlgorithm = _signatureAlgorithm,
                        SignatureCounter = (ulong)result.SignatureCounter,
                        SignatureBase64 = result.Signature
                    },
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync();

                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = _noExport + Guid.NewGuid().ToString(),
                        TseSerialNumberOctet = _tseSerialNumber
                    };
                }

                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);

                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                var archiveExportResult = await _exportCommandProvider.ArchiveExportAsync(request.ClientId);
                CacheExportAsync(tokenId).FireAndForget();
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

        private async Task CacheExportAsync(Guid tokenId)
        {
            try
            {
                using (var tempStream = File.Open(tokenId.ToString(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    do
                    {
                        var exportStateInformation = await _exportCommandProvider.GetExportDataAsync();
                        var result = Convert.FromBase64String(exportStateInformation.ExportData);
                        await tempStream.WriteAsync(result, 0, result.Length);
                        if (exportStateInformation.ExportStatus == "EXPORT_COMPLETE")
                        {
                            break;
                        }
                    } while (true);
                }
                SetExportState(tokenId, ExportState.Succeeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - ExportId: {ExportId}", nameof(CacheExportAsync), tokenId);
                SetExportState(tokenId, ExportState.Failed, ex);
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync();

                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);

                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                var archiveExportResult = await _exportCommandProvider.ExportFilteredByPeriodOfTimeAsync(request.From, request.To, request.ClientId);
                CacheExportAsync(tokenId).FireAndForget();
                return new StartExportSessionResponse
                {
                    TokenId = tokenId.ToString(),
                    TseSerialNumberOctet = _tseSerialNumber
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
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync();

                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);

                var tokenId = Guid.NewGuid();
                SetExportState(tokenId, ExportState.Running);
                var archiveExportResult = await _exportCommandProvider.ExportFilteredByTransactionNumberIntervalAsync(request.From, request.To, request.ClientId);
                CacheExportAsync(tokenId).FireAndForget();
                return new StartExportSessionResponse
                {
                    TokenId = tokenId.ToString(),
                    TseSerialNumberOctet = _tseSerialNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionByTransactionAsync), JsonConvert.SerializeObject(request));
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

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
        {
            try
            {
                if (request.TokenId.StartsWith(_noExport))
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
                    throw new EpsonException("The export failed to start. It needs to be retriggered");
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
                            chunkSize = (int)tempStream.Length - exportStateData.ReadPointer;
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
                if (request.TokenId.StartsWith(_noExport))
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
                    return await Task.Run(async () =>
                    {
                        using (var tempStream = File.OpenRead(tempFileName))
                        {
                            var sha256 = SHA256.Create().ComputeHash(tempStream);
                            if (_readStreamPointer[request.TokenId].ReadPointer == tempStream.Position && request.Sha256ChecksumBase64 == Convert.ToBase64String(sha256))
                            {
                                sessionResponse.IsValid = true;
                                if (request.Erase)
                                {
                                    try
                                    {
                                        await _exportCommandProvider.FinalizeExportAsync(true);
                                        sessionResponse.IsErased = true;
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.LogError(e, "Failed to delete export data from tse.");
                                    }
                                }
                                else
                                {
                                    await _exportCommandProvider.FinalizeExportAsync(false);
                                }
                                return sessionResponse;
                            }
                        }
                        return sessionResponse;
                    });
                }
                finally
                {
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

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync();

                var storageInfo = await _operationalCommandProvider.GetStorageInformationAsync();

                var registeredClientIds = new List<string>();
                var certificates = new List<string>();
                var startedTransactions = new List<ulong>();
                X509Certificate2 cert = null;
                if (storageInfo.TseInformation.TseInitializationState == GetStorageInfoResult.TseInitializationStateInitialized)
                {
                    await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                    registeredClientIds = (await _configurationCommandProvider.GetRegisteredClientsAsnc()).RegisteredClientIdList;
                    await _authenticationCommandProvider.LogOutForAdmin();
                    var logMessageCertificate = await _exportCommandProvider.GetLogMessageCertificateAsync();
                    certificates.Add(logMessageCertificate.LogMessageCertificateBase64);
                    startedTransactions = (await _transactionCommandProvider.GetStartedTransactionListAsync()).StartedTransactionNumberList;
                    try
                    {
                        cert = new X509Certificate2(Convert.FromBase64String(logMessageCertificate.LogMessageCertificateBase64));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse certificate NotAfter date");
                    }
                }

                return new TseInfo
                {
                    CurrentNumberOfClients = storageInfo.TseInformation.RegisteredClients,
                    MaxNumberOfClients = storageInfo.TseInformation.MaxRegisteredClients,
                    MaxNumberOfStartedTransactions = storageInfo.TseInformation.MaxStartedTransactions,
                    CurrentNumberOfStartedTransactions = storageInfo.TseInformation.StartedTransactions,
                    SerialNumberOctet = _tseSerialNumber,
                    CurrentState = GetTseState(storageInfo.TseInformation.TseInitializationState).CurrentState,
                    MaxNumberOfSignatures = storageInfo.TseInformation.MaxSignatures,
                    PublicKeyBase64 = _publicKey,
                    SignatureAlgorithm = _signatureAlgorithm,
                    LogTimeFormat = _logTimeFormat,
                    MaxLogMemorySize = (long)storageInfo.TseInformation.TseCapacity * _blockSize,
                    CurrentNumberOfSignatures = storageInfo.TseInformation.CreatedSignatures,
                    CurrentLogMemorySize = (long)storageInfo.TseInformation.TseCurrentSize * _blockSize,
                    FirmwareIdentification = BitConverter.GetBytes(storageInfo.TseInformation.SoftwareVersion).ToOctetString(),
                    CurrentClientIds = registeredClientIds,
                    CertificationIdentification = storageInfo.TseInformation.TseDescription,
                    CertificatesBase64 = certificates,
                    CurrentStartedTransactionNumbers = startedTransactions,
                    Info = cert != null ? new Dictionary<string, object>() { { "TseExpirationDate", cert.NotAfter.ToString("yyyy-MM-dd") } } : null
                };
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
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync();

                var storageInfo = await _operationalCommandProvider.GetStorageInformationAsync();
                switch (request.CurrentState)
                {
                    case TseStates.Uninitialized:
                        break;
                    case TseStates.Initialized:
                        if (storageInfo.TseInformation.TseInitializationState != GetStorageInfoResult.TseInitializationStateInitialized)
                        {
                            await _configurationCommandProvider.SetUpAsync(_configuration.Puk, _configuration.AdminPin, _configuration.TimeAdminPin);

                            await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                            await RunTSESelfTestAndReassignHostIfRequiredAsync();
                            await _configurationCommandProvider.UpdateTimeAsync(_configuration.AdminUser, false);
                            await _authenticationCommandProvider.LogOutForAdmin();
                        }
                        break;
                    case TseStates.Terminated:
                        await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                        await _configurationCommandProvider.DisableAsync();
                        await _authenticationCommandProvider.LogOutForAdmin();
                        break;
                    default:
                        break;
                }

                storageInfo = await _operationalCommandProvider.GetStorageInformationAsync();
                return GetTseState(storageInfo.TseInformation.TseInitializationState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private async Task RunTSESelfTestAndReassignHostIfRequiredAsync()
        {
            try
            {
                await _configurationCommandProvider.RunTSESelfTestAsync();
            }
            catch (EpsonException ex) when (ex.Message == "TSE1_ERROR_CLIENT_NOT_REGISTERED")
            {
                _logger.LogInformation("It seems like this TSE was previously used with another host and needs to be rebound to the current machine.");
                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                await _configurationCommandProvider.SetUpForPrinterAsync();
                await _authenticationCommandProvider.LogOutForAdmin();
                _logger.LogInformation("Successfully bound the TSE to the current host.");

                await _configurationCommandProvider.RunTSESelfTestAsync();
            }
        }

        private TseState GetTseState(string state)
        {
            return state switch
            {
                GetStorageInfoResult.TseInitializationStateInitialized => new TseState { CurrentState = TseStates.Initialized },
                GetStorageInfoResult.TseInitializationStateDecommissioned => new TseState { CurrentState = TseStates.Terminated },
                _ => new TseState { CurrentState = TseStates.Uninitialized },
            };
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse
        {
            Message = request.Message
        });

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync(TseStates.Initialized);

                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                await _configurationCommandProvider.RegisterClientAsync(request.ClientId);
                var registeredClientIds = (await _configurationCommandProvider.GetRegisteredClientsAsnc()).RegisteredClientIdList;
                await _authenticationCommandProvider.LogOutForAdmin();

                return new RegisterClientIdResponse
                {
                    ClientIds = registeredClientIds
                };
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
                ThrowIfDeviceIsNotConnected();
                await InitializeConnectionAsync();
                await EnsureInitializedAsync(TseStates.Initialized);

                await _authenticationCommandProvider.PerformAuthorizationForAdminAsync(_configuration.AdminUser, _configuration.AdminPin, _configuration.DefaultSharedSecret);
                await _configurationCommandProvider.DeregisterClientAsync(request.ClientId);
                var registeredClientIds = (await _configurationCommandProvider.GetRegisteredClientsAsnc()).RegisteredClientIdList;
                await _authenticationCommandProvider.LogOutForAdmin();

                return new UnregisterClientIdResponse
                {
                    ClientIds = registeredClientIds
                };
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
                await EnsureInitializedAsync(TseStates.Initialized);
                await _authenticationCommandProvider.PerformAuthorizationAsync(_configuration.TimeAdminUser, _configuration.TimeAdminPin, _configuration.DefaultSharedSecret);
                await _configurationCommandProvider.UpdateTimeForFirstAsync(_configuration.TimeAdminUser, DateTime.UtcNow, false);
                await _authenticationCommandProvider.LogOutForTimeAdmin(_configuration.TimeAdminUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {operation}", nameof(ExecuteSetTseTimeAsync));
                throw;
            }
        }

        public async Task ExecuteSelfTestAsync()
        {
            try
            {
                ThrowIfDeviceIsNotConnected();
                await RunTSESelfTestAndReassignHostIfRequiredAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {operation}", nameof(ExecuteSelfTestAsync));
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _operationalCommandProvider.CloseDeviceAsync().Wait();
                }
            }
            disposed = true;
        }
    }
}
