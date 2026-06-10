using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal
{
    public class DeutscheFiskalSCU : IDESSCD, IDisposable
    {
        private const string EMPTY_PROCESSDATA = "PG5vbmU+";
        private const string NO_EXPORT_PREFIX = "noexport-";
        private const string FCC_SUBDIR = "fiskaltrust/FCC";

        private readonly DateTime _minExportDateTime = new DateTime(2020, 1, 1);

        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer;
        private readonly ConcurrentDictionary<ulong, DateTime> _startTransactionTimeStampCache;
        private readonly ILogger<DeutscheFiskalSCU> _logger;
        private readonly DeutscheFiskalSCUConfiguration _configuration;
        private readonly IFccInitializationService _fccInitializationService;
        private readonly IFccProcessHost _fccProcessHost;
        private readonly IFccDownloadService _fccDownloadService;
        private readonly FccErsApiProvider _fccErsApiProvider;
        private readonly FccAdminApiProvider _fccAdminApiProvider;
        private string _fccDirectory;
        private Version _version;
        private readonly Task _fccInitializingTask;
        private TseInfo _lastTseInfo;

        public DeutscheFiskalSCU(ILogger<DeutscheFiskalSCU> logger, DeutscheFiskalSCUConfiguration configuration, IFccInitializationService fccInitializationService,
            IFccProcessHost fccProcessHost, IFccDownloadService fccDownloadService, FccErsApiProvider fccErsApiProvider, FccAdminApiProvider fccAdminApiProvider)
        {
            _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
            _startTransactionTimeStampCache = new ConcurrentDictionary<ulong, DateTime>();
            _logger = logger;
            _configuration = configuration;
            _fccInitializationService = fccInitializationService;
            _fccProcessHost = fccProcessHost;
            _fccDownloadService = fccDownloadService;
            _fccErsApiProvider = fccErsApiProvider;
            _fccAdminApiProvider = fccAdminApiProvider;
            _fccDirectory = _configuration.FccDirectory;

            if (string.IsNullOrEmpty(_configuration.FccUri))
            {
                _fccInitializingTask = StartLocalFCC();
            }
        }

        private async Task StartLocalFCC()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration.FccDirectory))
                {
                    _fccDirectory = GetDefaultFccDirectory();
                }

                if (!_fccDownloadService.IsInstalled(_fccDirectory))
                {
                    if (!_fccDownloadService.IsDownloaded(_fccDirectory))
                    {
                        await _fccDownloadService.DownloadFccAsync(_fccDirectory, null);
                    }
                    _fccInitializationService.Initialize(_fccDirectory);
                    _version = new Version(_configuration.FccVersion);

                } else if (new Version(_configuration.FccVersion) < new Version(4, 4, 0))
                {
                    throw new Exception($"The configured FCC version '{_configuration.FccVersion}' is not supported. Please use FCC version 4.4.0 or higher.");
                }
                else if (!_fccDownloadService.IsLatestVersion(_fccDirectory, new Version(_configuration.FccVersion)))
                {
                    var currentlyInstalledVersion = _fccDownloadService.UsedFCCVersion;
                    if (_fccDownloadService.DownloadFccAsync(_fccDirectory, currentlyInstalledVersion).Result)
                    {
                        _fccInitializationService.Update(_fccDirectory);
                    }
                }
                else if (!_fccDownloadService.IsLatestVersionDat(_fccDirectory, new Version(_configuration.FccVersion)))
                {
                     _fccInitializationService.Update(_fccDirectory);
                }
                else if (!_fccInitializationService.IsInitialized(_fccDirectory))
                {
                    _fccInitializationService.Initialize(_fccDirectory);
                }
                ConfigHelper.SetConfiguration(_configuration, _fccDirectory);

                if (_version == null)
                {
                    _version = _fccDownloadService.UsedFCCVersion;
                }
                StartFccIfNotRunning().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured while initializing the FCC. Please see the logs above for more details.");
            }
        }

        private string GetDefaultFccDirectory()
        {
            // Check if previously used default FCC directory exists to not break existing configurations
            var previousPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FCC_SUBDIR, _configuration.FccId);
            if (Directory.Exists(previousPath))
            {
                return previousPath;
            }

            return Path.Combine(_configuration.ServiceFolder, FCC_SUBDIR, _configuration.FccId);
        }

        private async Task StartFccIfNotRunning()
        {
            if (!_fccProcessHost.IsExtern && !_fccProcessHost.IsRunning)
            {
                await _fccProcessHost.StartAsync(_fccDirectory).ConfigureAwait(false);
            }
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                await StartFccIfNotRunning();
                if (_lastTseInfo == null)
                {
                    await GetTseInfoAsync();
                }

                var txId = Guid.NewGuid();
                var content = CreateStartTransactionRequest(request.ClientId, request.ProcessType, request.ProcessDataBase64);
                var transaction = await _fccErsApiProvider.StartTransactionRequestAsync(content);

                _startTransactionTimeStampCache.AddOrUpdate(transaction.TransactionNumber, transaction.LogTime, (key, oldValue) => transaction.LogTime);

                return CreateStartTransactionResponse(request.ClientId, transaction);
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(StartTransactionAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation}. Request: {Request}", nameof(StartTransactionAsync), JsonConvert.SerializeObject(request));
                }

                throw;
            }
        }

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request)
        {
            try
            {
                await StartFccIfNotRunning();
                if (_lastTseInfo == null)
                {
                    await GetTseInfoAsync();
                }

                var content = CreateUpdateTransactionRequest(request.ClientId, request.ProcessType, request.ProcessDataBase64);
                var transaction = await _fccErsApiProvider.UpdateTransactionRequestAsync(request.TransactionNumber, content);
                return CreateUpdateTransactionResponse(request.ClientId, request.TransactionNumber, request.ProcessDataBase64, request.ProcessType, transaction);
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(UpdateTransactionAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(UpdateTransactionAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request)
        {
            try
            {
                await StartFccIfNotRunning();
                if (_lastTseInfo == null)
                {
                    await GetTseInfoAsync();
                }

                var content = CreateFinishTransactionRequest(request.ClientId, request.ProcessType, request.ProcessDataBase64);
                var transaction = await _fccErsApiProvider.FinishTransactionRequestAsync(request.TransactionNumber, content);

                if (!_startTransactionTimeStampCache.TryRemove(request.TransactionNumber, out var startTransactionTimeStamp))
                {
                    var logs = await GetLogsForTransaction(request.TransactionNumber);
                    var startTransactionLog = logs.Single(x => x.OperationType.Contains(DeutscheFiskalConstants.TransactionType.StartTransaction));
                    startTransactionTimeStamp = startTransactionLog.LogTime;
                }

                return CreateFinishTransactionResponse(request.ClientId, request.TransactionNumber, request.ProcessDataBase64, request.ProcessType, transaction, startTransactionTimeStamp);
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(FinishTransactionAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                if (!_fccInitializingTask.IsCompleted)
                {
                    await _fccInitializingTask;
                }
                await StartFccIfNotRunning();

                var clients = await _fccAdminApiProvider.GetClientsAsync();
                var startedTransactions = await _fccErsApiProvider.GetStartedTransactionsAsync(clients.Select(x => x.ClientId));
                var fccInfo = await _fccAdminApiProvider.GetFccInfoAsync();
                var tssDetails = await _fccAdminApiProvider.GetTssDetailsAsync();
                var selfCheckResult = await _fccAdminApiProvider.GetSelfCheckResultAsync();

                // TODO check how many items are returned by selfCheckResult.KeyInfos and how they behave
                var activeKey = selfCheckResult.keyInfos.FirstOrDefault(x => x.state == KeyState.Active) ?? selfCheckResult.keyInfos.First();

                // The TSE's <public-key> field on the QR-code MUST be the raw SEC1 uncompressed
                // EC point ( 0x04 || X || Y ), because that is what every BSI verifier app
                // (Amadeus Verify, Fiskalycheck, ...) expects and what the <signatur-zaehler>
                // signature is verified against.
                //
                // The FCC exposes the key in two places, both of which can be the DER-encoded
                // SubjectPublicKeyInfo envelope instead of the raw point:
                //   * tssDetails.PublicKey      -> usually SPKI ("MHYwEAYHKoZIzj0CAQYFK4EEACID...")
                //   * tssDetails.LeafCertificate.GetPublicKey() -> raw SEC1 on most runtimes,
                //                                                  but BIT-STRING padding bugs
                //                                                  have been observed.
                //
                // Normalize to the raw point regardless of source
                //
                var certificatePublicKeyBase64 = ExtractRawEcPublicKeyBase64(tssDetails);

                _lastTseInfo = new TseInfo
                {
                    CurrentNumberOfClients = clients.Count,
                    CurrentNumberOfStartedTransactions = fccInfo.CurrentNumberOfTransactions,
                    SerialNumberOctet = tssDetails.SerialNumberHex,
                    PublicKeyBase64 = certificatePublicKeyBase64,
                    FirmwareIdentification = JsonConvert.SerializeObject(new Dictionary<string, string> { { "fccVersion", selfCheckResult.fccVersion }, { "localClientVersion", selfCheckResult.localClientVersion }, { "remoteCspVersion", selfCheckResult.remoteCspVersion } }),
                    CertificationIdentification = GetCertificationIdentification(),
                    MaxNumberOfClients = fccInfo.MaxNumberClients,
                    MaxNumberOfStartedTransactions = fccInfo.MaxNumberTransactions,
                    CertificatesBase64 = NormalizeCertificate(tssDetails.LeafCertificate),
                    CurrentClientIds = clients.Select(x => x.ClientId),
                    SignatureAlgorithm = tssDetails.Algorithm,
                    CurrentLogMemorySize = -1,
                    CurrentNumberOfSignatures = activeKey.lastSignatureCounter,
                    LogTimeFormat = tssDetails.TimeFormat,
                    MaxLogMemorySize = long.MaxValue,
                    MaxNumberOfSignatures = long.MaxValue,
                    CurrentStartedTransactionNumbers = startedTransactions.Select(x => (ulong) x.TransactionNumber).ToList(),
                    CurrentState = activeKey.state.ToTseState(),
                };

                if(_configuration.EnableFccMetrics)
                {
                    var metrics = await _fccProcessHost.QueryMetrics();
                    _logger.LogDebug($"FCC Metrics: {JToken.Parse(metrics).ToString(Formatting.Indented)}");
                }
                return _lastTseInfo;
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}",
                        nameof(GetTseInfoAsync), fex.Operation, fex.ErrorCode);
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation}", nameof(GetTseInfoAsync));
                }
                throw;
            }
        }

        private string GetCertificationIdentification()
        {
            if (_configuration.DisplayCertificationIdAddition && !string.IsNullOrEmpty(_configuration.CertificationIdAddition))
            {
                return $"{_configuration.CertificationId} [{_configuration.CertificationIdAddition}]";
            }

            return _configuration.CertificationId;
        }

        private List<string> NormalizeCertificate(string certificate)
        {
            if (string.IsNullOrWhiteSpace(certificate))
            {
                return new List<string>();
            }

            // If the FCC already returns a clean Base64 DER (no PEM markers), pass it through.
            if (certificate.IndexOf("-----BEGIN CERTIFICATE-----", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new List<string> { certificate.Trim() };
            }

            try
            {
                var certificates = new List<string>();
                using (var stringReader = new StringReader(certificate))
                {
                    var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(stringReader);

                    object pemObject;
                    while ((pemObject = pemReader.ReadObject()) != null)
                    {
                        if (pemObject is Org.BouncyCastle.X509.X509Certificate bcCert)
                        {
                            certificates.Add(Convert.ToBase64String(bcCert.GetEncoded()));
                        }
                        else if (pemObject is Org.BouncyCastle.Asn1.X509.X509CertificateStructure certStructure)
                        {
                            certificates.Add(Convert.ToBase64String(certStructure.GetEncoded()));
                        }
                    }
                }

                return certificates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse certificate using BouncyCastle. Falling back to manual parsing.");
                return NormalizeCertificateManual(certificate);
            }
        }

        private static List<string> NormalizeCertificateManual(string certificate)
        {
            const string pemHeader = "-----BEGIN CERTIFICATE-----";
            const string pemFooter = "-----END CERTIFICATE-----";
            var certificates = new List<string>();

            var startIndex = 0;
            while (true)
            {
                var headerIndex = certificate.IndexOf(pemHeader, startIndex, StringComparison.OrdinalIgnoreCase);
                if (headerIndex < 0)
                {
                    break;
                }

                var footerIndex = certificate.IndexOf(pemFooter, headerIndex, StringComparison.OrdinalIgnoreCase);
                if (footerIndex < 0)
                {
                    break;
                }

                var bodyStart = headerIndex + pemHeader.Length;
                var body = certificate.Substring(bodyStart, footerIndex - bodyStart);
                var base64 = body.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty).Trim();
                certificates.Add(base64);

                startIndex = footerIndex + pemFooter.Length;
            }

            return certificates;
        }

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                await StartFccIfNotRunning();
                var preexistingClients = await _fccAdminApiProvider.GetClientsAsync();

                if (preexistingClients.Any(x => x.ClientId == request.ClientId))
                {
                    _logger.LogWarning("The given client already exists and was therefore not registered.");
                    return new RegisterClientIdResponse
                    {
                        ClientIds = preexistingClients.Select(x => x.ClientId)
                    };
                }

                await _fccAdminApiProvider.CreateClientAsync(request.ClientId);
                return new RegisterClientIdResponse
                {
                    ClientIds = (await _fccAdminApiProvider.GetClientsAsync()).Select(x => x.ClientId)
                };
            }
            catch (FiskalCloudException ex) when (ex.ErrorType == DeutscheFiskalConstants.ErrorTypes.InvalidRegistrationKey)
            {
                _logger.LogError($"The client '{request.ClientId}' could not be registered. If you obtained this TSE via fiskaltrust's shop as a single product, registering more than one client is not permitted. Please refer to our product documentation for more details.");
                throw;
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}",
                        nameof(RegisterClientIdAsync), fex.Operation, fex.ErrorCode);
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(RegisterClientIdAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request)
        {
            try
            {
                await StartFccIfNotRunning();
                var existingClients = await _fccAdminApiProvider.GetClientsAsync();

                if (existingClients.Any(x => x.ClientId == request.ClientId))
                {
                    await _fccAdminApiProvider.DeregisterClientAsync(request.ClientId);
                }

                return new UnregisterClientIdResponse
                {
                    ClientIds = (await _fccAdminApiProvider.GetClientsAsync()).Select(x => x.ClientId)
                };
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}",
                        nameof(UnregisterClientIdAsync), fex.Operation, fex.ErrorCode);
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(UnregisterClientIdAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public async Task<TseState> SetTseStateAsync(TseState request)
        {
            try
            {
                if (_lastTseInfo == null)
                {
                    await GetTseInfoAsync();
                }

                return new TseState
                {
                    CurrentState = _lastTseInfo.CurrentState
                };
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(SetTseStateAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public Task ExecuteSetTseTimeAsync() => Task.CompletedTask;

        public Task ExecuteSelfTestAsync() => Task.CompletedTask;

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var tssDetails = await _fccAdminApiProvider.GetTssDetailsAsync();
                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = NO_EXPORT_PREFIX + Guid.NewGuid().ToString(),
                        TseSerialNumberOctet = tssDetails.SerialNumberHex
                    };
                }

                var exportId = Guid.NewGuid();

                SetExportState(exportId, ExportState.Running);
                WriteExportDetails(exportId, endTime, request.ClientId);
                CacheExportAsync(exportId, GetExportStartDateTimeSync(), endTime, request.ClientId).ExecuteInBackgroundThread();

                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = tssDetails.SerialNumberHex
                };
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(StartExportSessionAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        private DateTime GetExportStartDateTimeSync()
        {
            try
            {
                var exportDetail = JsonConvert.DeserializeObject<ExportDetails>(File.ReadAllText(Path.Combine(_fccDirectory, ".fccdata", "_lastexport.log")));
                return exportDetail?.EndDate ?? _minExportDateTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read last export date. Defaulting to minimum export date.");
                return _minExportDateTime;
            }
        }

        private async Task CacheExportAsync(Guid exportId, DateTime startDate, DateTime endDate, string clientId)
        {
            try
            {
                var filePath = GetTempPath(exportId.ToString());
                await _fccAdminApiProvider.RequestExportAsync(exportId, filePath, startDate, endDate, clientId);
                SetExportState(exportId, ExportState.Succeeded);
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, ExportId: '{ExportId}'",
                        nameof(StartExportSessionAsync), fex.Operation, fex.ErrorCode, exportId);
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - ExportId: {ExportId}", nameof(CacheExportAsync), exportId);
                }
                SetExportState(exportId, ExportState.Failed, ex);
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

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new NotImplementedException();

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new NotImplementedException();

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
        {
            if (request.TokenId.StartsWith(NO_EXPORT_PREFIX))
            {
                return new ExportDataResponse
                {
                    TokenId = request.TokenId,
                    TotalTarFileSizeAvailable = true,
                    TotalTarFileSize = 0,
                    TarFileEndOfFile = true
                };
            }
            try
            {
                if (!_readStreamPointer.ContainsKey(request.TokenId))
                {
                    throw new FiskalCloudException("The export failed to start. It needs to be retriggered.");
                }

                var filePath = GetTempPath(request.TokenId);

                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateData) && exportStateData.State == ExportState.Failed)
                {
                    throw exportStateData.Error;
                }

                if (exportStateData.State != ExportState.Succeeded || !File.Exists(filePath))
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
                    using (var tempStream = File.OpenRead(filePath))
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
                exportDataResponse.TotalTarFileSize = new FileInfo(filePath).Length;
                exportDataResponse.TotalTarFileSizeAvailable = exportDataResponse.TotalTarFileSize >= 0;
                exportDataResponse.TarFileEndOfFile = exportStateData.ReadPointer == exportDataResponse.TotalTarFileSize;
                return exportDataResponse;
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(ExportDataAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(ExportDataAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
        }

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            if (request.TokenId.StartsWith(NO_EXPORT_PREFIX))
            {
                return new EndExportSessionResponse
                {
                    TokenId = request.TokenId,
                    IsErased = false,
                    IsValid = true
                };
            }
            var filePath = GetTempPath(request.TokenId);

            try
            {
                var sessionResponse = new EndExportSessionResponse
                {
                    TokenId = request.TokenId
                };
                return await Task.Run(async () =>
                {
                    using (var tempStream = File.OpenRead(filePath))
                    {
                        var sha256 = SHA256.Create().ComputeHash(tempStream);
                        if (_readStreamPointer[request.TokenId].ReadPointer == tempStream.Position && request.Sha256ChecksumBase64 == Convert.ToBase64String(sha256))
                        {
                            sessionResponse.IsValid = true;

                            if (request.Erase)
                            {
                                try
                                {
                                    var exportDetails = GetExportDetails(request.TokenId);
                                    // Necessary because of how the DF handles acknowledging transactions.
                                    // It seems like it's required to go at least one minute back to not return a HTTP 500
                                    var endDate = exportDetails.EndDate.AddMinutes(-1);
                                    if (_fccAdminApiProvider.IsSplitExport(Guid.Parse(request.TokenId)))
                                    {
                                        await _fccAdminApiProvider.AcknowledgeSplitTransactionsAsync(Guid.Parse(request.TokenId), exportDetails.ClientId).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await _fccAdminApiProvider.AcknowledgeAllTransactionsAsync(_minExportDateTime, endDate, exportDetails.ClientId);
                                    }

                                    File.WriteAllText(Path.Combine(_fccDirectory, ".fccdata", "_lastexport.log"), JsonConvert.SerializeObject(new ExportDetails {EndDate = endDate }));
                                    sessionResponse.IsErased = true;
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, "Failed to delete export data from tse.");
                                }
                            }
                            return sessionResponse;
                        }
                    }
                    return sessionResponse;
                });
            }
            catch (Exception ex)
            {
                if (ex is FiskalCloudException fex)
                {
                    _logger.LogError(ex, "An error occured while communicating with the FCC during executing '{Operation}'. HTTP operation: '{FccOperation}', HTTP status code: {FccStatusCode}, Request: '{Request}'",
                        nameof(EndExportSessionAsync), fex.Operation, fex.ErrorCode, JsonConvert.SerializeObject(request));
                }
                else
                {
                    _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(EndExportSessionAsync), JsonConvert.SerializeObject(request));
                }
                throw;
            }
            finally
            {
                _fccAdminApiProvider.RemoveSplitExportIfExists(request.TokenId);
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete file {filePath} after succesfull export.");
                }
            }
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse
        {
            Message = request.Message
        });

        private async Task<List<TransactionLogMessage>> GetLogsForTransaction(ulong transactionNumber)
        {
            var log = await _fccAdminApiProvider.ExportSingleTransactionAsync(transactionNumber);

            using var stream = new MemoryStream(log);
            return LogParser.GetLogsFromTarStream(stream).OfType<TransactionLogMessage>().ToList();
        }

        private void WriteExportDetails(Guid exportId, DateTime endDate, string clientId)
        {
            var path = GetTempPath(exportId.ToString()) + ".details";
            File.WriteAllText(path, JsonConvert.SerializeObject(new ExportDetails { ClientId = clientId, EndDate = endDate }));
        }

        private ExportDetails GetExportDetails(string exportId)
        {
            return JsonConvert.DeserializeObject<ExportDetails>(File.ReadAllText(GetTempPath(exportId.ToString()) + ".details"));
        }

        private string GetTempPath(string exportId) => Path.Combine(Path.GetTempPath(), exportId);

        private StartTransactionRequestDto CreateStartTransactionRequest(string clientId, string processType, string processDataBase64)
        {
            return new StartTransactionRequestDto
            {
                ClientId = clientId,
                ExternalTransactionId = Guid.NewGuid(),
                ProcessType = processType,
                ProcessData = string.IsNullOrEmpty(processDataBase64) ? EMPTY_PROCESSDATA : processDataBase64
            };
        }

        private UpdateTransactionRequestDto CreateUpdateTransactionRequest(string clientId, string processType, string processDataBase64)
        {
            return new UpdateTransactionRequestDto
            {
                ClientId = clientId,
                ProcessType = processType,
                ProcessData = string.IsNullOrEmpty(processDataBase64) ? EMPTY_PROCESSDATA : processDataBase64
            };
        }

        private FinishTransactionRequestDto CreateFinishTransactionRequest(string clientId, string processType, string processDataBase64)
        {
            return new FinishTransactionRequestDto
            {
                ClientId = clientId,
                ProcessType = processType,
                ProcessData = string.IsNullOrEmpty(processDataBase64) ? EMPTY_PROCESSDATA : processDataBase64
            };
        }

        private StartTransactionResponse CreateStartTransactionResponse(string clientId, StartTransactionResponseDto transaction)
        {
            return new StartTransactionResponse
            {
                TransactionNumber = transaction.TransactionNumber,
                TseSerialNumberOctet = transaction.SerialNumber,
                // SwissbitCloudV2 pattern: pure pass-through of the queue clientId. The TSE
                // signs the LogMessage with whatever clientID it was handed at StartTransaction,
                // so the QR's <kassen-seriennummer> must carry the same value or the
                // <signatur> stops verifying against <public-key>.
                ClientId = clientId,
                TimeStamp = transaction.LogTime,
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transaction.SignatureValue,
                    SignatureCounter = transaction.SignatureCounter,
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64,
                }
            };
        }

        private UpdateTransactionResponse CreateUpdateTransactionResponse(string clientId, ulong transactionNumber, string processData, string processType, UpdateTransactionResponseDto transaction)
        {
            return new UpdateTransactionResponse
            {
                TransactionNumber = transactionNumber,
                TseSerialNumberOctet = _lastTseInfo.SerialNumberOctet,
                // See CreateStartTransactionResponse: pure pass-through of the queue clientId.
                ClientId = clientId,
                ProcessDataBase64 = processData,
                ProcessType = processType,
                TimeStamp = transaction.LogTime ?? default,
                SignatureData = new TseSignatureData
                {
                    SignatureBase64 = transaction.SignatureValue,
                    SignatureCounter = transaction.SignatureCounter ?? 0,
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64
                }
            };
        }

        private FinishTransactionResponse CreateFinishTransactionResponse(string clientId, ulong transactionNumber, string processData, string processType, FinishTransactionResponseDto transaction, DateTime startTransactionLogTime)
        {
            return new FinishTransactionResponse
            {
                TransactionNumber = transactionNumber,
                TseSerialNumberOctet = _lastTseInfo.SerialNumberOctet,
                // See CreateStartTransactionResponse: pure pass-through of the queue clientId.
                ClientId = clientId,
                ProcessDataBase64 = processData,
                ProcessType = processType,
                StartTransactionTimeStamp = startTransactionLogTime,
                SignatureData = new TseSignatureData
                {
                    SignatureBase64 = transaction.SignatureValue,
                    SignatureCounter = transaction.SignatureCounter,
                    SignatureAlgorithm = _lastTseInfo.SignatureAlgorithm,
                    PublicKeyBase64 = _lastTseInfo.PublicKeyBase64
                },
                TseTimeStampFormat = _lastTseInfo.LogTimeFormat,
                TimeStamp = transaction.LogTime
            };
        }

        /// <summary>
        /// Returns the TSE's elliptic-curve public key as a Base64-encoded raw SEC1
        /// uncompressed point (0x04 || X || Y), which is the form required by the
        /// QR-code &lt;public-key&gt; field according to BSI TR-03151 and accepted by
        /// fiscal verifier apps.
        ///
        /// The FCC returns the key either as
        ///   * a raw SEC1 point already (NIST P-256 = 65 bytes, P-384 = 97 bytes, P-521 = 133 bytes), or
        ///   * a DER-encoded SubjectPublicKeyInfo envelope, which must be unwrapped.
        /// We fall back to the leaf certificate's public key when tssDetails.PublicKey
        /// is missing.
        /// </summary>
        internal static string ExtractRawEcPublicKeyBase64(TssDetailsResponseDto tssDetails)
        {
            byte[] candidate = null;
            if (!string.IsNullOrEmpty(tssDetails?.PublicKey))
            {
                candidate = Convert.FromBase64String(tssDetails.PublicKey);
            }
            else if (!string.IsNullOrEmpty(tssDetails?.LeafCertificate))
            {
                using var cert = new X509Certificate2(Convert.FromBase64String(tssDetails.LeafCertificate));
                candidate = cert.GetPublicKey();
            }

            if (candidate == null || candidate.Length == 0)
            {
                return null;
            }

            return Convert.ToBase64String(NormalizeToSec1UncompressedPoint(candidate));
        }

        /// <summary>
        /// Strips a DER-encoded SubjectPublicKeyInfo wrapper if present and returns the
        /// raw SEC1 uncompressed EC point (0x04 || X || Y). If the input is already a
        /// raw point it is returned unchanged.
        /// </summary>
        internal static byte[] NormalizeToSec1UncompressedPoint(byte[] bytes)
        {
            // Already a raw SEC1 uncompressed point.
            if (bytes.Length > 0 && bytes[0] == 0x04 &&
                (bytes.Length == 65 || bytes.Length == 97 || bytes.Length == 133))
            {
                return bytes;
            }

            // DER SubjectPublicKeyInfo: try the framework first.
#if NET6_0_OR_GREATER
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(bytes, out _);
                var p = ecdsa.ExportParameters(false);
                var coordSize = p.Q.X.Length;
                var raw = new byte[1 + (2 * coordSize)];
                raw[0] = 0x04;
                Buffer.BlockCopy(p.Q.X, 0, raw, 1, coordSize);
                Buffer.BlockCopy(p.Q.Y, 0, raw, 1 + coordSize, coordSize);
                return raw;
            }
            catch
            {
                // Fall through to manual ASN.1 walk.
            }
