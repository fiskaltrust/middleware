using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Exports.Common.Helpers;
using fiskaltrust.Exports.DSFinVK;
using fiskaltrust.Exports.DSFinVK.Models;
using fiskaltrust.Exports.TAR;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Helpers;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class JournalProcessorDE : IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorDE> _logger;
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;
        private readonly IReadOnlyReceiptJournalRepository _receiptJournalRepository;
        private readonly IReadOnlyJournalDERepository _journalDERepository;
        private readonly IMiddlewareRepository<ftReceiptJournal> _middlewareReceiptJournalRepository;
        private readonly IMiddlewareRepository<ftJournalDE> _middlewareJournalDERepository;
        private readonly IReadOnlyActionJournalRepository _actionJournalRepository;
        private readonly IDESSCDProvider _deSSCDProvider;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly IMasterDataService _masterDataService;
        private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;
        private readonly ITarFileCleanupService _tarFileCleanupService;
        private readonly QueueDEConfiguration _queueDEConfiguration;

        public JournalProcessorDE(
            ILogger<JournalProcessorDE> logger,
            IReadOnlyConfigurationRepository configurationRepository,
            IReadOnlyQueueItemRepository queueItemRepository,
            IReadOnlyReceiptJournalRepository receiptJournalRepository,
            IReadOnlyJournalDERepository journalDERepository,
            IMiddlewareRepository<ftReceiptJournal> middlewareReceiptJournalRepository,
            IMiddlewareRepository<ftJournalDE> middlewareJournalDERepository,
            IReadOnlyActionJournalRepository actionJournalRepository,
            IDESSCDProvider deSSCDProvider,
            MiddlewareConfiguration middlewareConfiguration,
            IMasterDataService masterDataService,
            IMiddlewareQueueItemRepository middlewareQueueItemRepository,
            ITarFileCleanupService tarFileCleanupService,
            QueueDEConfiguration queueDEConfiguration)
        {
            _logger = logger;
            _configurationRepository = configurationRepository;
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _journalDERepository = journalDERepository;
            _middlewareReceiptJournalRepository = middlewareReceiptJournalRepository;
            _middlewareJournalDERepository = middlewareJournalDERepository;
            _actionJournalRepository = actionJournalRepository;
            _deSSCDProvider = deSSCDProvider;
            _middlewareConfiguration = middlewareConfiguration;
            _masterDataService = masterDataService;
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
            _tarFileCleanupService = tarFileCleanupService;
            _queueDEConfiguration = queueDEConfiguration;
        }

        public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            _logger.LogDebug($"Processing JournalRequest for DE (Type: {request.ftJournalType:X}");
            if (request.ftJournalType == (long) JournalTypes.TarExportFromTSE)
            {
                if (request.MaxChunkSize == 0)
                {
                    request.MaxChunkSize = _middlewareConfiguration.TarFileChunkSize;
                }
                await foreach (var value in ProcessTarExportFromTSEAsync(request).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
            else if (request.ftJournalType == (long) JournalTypes.DSFinVKExport)
            {
                await foreach (var value in ProcessDSFinVKExportAsync(request).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
            else if (request.ftJournalType == (long) JournalTypes.TarExportFromDatabase)
            {
                await foreach (var value in ProcessTarExportFromDatabaseAsync(request).ConfigureAwait(false))
                {
                    yield return value;
                }
            }
            else
            {
                var result = new
                {
                    QueueDEList = _configurationRepository.GetQueueDEListAsync().Result
                };
                yield return new JournalResponse
                {
                    Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)).ToList()
                };
            }
        }

        private async IAsyncEnumerable<JournalResponse> ProcessTarExportFromDatabaseAsync(JournalRequest request)
        {
            var journalDERepository = new JournalDERepositoryRangeDecorator(_middlewareJournalDERepository, _journalDERepository, request.From, request.To);

            var workingDirectory = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "TAR", DateTime.Now.ToString("yyyyMMddhhmmssfff"));
            Directory.CreateDirectory(workingDirectory);

            try
            {
                var tarPath = Path.Combine(workingDirectory, "export.tar");

                var exporter = new TarExporter(_logger, journalDERepository);
                await exporter.ExportAsync(tarPath);

                var fi = new FileInfo(tarPath);
                if (!fi.Exists || fi.Length == 0)
                {
                    _logger.LogInformation("No TAR export was generated. This may happen if there were no TAR files to export during the specified time range.");
                    yield break;
                }

                foreach (var chunk in FileHelpers.ReadFileAsChunks(tarPath, request.MaxChunkSize))
                {
                    yield return new JournalResponse
                    {
                        Chunk = chunk.ToList()
                    };
                }
            }
            finally
            {
                _tarFileCleanupService.CleanupTarFileDirectory(workingDirectory);
            }
        }

        private async IAsyncEnumerable<JournalResponse> ProcessTarExportFromTSEAsync(JournalRequest request)
        {
            var exportSession = await _deSSCDProvider.Instance.StartExportSessionAsync(new StartExportSessionRequest()).ConfigureAwait(false);
            var sha256CheckSum = "";

            byte[] chunk;
            var response = new JournalResponse();
            try
            {
                using (var stream = new FileStream(exportSession.TokenId + ".temp", FileMode.Create, FileAccess.ReadWrite))
                {
                    ExportDataResponse export;
                    do
                    {
                        export = await _deSSCDProvider.Instance.ExportDataAsync(new ExportDataRequest
                        {
                            TokenId = exportSession.TokenId,
                            MaxChunkSize = request.MaxChunkSize
                        }).ConfigureAwait(false);
                        if (!export.TotalTarFileSizeAvailable)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                        }
                        else
                        {
                            chunk = Convert.FromBase64String(export.TarFileByteChunkBase64);
                            stream.Write(chunk, 0, chunk.Length);
                            response.Chunk = chunk.ToList();
                            yield return response;
                        }
                    } while (!export.TarFileEndOfFile);
                    using var sha256 = SHA256.Create();
                    stream.Position = 0;
                    sha256CheckSum = Convert.ToBase64String(sha256.ComputeHash(stream));
                }

                var endSessionRequest = new EndExportSessionRequest
                {
                    TokenId = exportSession.TokenId,
                    Sha256ChecksumBase64 = sha256CheckSum
                };
                var endExportSessionResult = await _deSSCDProvider.Instance.EndExportSessionAsync(endSessionRequest).ConfigureAwait(false);
                if (!endExportSessionResult.IsValid)
                {
                    throw new Exception("The TAR file export was not successful.");
                }
            }
            finally
            {
                if (File.Exists(exportSession.TokenId + ".temp"))
                {
                    File.Delete(exportSession.TokenId + ".temp");
                }
            }
            yield break;
        }

        private async IAsyncEnumerable<JournalResponse> ProcessDSFinVKExportAsync(JournalRequest request)
        {

            var receiptJournalRepository = new ReceiptJournalRepositoryRangeDecorator(_middlewareReceiptJournalRepository, _receiptJournalRepository, request.From, request.To);

            var queueDE = await _configurationRepository.GetQueueDEAsync(_middlewareConfiguration.QueueId).ConfigureAwait(false);
            var scu = await _configurationRepository.GetSignaturCreationUnitDEAsync(queueDE.ftSignaturCreationUnitDEId.Value).ConfigureAwait(false);
            var tseInfo = JsonConvert.DeserializeObject<TseInfo>(scu.TseInfoJson);
            var workingDirectory = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", queueDE.ftQueueDEId.ToString(), "DSFinV-K", DateTime.Now.ToString("yyyyMMddhhmmssfff"));
            Directory.CreateDirectory(workingDirectory);

            try
            {
                var certificateBase64 = await GetCertificateBase64(queueDE).ConfigureAwait(false);
                var firstZNumber = await GetFirstZNumber(_actionJournalRepository, receiptJournalRepository, request).ConfigureAwait(false);

                var targetDirectory = $"{Path.Combine(workingDirectory, "raw")}{Path.DirectorySeparatorChar}";
                var to = request.To;
                if (request.To == 0)
                {
                    to = long.MaxValue;
                }

                var parameters = new DSFinVKParameters
                {
                    CashboxIdentification = queueDE.CashBoxIdentification,
                    FirstZNumber = firstZNumber,
                    TargetDirectory = targetDirectory,
                    TSECertificateBase64 = certificateBase64,
                    ReferencesLookUpType = _queueDEConfiguration.DisableDsfinvkExportReferences ? ReferencesLookUpType.NoReferences : ReferencesLookUpType.AddReferences,
                    IncludeOrders = _queueDEConfiguration.ExcludeDsfinvkOrders ? false : true,
                    PublicKeyBase64 = tseInfo?.PublicKeyBase64,
                    SignAlgorithm = tseInfo?.SignatureAlgorithm,
                    TimeFormat = tseInfo?.LogTimeFormat,
                    From = request.From,
                    To = to
                };

                var readOnlyReceiptReferenceRepository = new ReadOnlyReceiptReferenceRepository(_middlewareQueueItemRepository);
                var fallbackMasterDataRepo = new ReadOnlyMasterDataConfigurationRepository(_masterDataService.GetFromConfig());
                var dailyClosingRepository = new DailyClosingRepository(_actionJournalRepository, _middlewareQueueItemRepository);

                // No need to wrap the QueueItemRepository, as the DSFinV-K exporter only uses the GetAsync(Guid id) method
                var exporter = new DSFinVKExporter(_logger, receiptJournalRepository, _queueItemRepository, readOnlyReceiptReferenceRepository, dailyClosingRepository, fallbackMasterDataRepo);

                await exporter.ExportAsync(parameters).ConfigureAwait(false);

                if (!Directory.Exists(targetDirectory))
                {
                    _logger.LogWarning("No DSFinV-K was generated. Make sure you included the daily-closing receipt in the requested time range.");
                    yield break;
                }

                var zipPath = Path.Combine(workingDirectory, "export.zip");
                await Task.Run(() => ZipFile.CreateFromDirectory(targetDirectory, zipPath)).ConfigureAwait(false);
                foreach (var chunk in FileHelpers.ReadFileAsChunks(zipPath, request.MaxChunkSize))
                {
                    yield return new JournalResponse
                    {
                        Chunk = chunk
                    };
                }
            }
            finally
            {
                _tarFileCleanupService.CleanupTarFileDirectory(workingDirectory);
            }
        }

        private async Task<string> GetCertificateBase64(ftQueueDE queueDE)
        {
            if (queueDE.ftSignaturCreationUnitDEId.HasValue)
            {
                var scuDE = await _configurationRepository.GetSignaturCreationUnitDEAsync(queueDE.ftSignaturCreationUnitDEId.Value).ConfigureAwait(false);
                if (scuDE.TseInfoJson != null)
                {
                    var tseInfo = JsonConvert.DeserializeObject<TseInfo>(scuDE.TseInfoJson);
                    return tseInfo.CertificatesBase64?.FirstOrDefault() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private async Task<int> GetFirstZNumber(IReadOnlyActionJournalRepository actionJournalRepository, IReadOnlyReceiptJournalRepository receiptJournalRepository, JournalRequest request)
        {
            var dailyClosingRepository = new DailyClosingRepository(_actionJournalRepository, _middlewareQueueItemRepository);

            var firstZNumber = 1;
            var actionJournals = (await actionJournalRepository.GetAsync().ConfigureAwait(false)).OrderBy(x => x.TimeStamp);
            var receiptJournals = (await receiptJournalRepository.GetAsync().ConfigureAwait(false)).ToList();

            foreach (var actionJournal in actionJournals.Where(x => x.Type != null && x.Type.EndsWith("0007") && (x.Type.StartsWith("4445") || x.Type.StartsWith("0x4445"))))
            {
                if (actionJournal.ftQueueItemId == actionJournal.ftQueueId)
                {
                    var queueItem = await dailyClosingRepository.GetQueueItemOfMissingIdAsync(actionJournal).ConfigureAwait(false);
                    actionJournal.ftQueueItemId = queueItem.ftQueueItemId;
                }

                var receiptJournal = receiptJournals.FirstOrDefault(x => x.ftQueueItemId == actionJournal.ftQueueItemId);
                if (receiptJournal != null)
                {
                    if (receiptJournal.TimeStamp >= request.From)
                    {
                        break;
                    }
                }

                firstZNumber++;
            }

            return firstZNumber;
        }
    }
}
