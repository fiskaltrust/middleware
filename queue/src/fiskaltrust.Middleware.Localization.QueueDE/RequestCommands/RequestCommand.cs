using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    public abstract class RequestCommand
    {
        private const long SSCD_FAILED_MODE_FLAG = 0x0000_0000_0000_0002;

        protected const string RETRYPOLICYEXCEPTION_NAME = "RetryPolicyException";

        protected readonly ILogger<RequestCommand> _logger;
        protected readonly SignatureFactoryDE _signatureFactory;
        protected readonly IDESSCDProvider _deSSCDProvider;
        protected readonly ITransactionPayloadFactory _transactionPayloadFactory;
        protected readonly IReadOnlyQueueItemRepository _queueItemRepository;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly ITransactionFactory _transactionFactory;
        protected readonly IPersistentTransactionRepository<FailedStartTransaction> _failedStartTransactionRepo;
        protected readonly IPersistentTransactionRepository<FailedFinishTransaction> _failedFinishTransactionRepo;
        protected readonly IPersistentTransactionRepository<OpenTransaction> _openTransactionRepo;
        private readonly IJournalDERepository _journalDERepository;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly QueueDEConfiguration _queueDEConfiguration;
        private readonly ITarFileCleanupService _tarFileCleanupService;

        protected string _certificationIdentification = null;

        public abstract string ReceiptName { get; }

        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider,
            ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository,
            IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo,
            IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
            _deSSCDProvider = deSSCDProvider;
            _transactionPayloadFactory = transactionPayloadFactory;
            _queueItemRepository = queueItemRepository;
            _configurationRepository = configurationRepository;
            _journalDERepository = journalDERepository;
            _middlewareConfiguration = middlewareConfiguration;
            _failedStartTransactionRepo = failedStartTransactionRepo;
            _failedFinishTransactionRepo = failedFinishTransactionRepo;
            _openTransactionRepo = openTransactionRepo;
            _transactionFactory = new TransactionFactory(_deSSCDProvider);
            _tarFileCleanupService = tarFileCleanupService;
            _queueDEConfiguration = queueDEConfiguration;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem);

        protected void ThrowIfImplicitFlow(ReceiptRequest request)
        {
            if (request.IsImplictFlow())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} ({ReceiptName}) can not use implicit-flow flag.");
            }
        }

        protected void ThrowIfNoImplicitFlow(ReceiptRequest request)
        {
            if (!request.IsImplictFlow())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} ({ReceiptName}) must use implicit-flow flag.");
            }
        }

        protected void ThrowIfTraining(ReceiptRequest request)
        {
            if (request.IsTraining())
            {
                throw new ArgumentException($"ReceiptCase {request.ftReceiptCase:X} can not use 'TrainingMode' flag.");
            }
        }

        public async Task PerformTarFileExportAsync(ftQueueItem queueItem, ftQueue queue, ftQueueDE queueDE, bool erase)
        {
            if (_queueDEConfiguration.TarFileExportMode == TarFileExportMode.None)
            { return; }

            if (_queueDEConfiguration.TarFileExportMode == TarFileExportMode.Erased)
            {
                var tseInfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                if (tseInfo.CurrentNumberOfStartedTransactions > 0)
                {
                    _logger.LogInformation("Skipped export because there are open transactions and {key} is set to {value}", nameof(TarFileExportMode), nameof(TarFileExportMode.Erased));
                    return;
                }
            }

            try
            {
                var exportService = new TarFileExportService();
                (var filePath, var success, var checkSum, var isErased) = await exportService.ProcessTarFileExportAsync(_deSSCDProvider.Instance, queueDE.ftQueueDEId, queueDE.CashBoxIdentification, erase, _middlewareConfiguration.ServiceFolder, _middlewareConfiguration.TarFileChunkSize).ConfigureAwait(false);
                if (success)
                {
                    Guid? ftJournalDEId = null;
                    if (_queueDEConfiguration.TarFileExportMode == TarFileExportMode.Erased && !isErased)
                    {
                        _logger.LogInformation("Export not saved to database because it was not erased from the TSE and {key} is set to {value}", nameof(TarFileExportMode), nameof(TarFileExportMode.Erased));
                    }
                    else
                    {
                        var journalDE = new ftJournalDE
                        {
                            ftJournalDEId = Guid.NewGuid(),
                            FileContentBase64 = Convert.ToBase64String(Compress(filePath)),
                            FileExtension = ".zip",
                            FileName = Path.GetFileNameWithoutExtension(filePath),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Number = queue.ftReceiptNumerator + 1
                        };
                        await _journalDERepository.InsertAsync(journalDE).ConfigureAwait(false);
                        ftJournalDEId = journalDE.ftJournalDEId;
                    }

                    await _tarFileCleanupService.CleanupTarFileAsync(ftJournalDEId, filePath, checkSum);
                }
                else
                {
                    _logger.LogWarning("Failed to export TAR file from SCU.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export TAR file from SCU.");
            }
        }

        protected async Task UpdateTseInfoAsync(Guid signaturCreationUnitDEID)
        {
            try
            {
                var signaturCreationUnitDE = await _configurationRepository.GetSignaturCreationUnitDEAsync(signaturCreationUnitDEID).ConfigureAwait(false);
                var tseInfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);
                signaturCreationUnitDE.TseInfoJson = JsonConvert.SerializeObject(tseInfo);
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(signaturCreationUnitDE).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to updated status of SCU ({signaturcreationunitdeid}). Will try again later...", signaturCreationUnitDEID);
            }
        }

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, ftQueueDE queueDE)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftCashBoxIdentification = queueDE.CashBoxIdentification,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4445000000000000
            };
        }

        protected async Task<string> GetCertificationIdentificationAsync() => _certificationIdentification ??= (await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false)).CertificationIdentification;

        protected async Task<(ulong transactionNumber, List<SignaturItem> signatures)> ProcessReceiptStartTransSignAsync(string transactionIdentifier, string processType, string payload, ftQueueItem queueItem, ftQueueDE queueDE, bool implicitFlow)
        {
            if (implicitFlow)
            {
                var startTransactionResult = await _transactionFactory.PerformStartTransactionRequestAsync(queueItem.ftQueueItemId, queueDE.CashBoxIdentification).ConfigureAwait(false);
                await _openTransactionRepo.InsertOrUpdateTransactionAsync(new OpenTransaction
                {
                    cbReceiptReference = transactionIdentifier,
                    StartMoment = startTransactionResult.TimeStamp,
                    TransactionNumber = (long) startTransactionResult.TransactionNumber,
                    StartTransactionSignatureBase64 = startTransactionResult.SignatureData.SignatureBase64
                }).ConfigureAwait(false);
            }

            var openTransaction = await _openTransactionRepo.GetAsync(transactionIdentifier).ConfigureAwait(false);

            var finishTransactionResult = await _transactionFactory.PerformFinishTransactionRequestAsync(processType, payload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, (ulong) openTransaction.TransactionNumber).ConfigureAwait(false);
            await _openTransactionRepo.RemoveAsync(transactionIdentifier).ConfigureAwait(false);

            var signatures = _signatureFactory.GetSignaturesForPosReceiptTransaction(openTransaction.StartTransactionSignatureBase64, finishTransactionResult, await GetCertificationIdentificationAsync().ConfigureAwait(false));

            return (finishTransactionResult.TransactionNumber, signatures);
        }

        protected async Task<ProcessReceiptResponse> ProcessReceiptAsync(string transactionIdentifier, string processType, string payload, ftQueueItem queueItem, ftQueueDE queueDE)
        {
            var startTransactionResult = await _transactionFactory.PerformStartTransactionRequestAsync(queueItem.ftQueueItemId, queueDE.CashBoxIdentification).ConfigureAwait(false);
            await _openTransactionRepo.InsertOrUpdateTransactionAsync(new OpenTransaction
            {
                cbReceiptReference = transactionIdentifier,
                StartTransactionSignatureBase64 = startTransactionResult.SignatureData.SignatureBase64,
                StartMoment = startTransactionResult.TimeStamp,
                TransactionNumber = (long) startTransactionResult.TransactionNumber
            }).ConfigureAwait(false);

            var finishTransactionResult = await _transactionFactory.PerformFinishTransactionRequestAsync(processType, payload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, startTransactionResult.TransactionNumber).ConfigureAwait(false);
            await _openTransactionRepo.RemoveAsync(transactionIdentifier).ConfigureAwait(false);

            var signatures = _signatureFactory.GetSignaturesForTransaction(startTransactionResult.SignatureData.SignatureBase64, finishTransactionResult, await GetCertificationIdentificationAsync().ConfigureAwait(false));

            return new ProcessReceiptResponse
            {
                ClientId = finishTransactionResult.ClientId,
                TransactionNumber = finishTransactionResult.TransactionNumber,
                SignatureAlgorithm = finishTransactionResult.SignatureData.SignatureAlgorithm,
                PublicKeyBase64 = finishTransactionResult.SignatureData.PublicKeyBase64,
                SerialNumberOctet = finishTransactionResult.TseSerialNumberOctet,
                Signatures = signatures
            };
        }

        protected async Task<RequestCommandResponse> ProcessSSCDFailedReceiptRequest(ReceiptRequest request, ftQueueItem queueItem, ftQueue queue, ftQueueDE queueDE, List<ftActionJournal> actionJournals = null)
        {
            if (queueDE.SSCDFailCount == 0)
            {
                queueDE.SSCDFailMoment = DateTime.UtcNow;
                queueDE.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }

            queueDE.SSCDFailCount++;

            _logger.LogDebug($"SSCDFailCount: {queueDE.SSCDFailCount}");
            if (!request.IsImplictFlow())
            {
                if ((request.ftReceiptCase & 0xFFFF) == 0x0008) // Explicit start transaction
                {
                    await _failedStartTransactionRepo.InsertOrUpdateTransactionAsync(new FailedStartTransaction
                    {
                        StartMoment = DateTime.UtcNow,
                        CashBoxIdentification = queueDE.CashBoxIdentification,
                        ftQueueItemId = queueItem.ftQueueItemId,
                        cbReceiptReference = request.cbReceiptReference,
                        Request = JsonConvert.SerializeObject(request)
                    }).ConfigureAwait(false);
                    _logger.LogDebug($"SSCDFail-StartTransaction: {request.cbReceiptReference}");
                }
                else if ((request.ftReceiptCase & 0xFFFF) == 0x0009) // Explicit update transaction
                {
                    // do nothing
                }
                else if ((request.ftReceiptCase & 0xFFFF) == 0x000a) // Explicit delta transaction
                {
                    // do nothing
                }
                else // Explicit finish receipt
                {
                    if (await _failedStartTransactionRepo.ExistsAsync(request.cbReceiptReference).ConfigureAwait(false))
                    {
                        await _failedStartTransactionRepo.RemoveAsync(request.cbReceiptReference).ConfigureAwait(false);
                        _logger.LogDebug($"SSCDFail-FinishTransaction: {request.cbReceiptReference}, started in SSCDFailed");
                    }
                    else if (await _openTransactionRepo.ExistsAsync(request.cbReceiptReference).ConfigureAwait(false))
                    {
                        // Transaction was started before failed mode
                        var tseStartTransaction = await _openTransactionRepo.GetAsync(request.cbReceiptReference).ConfigureAwait(false);

                        await _failedFinishTransactionRepo.InsertOrUpdateTransactionAsync(new FailedFinishTransaction
                        {
                            cbReceiptReference = request.cbReceiptReference,
                            CashBoxIdentification = queueDE.CashBoxIdentification,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            TransactionNumber = tseStartTransaction.TransactionNumber,
                            FinishMoment = DateTime.UtcNow,
                            Request = JsonConvert.SerializeObject(request)
                        }).ConfigureAwait(false);

                        _logger.LogDebug($"SSCDFail-FinishTransaction: {request.cbReceiptReference}, started with TransactionNumber: {tseStartTransaction.TransactionNumber}");
                    }
                    else
                    {
                        // Transaction was not found or not started
                        _logger.LogDebug($"SSCDFail-FinishTransaction: {request.cbReceiptReference}, unknown start");
                    }
                }
            }

            var receiptResponse = CreateReceiptResponse(request, queueItem, queueDE);

            actionJournals ??= new List<ftActionJournal>();

            receiptResponse.ftReceiptIdentification = request.GetReceiptIdentification(queue.ftReceiptNumerator, null);

            var signatures = new List<SignaturItem>
            {
                _signatureFactory.CreateTextSignature(0x0000_0000_0000_1000, "Kommunikation mit der technischen Sicherheitseinrichtung (TSE) fehlgeschlagen", $"Fehlerzähler seit {queueDE?.SSCDFailMoment:yyyy-MM-ddTHH:mm:ss.fffZ}: {queueDE?.SSCDFailCount}", false)
            };

            receiptResponse.ftSignatures = signatures.ToArray();
            receiptResponse.ftState += SSCD_FAILED_MODE_FLAG;

            return await Task.FromResult(new RequestCommandResponse()
            {
                ReceiptResponse = receiptResponse,
                Signatures = signatures,
                ActionJournals = actionJournals
            }).ConfigureAwait(false);
        }

        protected async Task<(ulong transactionNumber, List<SignaturItem> signatures)> ProcessUpdateTransactionRequestAsync(string transactionIdentifier, string processType, string payload, ftQueueItem queueItem, ftQueueDE queueDE)
        {
            var openTransaction = await _openTransactionRepo.GetAsync(transactionIdentifier).ConfigureAwait(false);
            var updatTransactionResult = await _transactionFactory.PerformUpdateTransactionRequestAsync(processType, payload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, (ulong) openTransaction.TransactionNumber).ConfigureAwait(false);
            var signatures = _signatureFactory.GetSignaturesForUpdateTransaction(updatTransactionResult);
            return (updatTransactionResult.TransactionNumber, signatures);
        }

        protected async Task<(ulong transactionNumber, List<SignaturItem> signatures, string clientId, string signatureAlgorithm, string publicKeyBase64, string serialNumberOctet)> ProcessInitialOperationReceiptAsync(string transactionIdentifier, string processType, string payload, ftQueueItem queueItem, ftQueueDE queueDE, bool clientIdRegistrationOnly)
        {
            if (!clientIdRegistrationOnly)
            {
                await _deSSCDProvider.Instance.SetTseStateAsync(new TseState { CurrentState = TseStates.Initialized }).ConfigureAwait(false);
                _logger.LogInformation("Successfully initialized TSE device");
            }

            var tseInfo = await _deSSCDProvider.Instance.GetTseInfoAsync().ConfigureAwait(false);

            if (tseInfo.CurrentState != TseStates.Initialized)
            {
                throw new Exception("TSE device is not in awaited lifecycle state 'Initialized'. Can not process 'Initial Operations Receipt'.");
            }

            try
            {
                await _deSSCDProvider.Instance.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = queueDE.CashBoxIdentification }).ConfigureAwait(false);
                _logger.LogInformation("Successfully registered TSE client. ClientId: {ClientId}", queueDE.CashBoxIdentification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TSE client registration failed. ClientId: {queueDE.CashBoxIdentification}");
                throw;
            }

            var processReceiptResponse = await ProcessReceiptAsync(transactionIdentifier, processType, payload, queueItem, queueDE).ConfigureAwait(false);
            return (processReceiptResponse.TransactionNumber, processReceiptResponse.Signatures, processReceiptResponse.ClientId, processReceiptResponse.SignatureAlgorithm, processReceiptResponse.PublicKeyBase64, processReceiptResponse.SerialNumberOctet);
        }

        protected async Task<(ulong transactionNumber, List<SignaturItem> signatures, string clientId, string signatureAlgorithm, string publicKeyBase64, string serialNumberOctet)> ProcessOutOfOperationReceiptAsync(string transactionIdentifier, string processType, string payload, ftQueueItem queueItem, ftQueueDE queueDE, bool isClientIdOnlyRequest)
        {
            try
            {
                var processReceiptResponse = await ProcessReceiptAsync(transactionIdentifier, processType, payload, queueItem, queueDE);
                return (processReceiptResponse.TransactionNumber, processReceiptResponse.Signatures, processReceiptResponse.ClientId, processReceiptResponse.SignatureAlgorithm, processReceiptResponse.PublicKeyBase64, processReceiptResponse.SerialNumberOctet);
            }
            finally
            {
                await _deSSCDProvider.Instance.UnregisterClientIdAsync(new UnregisterClientIdRequest { ClientId = queueDE.CashBoxIdentification }).ConfigureAwait(false);
                if (!isClientIdOnlyRequest)
                {
                    await _deSSCDProvider.Instance.SetTseStateAsync(new TseState { CurrentState = TseStates.Terminated }).ConfigureAwait(false);
                }
            }
        }

        public static byte[] Compress(string sourcePath)
        {
            if (sourcePath == null)
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            using var ms = new MemoryStream();
            using (var arch = new ZipArchive(ms, ZipArchiveMode.Create))
            {
                arch.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
            }

            return ms.ToArray();
        }
    }
}