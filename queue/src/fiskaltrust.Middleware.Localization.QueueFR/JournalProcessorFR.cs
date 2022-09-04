using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.Helpers;
using CsvHelper;
using System.Globalization;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueFR.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueFR
{
    public class JournalProcessorFR : IJournalProcessor, IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorFR> _logger;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly ArchiveProcessor _archiveProcessor;
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IMiddlewareJournalFRRepository _journalFRRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;

        public JournalProcessorFR(ILogger<JournalProcessorFR> logger, MiddlewareConfiguration middlewareConfiguration, ArchiveProcessor archiveProcessor,
            IReadOnlyConfigurationRepository configurationRepository, IMiddlewareJournalFRRepository journalFRRepository, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _logger = logger;
            _middlewareConfiguration = middlewareConfiguration;
            _archiveProcessor = archiveProcessor;
            _configurationRepository = configurationRepository;
            _journalFRRepository = journalFRRepository;
            _queueItemRepository = queueItemRepository;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            if ((0xFFFF000000000000 & (ulong) request.ftJournalType) != 0x4652000000000000)
            {
                throw new ArgumentException($"The given ftJournalType 0x{request.ftJournalType:x} is not supported in French Middleware instances.");
            }

            _logger.LogDebug($"Processing JournalRequest for FR (Type: {request.ftJournalType:X}");

            return request.ftJournalType switch
            {
                (long) JournalTypes.TicketJournalsFR => ExportToCsvAsync<TicketPayload>("T", request),
                (long) JournalTypes.PaymentProveJournalsFR => ExportToCsvAsync<TicketPayload>("P", request),
                (long) JournalTypes.InvoiceJournalsFR => ExportToCsvAsync<TicketPayload>("I", request),
                (long) JournalTypes.GrandTotalJournalsFR => ExportToCsvAsync<GrandTotalPayload>("G", request),
                (long) JournalTypes.BillJournalsFR => ExportToCsvAsync<TicketPayload>("B", request),
                (long) JournalTypes.ArchiveJournalsFR => ExportToCsvAsync<ArchivePayload>("A", request),
                (long) JournalTypes.LogJournalsFR => ExportToCsvAsync<TicketPayload>("L", request),
                (long) JournalTypes.CopyJournalsFR => ExportToCsvAsync<CopyPayload>("C", request),
                (long) JournalTypes.TrainingJournalsFR => ExportToCsvAsync<TicketPayload>("X", request),
                (long) JournalTypes.Archive => ExportArchiveAsync(request),
                _ => ExportQueueFRsAsync()
            };
        }

        private async IAsyncEnumerable<JournalResponse> ExportArchiveAsync(JournalRequest request)
        {
            var isQueueRowBasedRequest = (request.ftJournalType & 0x0000000000010000) != 0;
            if (!isQueueRowBasedRequest)
            {
                yield break;
            }

            var workingDirectory = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "Archive", DateTime.Now.ToString("yyyyMMddhhmmssfff"));
            Directory.CreateDirectory(workingDirectory);
            var targetFile = Path.Combine(workingDirectory, "export.zip");

            try
            {
                var archiveQueueItem = await _queueItemRepository.GetByQueueRowAsync(request.From);
                if (archiveQueueItem == null)
                {
                    _logger.LogError($"No QueueItem found for row '{request.From}', archive cannot be processed.");
                    yield break;
                }

                var receiptResponse = JsonConvert.DeserializeObject<ReceiptResponse>(archiveQueueItem.response);
                if (receiptResponse == null)
                {
                    _logger.LogError($"QueueItem '{archiveQueueItem.ftQueueItemId}' found for row '{request.From}', but has no response, therefore the archive export cannot be processed. This may happen in case that an error occured while processing the targeted archive receipt.");
                    yield break;
                }

                var archiveSignature = receiptResponse.ftSignatures.Where(s => s.ftSignatureType == 0x4652000000000001).First();
                var archivePayload = JsonConvert.DeserializeObject<ArchivePayload>(Encoding.UTF8.GetString(ConversionHelper.FromBase64UrlString(archiveSignature.Data.Split('.')[1])));
                
                var queue = await _configurationRepository.GetQueueFRAsync(_middlewareConfiguration.QueueId);
                var scu = await _configurationRepository.GetSignaturCreationUnitFRAsync(queue.ftSignaturCreationUnitFRId);

                await _archiveProcessor.ExportArchiveDataAsync(targetFile, archivePayload, scu);
               
                if (!File.Exists(targetFile))
                {
                    _logger.LogError("No archive export could be generated.");                    
                    yield break;
                }

                foreach (var chunk in FileHelper.ReadFileAsChunks(targetFile, request.MaxChunkSize))
                {
                    yield return new JournalResponse
                    {
                        Chunk = chunk.ToList()
                    };
                }
            }
            finally
            {
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
            }
        }

        private async IAsyncEnumerable<JournalResponse> ExportQueueFRsAsync()
        {
            var result = new { QueueFRList = await _configurationRepository.GetQueueFRListAsync() };
            yield return new JournalResponse
            {
                Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)).ToList()
            };
        }

        private async IAsyncEnumerable<JournalResponse> ExportToCsvAsync<T>(string journalType, JournalRequest request) where T : MinimumPayload
        {
            var journalFRRepo = new JournalFRRepositoryRangeDecorator(_journalFRRepository, request.From, request.To, journalType);

            var workingDirectory = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "CSV", DateTime.Now.ToString("yyyyMMddhhmmssfff"));
            Directory.CreateDirectory(workingDirectory);
            var csvPath = Path.Combine(workingDirectory, "export.csv");

            try
            {
                using (var writer = new StreamWriter(csvPath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<T>();
                    await csv.NextRecordAsync();

                    await foreach (var journal in journalFRRepo.GetAsync())
                    {
                        var record = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(ConversionHelper.FromBase64UrlString(journal.JWT.Split('.')[1])));
                        csv.WriteRecord(record);
                        await csv.NextRecordAsync();
                    }
                }

                foreach (var chunk in FileHelper.ReadFileAsChunks(csvPath, request.MaxChunkSize))
                {
                    yield return new JournalResponse
                    {
                        Chunk = chunk.ToList()
                    };
                }
            }
            finally
            {
                if (File.Exists(csvPath))
                {
                    File.Delete(csvPath);
                }
            }
        }
    }
}
