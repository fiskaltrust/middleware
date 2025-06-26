using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Exceptions;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Constants;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2
{
    public class SwissbitCloudV2SCU : IDESSCD
    {
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer;
        private readonly ILogger<SwissbitCloudV2SCU> _logger;
        private readonly SwissbitCloudV2SCUConfiguration _configuration;
        private readonly ClientCache _clientCache;
        private readonly ISwissbitCloudV2ApiProvider _swissbitCloudV2Provider;
        private const string _noExport = "noexport-";
        private TseInfo LastTseInfo;
        public SwissbitCloudV2SCU(ILogger<SwissbitCloudV2SCU> logger, ISwissbitCloudV2ApiProvider apiProvider, ClientCache clientCache, SwissbitCloudV2SCUConfiguration configuration)
        {
            _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
            _logger = logger;
            _configuration = configuration;
            _swissbitCloudV2Provider = apiProvider;
            _clientCache = clientCache;
           
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                if (!await _clientCache.IsClientExistent(request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }
                var startTransactionRequest = new TransactionRequestDto() { 
                    ClientId = request.ClientId,
                    ProcessData = request.ProcessDataBase64 ?? "",
                    ProcessType = request.ProcessType ?? "",
                };
                var startTransactionResponse = await _swissbitCloudV2Provider.TransactionAsync(TransactionType.StartTransaction, startTransactionRequest);

                return CreateStartTransactionResponse(request.ClientId, startTransactionResponse);
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
                if (!await _clientCache.IsClientExistent(request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }

                var updateTransactionRequest = new TransactionRequestDto() { 
                    ClientId = request.ClientId,
                    ProcessData = request.ProcessDataBase64,
                    ProcessType = request.ProcessType,
                    Number = (int) request.TransactionNumber,
                };
                var updateTransactionResponse = await _swissbitCloudV2Provider.TransactionAsync(TransactionType.UpdateTransaction, updateTransactionRequest);

                return CreateUpdateTransactionResponse(request.ClientId, updateTransactionRequest, updateTransactionResponse);
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
                if (!await _clientCache.IsClientExistent(request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }

                var finishTransactionRequest = new TransactionRequestDto()
                {
                    ClientId = request.ClientId,
                    ProcessData = request.ProcessDataBase64,
                    ProcessType = request.ProcessType,
                    Number = (int) request.TransactionNumber,
                };
                var finishTransactionResponse = await _swissbitCloudV2Provider.TransactionAsync(TransactionType.FinishTransaction, finishTransactionRequest);

                return CreateFinishTransactionResponse(request.ClientId, finishTransactionRequest, finishTransactionResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                var clientDtoTask = _swissbitCloudV2Provider.GetClientsAsync();
                var tseResultTask =  _swissbitCloudV2Provider.GetTseStatusAsync();
                var startedTransactionsTask =  _swissbitCloudV2Provider.GetStartedTransactionsAsync();
                
                await Task.WhenAll(clientDtoTask, tseResultTask, startedTransactionsTask).ConfigureAwait(false);

                var clientDto = await clientDtoTask;
                var tseResult = await tseResultTask;
                var startedTransactions = await startedTransactionsTask;

                var bytes = Encoding.ASCII.GetBytes(tseResult.CertificateChain);
                var cert = new X509Certificate2(bytes);

                var certPublicKey = BitConverter.ToString(cert.GetPublicKey());
                var certPublicKeyBytes = Encoding.ASCII.GetBytes(certPublicKey);

                var algorithm = cert.SignatureAlgorithm.FriendlyName;

                var tseInfo= new TseInfo
                {
                    CurrentNumberOfClients = tseResult.NumberOfRegisteredClients,
                    CurrentNumberOfStartedTransactions = tseResult.NumberOfStartedTransactions,
                    SerialNumberOctet = tseResult.SerialNumber,
                    PublicKeyBase64 = Convert.ToBase64String(certPublicKeyBytes),
                    FirmwareIdentification = tseResult.SoftwareVersion,
                    CertificationIdentification = _configuration.CertificationId,
                    MaxNumberOfClients = tseResult.MaxNumberOfRegisteredClients,
                    MaxNumberOfStartedTransactions = tseResult.MaxNumberOfStartedTransactions,
                    CertificatesBase64 = new List<string>
                    {
                        Convert.ToBase64String(Encoding.ASCII.GetBytes(tseResult.CertificateChain))
                    },
                    CurrentClientIds = clientDto,
                    SignatureAlgorithm = algorithm,
                    CurrentLogMemorySize = tseResult.StorageUsed,
                    CurrentNumberOfSignatures = tseResult.CreatedSignatures,
                    LogTimeFormat = "unixTime",
                    MaxLogMemorySize = tseResult.StorageCapacity,
                    MaxNumberOfSignatures = long.MaxValue,
                    CurrentStartedTransactionNumbers = startedTransactions.Select(x => (ulong) x).ToList(),
                    CurrentState = ((SwissbitCloudV2TseState) Enum.Parse(typeof(SwissbitCloudV2TseState), tseResult.InitializationState, true)).ToTseStateEnum()
                };
                LastTseInfo = tseInfo;

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
                var tssResult = await _swissbitCloudV2Provider.GetTseStatusAsync();
                if (tssResult.InitializationState == SwissbitCloudV2TseState.initialized.ToString() && request.CurrentState == TseStates.Initialized)
                {
                    return request;
                }
                if (tssResult.InitializationState == SwissbitCloudV2TseState.uninitialized.ToString())
                {
                    throw new SwissbitCloudV2Exception("The state of the TSE is 'UNINITIALIZED' and therefore not yet personalized, which is currently not supported.");
                }
                if (request.CurrentState != TseStates.Terminated)
                {
                    throw new SwissbitCloudV2Exception($"The state of the TSE is {tssResult.InitializationState} and therefore this request is not supported.");
                }

                var tseResult = await _swissbitCloudV2Provider.DisableTseAsync();

                return ((SwissbitCloudV2TseState) Enum.Parse(typeof(SwissbitCloudV2TseState), tseResult.InitializationState, true)).ToTseState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public Task ExecuteSetTseTimeAsync() => Task.CompletedTask;

        public Task ExecuteSelfTestAsync() => Task.CompletedTask;

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = _noExport + Guid.NewGuid().ToString(),
                        TseSerialNumberOctet = _configuration.TseSerialNumber
                    };
                }

                var exportDto = await _swissbitCloudV2Provider.StartExportAsync();

                CacheExportAsync(exportDto).ExecuteInBackgroundThread();

                SetExportState(exportDto.Id, ExportState.Running);

                return new StartExportSessionResponse
                {
                    TokenId = exportDto.Id,
                    TseSerialNumberOctet = _configuration.TseSerialNumber
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private async Task CacheExportAsync(ExportDto exportDto, int currentTry = 0)
        {
            try
            {
                await _swissbitCloudV2Provider.StoreDownloadResultAsync(exportDto);
                SetExportState(exportDto.Id, ExportState.Succeeded);
            }
            catch (WebException)
            {
                if (_configuration.RetriesOnTarExportWebException > currentTry)
                {
                    currentTry++;
                    _logger.LogWarning($"WebException on Export from SwissbitCloud retry {currentTry} from {_configuration.RetriesOnTarExportWebException}, DelayOnRetriesInMs: {_configuration.DelayOnRetriesInMs}.");
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    await CacheExportAsync(exportDto, currentTry).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - ExportId: {ExportId}", nameof(CacheExportAsync), exportDto.Id);
                SetExportState(exportDto.Id, ExportState.Failed, ex);
            }
        }

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request) => throw new NotImplementedException("Export by Time range is not implemented in SwissbitCloudV2");

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request) => throw new NotImplementedException("Export by Transaction range is not implemented in SwissbitCloudV2");

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
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
            try
            {
                if (!_readStreamPointer.ContainsKey(request.TokenId))
                {
                    throw new SwissbitCloudV2Exception("The export failed to start. It needs to be retriggered");
                }
                var tempFileName = request.TokenId;
                var exportId = Guid.Parse(request.TokenId);

                var exportStateResponse = await _swissbitCloudV2Provider.GetExportStateResponseByIdAsync(request.TokenId);

                if (exportStateResponse.Status == "failure")
                {
                    throw new SwissbitCloudV2Exception($"The export failed with a SwissbitCloudV2 internal error. ErrorCode: {exportStateResponse.ErrorCode} ErrorMessage: {exportStateResponse.ErrorMessage}");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateDat) && exportStateDat.State == ExportState.Failed)
                {
                    throw exportStateDat.Error;
                }

                if (exportStateResponse.Status != "success" || !File.Exists(tempFileName))
                {
                    return new ExportDataResponse
                    {
                        TokenId = request.TokenId,
                        TotalTarFileSize = -1,
                        TarFileEndOfFile = false,
                        TotalTarFileSizeAvailable = false
                    };
                }

                _readStreamPointer.TryGetValue(request.TokenId, out var exportStateData);
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

                        if (tempStream.Length - exportStateData.ReadPointer < chunkSize)
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
                            if (request.Erase)
                            {
                                var openTransaction = await _swissbitCloudV2Provider.GetStartedTransactionsAsync();
                                if (openTransaction.Any())
                                {
                                    var list = string.Join(",", openTransaction);
                                    _logger.LogWarning("Could not delete log files from TSE after successfully exporting them because the following transactions were open: {OpenTransactions}. " +
                                        "If these transactions are not used anymore and could not be closed automatically by a daily closing receipt, please consider sending a fail-transaction-receipt to cancel them.", list);
                                }
                                else
                                {
                                    var openExports = await _swissbitCloudV2Provider.GetExportsAsync();
                                    var exportDto = openExports.FirstOrDefault(x => x.Id == request.TokenId);
                                    if (exportDto != null)
                                    {
                                        exportDto = await _swissbitCloudV2Provider.DeleteExportByIdAsync(request.TokenId);
                                        if (!string.IsNullOrEmpty(exportDto?.ErrorCode))
                                        {
                                            _logger.LogWarning($"Could not delete log files from TSE after successfully exporting. ErrorCode: {exportDto.ErrorCode} Errormessage: {exportDto.ErrorMessage}.");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Could not delete log files from TSE after successfully exporting, as it was already deleted.");
                                    }
                                }
                            }
                            sessionResponse.IsValid = true;
                            return sessionResponse;
                        }
                    }
                    return sessionResponse;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(EndExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
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
                    _logger.LogError(ex, $"Failed to delete file {tempFileName} after succesfull export.");
                }
            }
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request) => await Task.FromResult(new ScuDeEchoResponse
        {
            Message = request.Message
        });

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            try
            {
                if (!await _clientCache.IsClientExistent( request.ClientId))
                {
                    if (_configuration.MaxClientCount.HasValue && _clientCache.GetClientIds().Count >= _configuration.MaxClientCount.Value)
                    {
                        throw new SwissbitCloudV2Exception($"The client '{request.ClientId}' could not be registered, as the maximum number of permitted clients for this TSS was reached. If you obtained this TSE via fiskaltrust's shop as a Queue-based product, only one client can be registered. Please refer to our product documentation for more details.");
                    }

                    await _swissbitCloudV2Provider.CreateClientAsync(new ClientDto() {  ClientId = request.ClientId});
                    _clientCache.AddClient(request.ClientId);
                }
                return new RegisterClientIdResponse
                {
                    ClientIds = _clientCache.GetClientIds()
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
                if (await _clientCache.IsClientExistent(request.ClientId))
                {
                    var clientDto = new ClientDto { ClientId = request.ClientId };
                    await _swissbitCloudV2Provider.DeregisterClientAsync(clientDto);

                    _clientCache.RemoveClient(request.ClientId);
                }

                return new UnregisterClientIdResponse
                {
                    ClientIds = _clientCache.GetClientIds()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(UnregisterClientIdAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private StartTransactionResponse CreateStartTransactionResponse(string clientId, TransactionResponseDto transactionResponse)
        {
            return new StartTransactionResponse
            {
                TransactionNumber = (ulong) transactionResponse.Number,
                TseSerialNumberOctet = _configuration.TseSerialNumber,
                ClientId = clientId,
                TimeStamp = transactionResponse.SignatureCreationTime.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transactionResponse.SignatureValue,
                    SignatureCounter = transactionResponse.SignatureCounter,
                    SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                    PublicKeyBase64 = LastTseInfo?.PublicKeyBase64
                }
            };
        }

        private UpdateTransactionResponse CreateUpdateTransactionResponse(string clientId, TransactionRequestDto transactionRequest, TransactionResponseDto transactionResponse)
        {
            return new UpdateTransactionResponse
            {
                TransactionNumber = (ulong) transactionResponse.Number,
                TseSerialNumberOctet = _configuration.TseSerialNumber,
                ClientId = clientId,
                ProcessDataBase64 = transactionRequest.ProcessData,
                ProcessType = transactionRequest.ProcessType,
                TimeStamp = transactionResponse.SignatureCreationTime.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transactionResponse.SignatureValue,
                    SignatureCounter = transactionResponse.SignatureCounter,
                    SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                    PublicKeyBase64 = LastTseInfo?.PublicKeyBase64
                }
            };
        }
        private FinishTransactionResponse CreateFinishTransactionResponse(string clientId, TransactionRequestDto transactionRequest, TransactionResponseDto transactionResponse)
        {
            return new FinishTransactionResponse
            {
                TransactionNumber = (ulong) transactionResponse.Number,
                TseSerialNumberOctet = _configuration.TseSerialNumber,
                ClientId = clientId,
                ProcessDataBase64 = transactionRequest.ProcessData,
                ProcessType = transactionRequest.ProcessType,
                StartTransactionTimeStamp = transactionResponse.SignatureCreationTime.FromUnixTime(),//To check
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transactionResponse.SignatureValue,
                    SignatureCounter = transactionResponse.SignatureCounter,
                    SignatureAlgorithm = LastTseInfo?.SignatureAlgorithm,
                    PublicKeyBase64 = LastTseInfo?.PublicKeyBase64
                },
                TseTimeStampFormat = "unixTime",
                TimeStamp = transactionResponse.SignatureCreationTime.FromUnixTime(),
            };
        }

        private void SetExportState(string exportId, ExportState exportState, Exception error = null)
        {
            _readStreamPointer.AddOrUpdate(exportId, new ExportStateData
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

        public void Dispose() => _swissbitCloudV2Provider.Dispose();
    }
}
