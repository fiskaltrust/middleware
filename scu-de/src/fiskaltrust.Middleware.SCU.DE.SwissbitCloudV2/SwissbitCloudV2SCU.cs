﻿using System;
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
using TseInfo = fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Models.TseInfo;

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
                    ProcessData = request.ProcessDataBase64,
                    ProcessType = request.ProcessType
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
                var finishTransactionResponse = await _swissbitCloudV2Provider.TransactionAsync(TransactionType.UpdateTransaction, finishTransactionRequest);

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
                var tseInfo = await _swissbitCloudV2Provider.GetTseInfoAsync();
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
                await _swissbitCloudV2Provider.SetTseStateAsync(request);
                return request;
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

                if (!await _clientCache.IsClientExistent(request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }
                var startExportResponse = await _swissbitCloudV2Provider.StartExport();

                CacheExportAsync(startExportResponse.ExportId).ExecuteInBackgroundThread();

                SetExportState(startExportResponse.ExportId, ExportState.Running);

                return new StartExportSessionResponse
                {
                    TokenId = startExportResponse.ExportId,
                    TseSerialNumberOctet = _configuration.TseSerialNumber
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private async Task CacheExportAsync(string exportId, int currentTry = 0)
        {
            try
            {
                await _swissbitCloudV2Provider.StoreDownloadResultAsync(exportId);
                SetExportState(exportId, ExportState.Succeeded);
            }
            catch (WebException)
            {
                if (_configuration.RetriesOnTarExportWebException > currentTry)
                {
                    currentTry++;
                    _logger.LogWarning($"WebException on Export from SwissbitCloud retry {currentTry} from {_configuration.RetriesOnTarExportWebException}, DelayOnRetriesInMs: {_configuration.DelayOnRetriesInMs}.");
                    await Task.Delay(_configuration.DelayOnRetriesInMs * (currentTry + 1)).ConfigureAwait(false);
                    await CacheExportAsync(exportId, currentTry).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - ExportId: {ExportId}", nameof(CacheExportAsync), exportId);
                SetExportState(exportId, ExportState.Failed, ex);
            }
        }

        public Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request)
        {
            try
            {

                //Todo 
                var exportId = Guid.NewGuid();

                //CacheExportAsync(exportId).ExecuteInBackgroundThread();

                return Task.FromResult( new StartExportSessionResponse
                {
                    TokenId = exportId.ToString()

                    //Todo TseSerialNumberOctet = tssResult.SerialNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionByTimeStampAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request)
        {
            try
            {
                var exportId = Guid.NewGuid();
                //CacheExportAsync(exportId).ExecuteInBackgroundThread();

                return Task.FromResult( new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),

                    //Todo TseSerialNumberOctet = tssResult.SerialNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionByTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

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

                if (exportStateResponse.State == "failure")
                {
                    throw new SwissbitCloudV2Exception($"The export failed with a SwissbitCloudV2 internal error. ErrorCode: {exportStateResponse.ErrorCode} ErrorMessage: {exportStateResponse.ErrorMessage}");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateDat) && exportStateDat.State == ExportState.Failed)
                {
                    throw exportStateDat.Error;
                }

                if (exportStateResponse.State != "success" || !File.Exists(tempFileName))
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

        public Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            //Todo
            /*
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
                                var openTransaction = await _fiskalyApiProvider.GetStartedTransactionsAsync(_configuration.TssId);
                                if (openTransaction.Any())
                                {
                                    var list = string.Join(",", openTransaction.Select(x => x.Number).ToArray());
                                    _logger.LogWarning("Could not delete log files from TSE after successfully exporting them because the following transactions were open: {OpenTransactions}. " +
                                        "If these transactions are not used anymore and could not be closed automatically by a daily closing receipt, please consider sending a fail-transaction-receipt to cancel them.", list);
                                }
                                else
                                {
                                    Dictionary<string, object> metadata;
     
                                        metadata = await _fiskalyApiProvider.GetExportMetadataAsync(_configuration.TssId, Guid.Parse(request.TokenId));
                                    if (metadata.ContainsKey("end_transaction_number"))
                                    {
                                        await SetLastExportedTransactionNumber(Convert.ToInt64(metadata["end_transaction_number"], CultureInfo.InvariantCulture));
                                        sessionResponse.IsErased = true;
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
            }*/
            return Task.FromResult( new EndExportSessionResponse());
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

                    var clientId = Guid.NewGuid();
                    await _swissbitCloudV2Provider.CreateClientAsync(new ClientDto() {  ClientId = request.ClientId});
                    _clientCache.AddClient(request.ClientId, clientId);
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
            //Todo
            try
            {
                if (await _clientCache.IsClientExistent(request.ClientId))
                {
                    var clientDto = new ClientDto { ClientId = request.ClientId };
                    await _swissbitCloudV2Provider.DeregisterClientAsync(clientDto);

                    //Todo DisableClientAsync(_configuration.TssId, request.ClientId, _clientCache.GetClientId(request.ClientId));
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
                    SignatureAlgorithm = "transaction.Signature.Algorithm",
                    PublicKeyBase64 = "transaction.Signature.PublicKey"
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
                    SignatureAlgorithm = "transaction.Signature.Algorithm",
                    PublicKeyBase64 = "transaction.Signature.PublicKey"
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
                    SignatureAlgorithm = "transaction.Signature.Algorithm",
                    PublicKeyBase64 = "transaction.Signature.PublicKey"
                },
                TseTimeStampFormat = "transaction.Log.TimestampFormat",
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