#endif

            // Manual ASN.1 walk: SubjectPublicKeyInfo ::= SEQUENCE { AlgorithmIdentifier, BIT STRING }
            // BIT STRING value = 0x00 (unused-bits) || rawSec1Point
            var bitString = TryFindBitString(bytes);
            if (bitString != null && bitString.Length > 1 && bitString[0] == 0x00 && bitString[1] == 0x04)
            {
                var raw = new byte[bitString.Length - 1];
                Buffer.BlockCopy(bitString, 1, raw, 0, raw.Length);
                return raw;
            }

            // Last resort: return as-is so callers don't crash; verifiers will still reject it,
            // but logs/CertificatesBase64 etc. remain populated.
            return bytes;
        }

        private static byte[] TryFindBitString(byte[] der)
        {
            // Minimal scanner: walk the outer SEQUENCE and return the contents of the first
            // top-level BIT STRING (tag 0x03) encountered.
            try
            {
                if (der.Length < 2 || der[0] != 0x30) return null;
                var (outerLen, outerHeader) = ReadAsn1Length(der, 1);
                var offset = 1 + outerHeader;
                var end = offset + outerLen;
                while (offset < end && offset < der.Length)
                {
                    var tag = der[offset];
                    var (len, headerLen) = ReadAsn1Length(der, offset + 1);
                    var contentStart = offset + 1 + headerLen;
                    if (tag == 0x03)
                    {
                        var content = new byte[len];
                        Buffer.BlockCopy(der, contentStart, content, 0, len);
                        return content;
                    }
                    offset = contentStart + len;
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private static (int length, int headerLength) ReadAsn1Length(byte[] der, int offset)
        {
            var first = der[offset];
            if ((first & 0x80) == 0)
            {
                return (first, 1);
            }
            var lenOfLen = first & 0x7F;
            var length = 0;
            for (var i = 0; i < lenOfLen; i++)
            {
                length = (length << 8) | der[offset + 1 + i];
            }
            return (length, 1 + lenOfLen);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fccProcessHost?.Dispose();
                _fccErsApiProvider.Dispose();
            }
        }
    }
}
