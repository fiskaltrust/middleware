using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueAT
{
    public class JournalProcessorAT : IJournalProcessor, IMarketSpecificJournalProcessor
    {
        private readonly ILogger<JournalProcessorAT> _logger;
        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly ExportService _exportService;
        private readonly IReadOnlyConfigurationRepository _configurationRepository;

        public JournalProcessorAT(ILogger<JournalProcessorAT> logger, MiddlewareConfiguration middlewareConfiguration, ExportService exportService,
            IReadOnlyConfigurationRepository configurationRepository)
        {
            _logger = logger;
            _middlewareConfiguration = middlewareConfiguration;
            _exportService = exportService;
            _configurationRepository = configurationRepository;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            if ((0xFFFF000000000000 & (ulong) request.ftJournalType) != 0x4154000000000000)
            {
                throw new ArgumentException($"The given ftJournalType 0x{request.ftJournalType:x} is not supported in Austrian Middleware instances.");
            }

            _logger.LogDebug($"Processing JournalRequest for AT (Type: {request.ftJournalType:X}");

            return request.ftJournalType switch
            {
                (long) JournalTypes.RKSV => ExportRksvAsync(request),
                _ => ExportQueueATsAsync()
            };
        }

        private async IAsyncEnumerable<JournalResponse> ExportRksvAsync(JournalRequest request)
        {
            var workingDirectory = Path.Combine(_middlewareConfiguration.ServiceFolder, "Exports", _middlewareConfiguration.QueueId.ToString(), "RKSV", DateTime.Now.ToString("yyyyMMddhhmmssfff"));
            Directory.CreateDirectory(workingDirectory);
            var targetFile = Path.Combine(workingDirectory, "export.zip");

            try
            {
                await _exportService.PerformRksvJournalExportAsync(request.From, request.To, targetFile);

                if (!File.Exists(targetFile))
                {
                    _logger.LogError("No RSKV export could be generated.");
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

        private async IAsyncEnumerable<JournalResponse> ExportQueueATsAsync()
        {
            var result = new { QueueATList = await _configurationRepository.GetQueueATListAsync() };
            yield return new JournalResponse
            {
                Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)).ToList()
            };
        }
    }
}
