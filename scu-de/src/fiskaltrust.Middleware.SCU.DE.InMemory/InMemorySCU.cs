using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.InMemory.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.InMemory
{
    public class InMemorySCU : IDESSCD
    {

        private readonly ConcurrentDictionary<ulong, long> _transactionsStates;
        private readonly ILogger<InMemorySCU> _logger;
        private readonly InMemoryTSE _inMemoryTSE;
        private const string OperationFailed = "Failed to execute {Operation} - Request: {Request}";

        public InMemorySCU(ILogger<InMemorySCU> logger, InMemoryTSE inMemoryTSE)
        {
            _transactionsStates = new ConcurrentDictionary<ulong, long>();
            _logger = logger;
            _inMemoryTSE = inMemoryTSE;
        }

        public async Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request)
        {
            try
            {
                 _ = request ?? throw new ArgumentNullException(nameof(request));
                var content = CreateTransaction(request.ClientId, request.ProcessType, request.ProcessDataBase64, "ACTIVE");
                var transaction = _inMemoryTSE.StartTransactionAsync(content);
                _ = _transactionsStates.TryAdd(transaction.Number, transaction.LatestRevision);
                return await Task.FromResult(CreateStartTransactionResponse(transaction)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(StartTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request)
        {
            try
            {
                _ = request ?? throw new ArgumentNullException(nameof(request));
                var lastRevisionForTransaction = await GetTransactionStateByNumberAsync(request.TransactionNumber).ConfigureAwait(false);
                var content = CreateTransaction(request.ClientId, request.ProcessType, request.ProcessDataBase64, "ACTIVE");
                var transaction = _inMemoryTSE.PutTransactionRequestWithStateAsync(request.TransactionNumber, lastRevisionForTransaction + 1, content);
                _ = _transactionsStates.TryUpdate(request.TransactionNumber, transaction.LatestRevision, lastRevisionForTransaction);
                return await Task.FromResult(CreateUpdateTransactionResponse(transaction)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(UpdateTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request)
        {
            try
            {
                _ = request ?? throw new ArgumentNullException(nameof(request));
                var lastRevisionForTransaction = await GetTransactionStateByNumberAsync(request.TransactionNumber).ConfigureAwait(false);
                var content = CreateTransaction(request.ClientId, request.ProcessType, request.ProcessDataBase64, "FINISHED");
                var transaction = _inMemoryTSE.PutTransactionRequestWithStateAsync(request.TransactionNumber, lastRevisionForTransaction, content);
                _ = _transactionsStates.TryRemove(request.TransactionNumber, out _);
                return await Task.FromResult(CreateFinishTransactionResponse(transaction)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(FinishTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<TseInfo> GetTseInfoAsync()
        {
            try
            {
                return await Task.Run(() => _inMemoryTSE.GetTseInfo()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(GetTseInfoAsync));
                throw;
            }
        }

        public async Task<TseState> SetTseStateAsync(TseState request)
        {
            try
            {
                _ = request ?? throw new ArgumentNullException(nameof(request));
                return await Task.Run(() => _inMemoryTSE.SetTseState(request)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(SetTseStateAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public Task ExecuteSetTseTimeAsync() => Task.CompletedTask;

        public Task ExecuteSelfTestAsync() => Task.CompletedTask;

        public async Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request)
        {
            try
            {
                var exportId = Guid.NewGuid();
                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = await Task.Run(() => InMemoryTSE.TssCertificateSerial).ConfigureAwait(false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(StartExportSessionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request)
        {
            try
            {
                _ = request ?? throw new ArgumentNullException(nameof(request));
                var exportId = Guid.NewGuid();
                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = await Task.Run(() => InMemoryTSE.TssCertificateSerial).ConfigureAwait(false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(StartExportSessionByTimeStampAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request)
        {
            try
            {
                var exportId = Guid.NewGuid();
                return new StartExportSessionResponse
                {
                    TokenId = exportId.ToString(),
                    TseSerialNumberOctet = await Task.Run(() => InMemoryTSE.TssCertificateSerial).ConfigureAwait(false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, OperationFailed, nameof(StartExportSessionByTransactionAsync), JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            return await Task.Run(() => InMemoryTSE.CreateExportDataResponse(request)).ConfigureAwait(false);
        }

        public async Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            return await Task.Run(() => CreateEndExportSessionAsync(request)).ConfigureAwait(false);
        }

        public async Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            return await Task.FromResult(new ScuDeEchoResponse
            {
                Message = request.Message
            }).ConfigureAwait(false);
        }

        public async Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            _inMemoryTSE.GetOrRegisterClient(request.ClientId);
            return new RegisterClientIdResponse
            {
                ClientIds = await Task.Run(() => _inMemoryTSE.GetClientIds()).ConfigureAwait(false)
            };
        }

        public async Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            _inMemoryTSE.UnregisterClient(request.ClientId);
            return await Task.FromResult(new UnregisterClientIdResponse
            {
                ClientIds = _inMemoryTSE.GetClientIds()
            }).ConfigureAwait(false);
        }

        private EndExportSessionResponse CreateEndExportSessionAsync(EndExportSessionRequest request)
        {
            return new EndExportSessionResponse
            {
                TokenId = request.TokenId,
                IsValid = true
            };
        }

        public async Task<long> GetTransactionStateByNumberAsync(ulong transactionNumber)
        {
            if (!_transactionsStates.TryGetValue(transactionNumber, out var transactionState))
            {
                var transaction = await Task.Run(() => _inMemoryTSE.GetTransactionDtoAsync(transactionNumber)).ConfigureAwait(false);
                if (transaction == null)
                {
                    return 1;
                }
                else
                {
                    _ = _transactionsStates.TryAdd(transaction.Number, transaction.LatestRevision);
                    return transaction.LatestRevision;
                }
            }
            return transactionState;
        }

        private static StartTransactionResponse CreateStartTransactionResponse(TransactionDto transaction)
        {
            return new StartTransactionResponse
            {
                TransactionNumber = transaction.Number,
                TseSerialNumberOctet = transaction.CertificateSerial,
                ClientId = transaction.ClientSerialNumber,
                TimeStamp = transaction.Log.Timestamp.FromUnixTime(),
                SignatureData = new TseSignatureData()
                {
                    SignatureBase64 = transaction.Signature.Value,
                    SignatureCounter = transaction.Signature.SignatureCounter,
                    SignatureAlgorithm = transaction.Signature.Algorithm,
                    PublicKeyBase64 = transaction.Signature.PublicKey,
                }
            };
        }

        private static UpdateTransactionResponse CreateUpdateTransactionResponse(TransactionDto transaction)
        {
            return new UpdateTransactionResponse
            {
                TransactionNumber = transaction.Number,
                TseSerialNumberOctet = transaction.CertificateSerial,
                ClientId = transaction.ClientSerialNumber,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(transaction.Schema.RawData.ProcessData)),
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
                TseSerialNumberOctet = transaction.CertificateSerial,
                ClientId = transaction.ClientSerialNumber,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(transaction.Schema.RawData.ProcessData)),
                ProcessType = transaction.Schema.RawData.ProcessType,
                StartTransactionTimeStamp = transaction.TimeStart,
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

        private TransactionRequestDto CreateTransaction(string clientId, string processType, string processDataBase64, string state)
        {
            _inMemoryTSE.GetOrRegisterClient(clientId);
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
    }
}
