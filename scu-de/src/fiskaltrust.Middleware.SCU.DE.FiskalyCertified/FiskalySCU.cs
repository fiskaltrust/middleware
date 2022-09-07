using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Exceptions;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified
{
    public class FiskalySCU : IDESSCD
    {
        private readonly ConcurrentDictionary<ulong, long> _transactionsStates;
        private readonly ConcurrentDictionary<string, ExportStateData> _readStreamPointer = new ConcurrentDictionary<string, ExportStateData>();
        private readonly ILogger<FiskalySCU> _logger;
        private readonly FiskalySCUConfiguration _configuration;
        private readonly ClientCache _clientCache;
        private readonly IFiskalyApiProvider _fiskalyApiProvider;
        private const string _noExport = "noexport-";
        private const string _lastExportedTransactionNumberKey = "LastExportedTransactionNumber";

        public FiskalySCU(ILogger<FiskalySCU> logger, IFiskalyApiProvider apiProvider, ClientCache clientCache, FiskalySCUConfiguration configuration)
        {
            _transactionsStates = new ConcurrentDictionary<ulong, long>();
            _logger = logger;
            _configuration = configuration;
            _fiskalyApiProvider = apiProvider;
            _clientCache = clientCache;
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                if (!await _clientCache.IsClientExistent(_configuration.TssId, request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }
                var txId = Guid.NewGuid();
                var content = CreateTransaction(_clientCache.GetClientId(request.ClientId), request.ProcessType, request.ProcessDataBase64, "ACTIVE");
                var transaction = await _fiskalyApiProvider.PutTransactionRequestAsync(_configuration.TssId, txId, content);

                _transactionsStates.TryAdd(transaction.Number, transaction.LatestRevision);
                return CreateStartTransactionResponse(transaction);
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
                if (!await _clientCache.IsClientExistent(_configuration.TssId, request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }
                var transaction = new TransactionDto
                {
                    Number = (uint) request.TransactionNumber,
                    Signature = new TransactionSignatureDto
                    {
                        Value = string.Empty
                    },
                    Schema = new TransactionDataDto()
                    {
                        RawData = new RawData()
                    },
                    Log = new TransactionLogDto()
                };
                return CreateUpdateTransactionResponse(transaction);
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
                if (!await _clientCache.IsClientExistent(_configuration.TssId, request.ClientId))
                {
                    throw new Exception($"The client {request.ClientId} is not registered.");
                }
                var lastRevisionForTransaction = await GetTransactionStateByNumberAsync(request.TransactionNumber);
                var content = CreateTransaction(_clientCache.GetClientId(request.ClientId), request.ProcessType, request.ProcessDataBase64, "FINISHED");
                var nextRevisionNumber = lastRevisionForTransaction;
                nextRevisionNumber += 1;
                var transaction = await _fiskalyApiProvider.PutTransactionRequestWithStateAsync(_configuration.TssId, request.TransactionNumber, nextRevisionNumber, content);
                _transactionsStates.TryRemove(request.TransactionNumber, out _);
                return CreateFinishTransactionResponse(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private async Task<long> GetTransactionStateByNumberAsync(ulong transactionNumber)
        {
            if (!_transactionsStates.TryGetValue(transactionNumber, out var transactionState))
            {
                var transaction = await _fiskalyApiProvider.GetTransactionDtoAsync(_configuration.TssId, transactionNumber);
                if (transaction == null)
                {
                    throw new Exception("The given transaction does not exist in this SCU.");
                }
                else
                {
                    _transactionsStates.TryAdd(transaction.Number, transaction.LatestRevision);
                    return transaction.LatestRevision;
                }
            }

            return transactionState;
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                var clientDto = await _fiskalyApiProvider.GetClientsAsync(_configuration.TssId).ConfigureAwait(false);
                var tssResult = await _fiskalyApiProvider.GetTseByIdAsync(_configuration.TssId);
                if (tssResult.State.Equals("DELETED") && tssResult.Env.Equals("TEST"))
                {
                    throw new FiskalyException("The specified TSE is in 'DELETED' state. Fiskaly automatically deletes all v2 test TSEs each Sunday, which can lead to this behavior; " +
                        "please produce a new TSE for test purposes. Production TSEs are not affected by these regular cleanups.");
                }
                var startedTransactions = await _fiskalyApiProvider.GetStartedTransactionsAsync(_configuration.TssId);
                var serial = tssResult.SerialNumber;

                return new TseInfo
                {
                    CurrentNumberOfClients = clientDto.Where(x => x.State.Equals("REGISTERED")).ToList().Count,
                    CurrentNumberOfStartedTransactions = startedTransactions.Count(),
                    SerialNumberOctet = serial,
                    PublicKeyBase64 = tssResult.PublicKey,
                    FirmwareIdentification = tssResult.Version,
                    CertificationIdentification = _configuration.CertificationId,
                    MaxNumberOfClients = tssResult.MaxNumberOfRegisteredClients ?? int.MaxValue,
                    MaxNumberOfStartedTransactions = tssResult.MaxNumberOfActiveTransactions ?? int.MaxValue,
                    CertificatesBase64 = new List<string>
                    {
                        tssResult.Certificate.AsBase64()
                    },
                    CurrentClientIds = clientDto.Where(x => x.State.Equals("REGISTERED")).Select(x => x.SerialNumber),
                    SignatureAlgorithm = tssResult.SignatureAlgorithm,
                    CurrentLogMemorySize = -1,
                    CurrentNumberOfSignatures = tssResult.SignatureCounter,
                    LogTimeFormat = tssResult.SignatureTimestampFormat,
                    MaxLogMemorySize = long.MaxValue,
                    MaxNumberOfSignatures = long.MaxValue,
                    CurrentStartedTransactionNumbers = startedTransactions.Select(x => (ulong) x.Number).ToList(),
                    CurrentState = ((FiskalyTseState) Enum.Parse(typeof(FiskalyTseState), tssResult.State, true)).ToTseStateEnum()
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
                var tssResult = await _fiskalyApiProvider.GetTseByIdAsync(_configuration.TssId);
                if(tssResult.State == FiskalyTseState.CREATED.ToString())
                {
                    throw new FiskalyException("The state of the TSS is 'CREATED' and therefore not yet personalized, which is currently not supported.");
                }
                var tseStateDto = new TseStateRequestDto()
                {
                    State = request.CurrentState.ToFiskalyTseState().ToString()
                };

                var response = await _fiskalyApiProvider.PatchTseStateAsync(_configuration.TssId, tseStateDto);

                return ((FiskalyTseState) Enum.Parse(typeof(FiskalyTseState), response.State, true)).ToTseState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private TransactionRequestDto CreateTransaction(Guid clientId, string processType, string processDataBase64, string state)
        {
            if (string.IsNullOrEmpty(processType) && string.IsNullOrEmpty(processDataBase64))
            {
                return new TransactionRequestDto
                {
                    ClientId = clientId,
                    State = state
                };
            }
            return new TransactionRequestDto
            {
                ClientId = clientId,
                State = state,
                Data = new TransactionDataDto
                {
                    RawData = new RawData
                    {
                        ProcessType = processType,
                        ProcessData = processDataBase64 ?? string.Empty
                    }
                }
            };
        }

        private static StartTransactionResponse CreateStartTransactionResponse(TransactionDto transaction)
        {
            return new StartTransactionResponse
            {
                TransactionNumber = transaction.Number,
                TseSerialNumberOctet = transaction.TssSerialNumber,
                ClientId = transaction.ClientSerialNumber,
                TimeStamp = transaction.Log.Timestamp.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transaction.Signature.Value,
                    SignatureCounter = transaction.Signature.SignatureCounter,
                    SignatureAlgorithm = transaction.Signature.Algorithm,
                    PublicKeyBase64 = transaction.Signature.PublicKey
                }
            };
        }

        private static UpdateTransactionResponse CreateUpdateTransactionResponse(TransactionDto transaction)
        {
            return new UpdateTransactionResponse
            {
                TransactionNumber = transaction.Number,
                TseSerialNumberOctet = transaction.TssSerialNumber,
                ClientId = transaction.ClientSerialNumber,
                ProcessDataBase64 = transaction.Schema.RawData.ProcessData,
                ProcessType = transaction.Schema.RawData.ProcessType,
                TimeStamp = transaction.Log.Timestamp.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transaction.Signature.Value,
                    SignatureCounter = transaction.Signature.SignatureCounter,
                    SignatureAlgorithm = transaction.Signature.Algorithm,
                    PublicKeyBase64 = transaction.Signature.PublicKey
                }
            };
        }

        private static FinishTransactionResponse CreateFinishTransactionResponse(TransactionDto transaction)
        {
            return new FinishTransactionResponse
            {
                TransactionNumber = transaction.Number,
                TseSerialNumberOctet = transaction.TssSerialNumber,
                ClientId = transaction.ClientSerialNumber,
                ProcessDataBase64 = transaction.Schema.RawData.ProcessData,
                ProcessType = transaction.Schema.RawData.ProcessType,
                StartTransactionTimeStamp = transaction.TimeStart.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transaction.Signature.Value,
                    SignatureCounter = transaction.Signature.SignatureCounter,
                    SignatureAlgorithm = transaction.Signature.Algorithm,
                    PublicKeyBase64 = transaction.Signature.PublicKey
                },
                TseTimeStampFormat = transaction.Log.TimestampFormat,
                TimeStamp = transaction.Log.Timestamp.FromUnixTime()
            };
        }

        public Task ExecuteSetTseTimeAsync() => Task.CompletedTask;

        public Task ExecuteSelfTestAsync() => Task.CompletedTask;

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                var exportRequest = new ExportTransactions();
                if (!string.IsNullOrEmpty(request.ClientId))
                {
                    exportRequest.ClientId = _clientCache.GetClientId(request.ClientId);
                }

                var tss = await _fiskalyApiProvider.GetTseByIdAsync(_configuration.TssId);
                if (!_configuration.EnableTarFileExport)
                {
                    return new StartExportSessionResponse
                    {
                        TokenId = _noExport + Guid.NewGuid().ToString(),
                        TseSerialNumberOctet = tss.SerialNumber
                    };
                }

                var (from, to) = GetExportRange(tss);

                var exportId = Guid.NewGuid();
                await _fiskalyApiProvider.RequestExportAsync(_configuration.TssId, exportRequest, exportId, from, to);
                await _fiskalyApiProvider.SetExportMetadataAsync(_configuration.TssId, exportId, from, to);
                SetExportState(exportId, ExportState.Running);
                CacheExportAsync(exportId).ExecuteInBackgroundThread();

                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = tss.SerialNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {Operation} - Request: {Request}", nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        private (long? from, long to) GetExportRange(TssDto tss)
        {
            var from = tss.Metadata.ContainsKey(_lastExportedTransactionNumberKey)
                ? Convert.ToInt64(tss.Metadata[_lastExportedTransactionNumberKey], CultureInfo.InvariantCulture) + 1
                : (long?) null;

            return (from, tss.TransactionCounter);
        }

        private async Task SetLastExportedTransactionNumber(long lastExportedTransactionNumber)
        {
            await _fiskalyApiProvider.PatchTseMetadataAsync(_configuration.TssId, new Dictionary<string, object> { { _lastExportedTransactionNumberKey, lastExportedTransactionNumber } });
        }

        private async Task CacheExportAsync(Guid exportId, int i = 0)
        {
            try
            {
                await _fiskalyApiProvider.StoreDownloadResultAsync(_configuration.TssId, exportId);
                SetExportState(exportId, ExportState.Succeeded);
            }catch(WebException)
            {
                if (_configuration.RetriesOnTarExportWebException > i)
                {
                    i++;
                    _logger.LogInformation($"WebException on Export from Fiskaly retry {i} from {_configuration.RetriesOnTarExportWebException}");
                    await Task.Delay(1000 * (i + 1)).ConfigureAwait(false);
                    await CacheExportAsync(exportId, i).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Failed to execute {Operation} - ExportId: {ExportId}", nameof(CacheExportAsync), exportId);
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

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request)
        {
            try
            {
                var exportRequest = new ExportTransactionsWithDatesDto
                {
                    StartDate = request.From.ToUnixTime(),
                    EndDate = request.To.ToUnixTime()
                };
                if (!string.IsNullOrEmpty(request.ClientId))
                {
                    exportRequest.ClientId = _clientCache.GetClientId(request.ClientId);
                }

                var tssResult = await _fiskalyApiProvider.GetTseByIdAsync(_configuration.TssId);
                var exportId = Guid.NewGuid();
                await _fiskalyApiProvider.RequestExportAsync(_configuration.TssId, exportRequest, exportId);
                SetExportState(exportId, ExportState.Running);
                CacheExportAsync(exportId).ExecuteInBackgroundThread();

                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = tssResult.SerialNumber
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
                var exportRequest = new ExportTransactionsWithTransactionNumberDto
                {
                    StartTransactionNumber = request.From.ToString(),
                    EndTransactionNumber = request.To.ToString()
                };
                if (!string.IsNullOrEmpty(request.ClientId))
                {
                    exportRequest.ClientId = _clientCache.GetClientId(request.ClientId);
                }

                var tssResult = await _fiskalyApiProvider.GetTseByIdAsync(_configuration.TssId);
                var exportId = Guid.NewGuid();
                await _fiskalyApiProvider.RequestExportAsync(_configuration.TssId, exportRequest, exportId);
                SetExportState(exportId, ExportState.Running);
                CacheExportAsync(exportId).ExecuteInBackgroundThread();

                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = tssResult.SerialNumber
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
                    throw new FiskalyException("The export failed to start. It needs to be retriggered");
                }
                var tempFileName = request.TokenId;
                var exportStateInformation = await _fiskalyApiProvider.GetExportStateInformationByIdAsync(_configuration.TssId, Guid.Parse(request.TokenId));
                if (exportStateInformation.State == "ERROR")
                {
                    throw new FiskalyException($"The export failed with a fiskaly internal error: {exportStateInformation.Exception}");
                }
                if (_readStreamPointer.TryGetValue(request.TokenId, out var exportStateData) && exportStateData.State == ExportState.Failed)
                {
                    throw exportStateData.Error;
                }

                if (exportStateInformation.State != "COMPLETED" || !File.Exists(tempFileName))
                {
                    return new ExportDataResponse
                    {
                        TokenId = request.TokenId,
                        TotalTarFileSize = -1,
                        TarFileEndOfFile = false,
                        TotalTarFileSizeAvailable = false
                    };
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
                                }else
                                {
                                    var metadata = await _fiskalyApiProvider.GetExportMetadataAsync(_configuration.TssId, Guid.Parse(request.TokenId));
                                    if (metadata.ContainsKey("end_transaction_number"))
                                    {
                                        await SetLastExportedTransactionNumber(Convert.ToInt64(metadata["end_transaction_number"], CultureInfo.InvariantCulture));
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
                if (!await _clientCache.IsClientExistent(_configuration.TssId, request.ClientId))
                {
                    if (_configuration.MaxClientCount.HasValue && _clientCache.GetClientIds().Count >= _configuration.MaxClientCount.Value)
                    {
                        throw new FiskalyException($"The client '{request.ClientId}' could not be registered, as the maximum number of permitted clients for this TSS was reached. If you obtained this TSE via fiskaltrust's shop as a Queue-based product, only one client can be registered. Please refer to our product documentation for more details.");
                    }

                    var clientId = Guid.NewGuid();
                    await _fiskalyApiProvider.CreateClientAsync(_configuration.TssId, request.ClientId, clientId);
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
            try
            {
                if (await _clientCache.IsClientExistent(_configuration.TssId, request.ClientId))
                {
                    await _fiskalyApiProvider.DisableClientAsync(_configuration.TssId, request.ClientId, _clientCache.GetClientId(request.ClientId));
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
    }
}
