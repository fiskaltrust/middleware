using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue
{
    public class JournalProcessor : IJournalProcessor
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IMiddlewareRepository<ftQueueItem> _queueItemRepository;
        private readonly IMiddlewareRepository<ftReceiptJournal> _receiptJournalRepository;
        private readonly IMiddlewareRepository<ftActionJournal> _actionJournalRepository;
        private readonly IMiddlewareRepository<ftJournalAT> _journalATRepository;
        private readonly IMiddlewareRepository<ftJournalDE> _journalDERepository;
        private readonly IMiddlewareRepository<ftJournalFR> _journalFRRepository;
        private readonly IMarketSpecificJournalProcessor _marketSpecificJournalProcessor;
        private readonly ILogger<JournalProcessor> _logger;
        private readonly MiddlewareConfiguration _middlewareConfiguration;

        public JournalProcessor(
            IReadOnlyConfigurationRepository configurationRepository,
            IMiddlewareRepository<ftQueueItem> queueItemRepository,
            IMiddlewareRepository<ftReceiptJournal> receiptJournalRepository,
            IMiddlewareRepository<ftActionJournal> actionJournalRepository,
            IMiddlewareRepository<ftJournalAT> journalATRepository,
            IMiddlewareRepository<ftJournalDE> journalDERepository,
            IMiddlewareRepository<ftJournalFR> journalFRRepository,
            IMarketSpecificJournalProcessor marketSpecificJournalProcessor,
            ILogger<JournalProcessor> logger,
            MiddlewareConfiguration middlewareConfiguration)
        {
            _configurationRepository = configurationRepository;
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _actionJournalRepository = actionJournalRepository;
            _journalATRepository = journalATRepository;
            _journalDERepository = journalDERepository;
            _journalFRRepository = journalFRRepository;
            _marketSpecificJournalProcessor = marketSpecificJournalProcessor;
            _logger = logger;
            _middlewareConfiguration = middlewareConfiguration;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            try
            {
                if ((0xFFFF000000000000 & (ulong) request.ftJournalType) == 0x4445000000000000)
                {
                    ThrowIfQueueHasIncorrectCountrySet("DE", request.ftJournalType);
                    return _marketSpecificJournalProcessor.ProcessAsync(request);
                }

                if ((0xFFFF000000000000 & (ulong) request.ftJournalType) == 0x4D45000000000000)
                {
                    ThrowIfQueueHasIncorrectCountrySet("ME", request.ftJournalType);
                    return _marketSpecificJournalProcessor.ProcessAsync(request);
                }

                return request.ftJournalType switch
                {
                    (long) JournalTypes.ActionJournal => ToJournalResponseAsync(GetEntitiesAsync(_actionJournalRepository, request), request.MaxChunkSize),
                    (long) JournalTypes.ReceiptJournal => ToJournalResponseAsync(GetEntitiesAsync(_receiptJournalRepository, request), request.MaxChunkSize),
                    (long) JournalTypes.QueueItem => ToJournalResponseAsync(GetEntitiesAsync(_queueItemRepository, request), request.MaxChunkSize),
                    (long) JournalTypes.JournalAT => ToJournalResponseAsync(GetEntitiesAsync(_journalATRepository, request), request.MaxChunkSize),
                    (long) JournalTypes.JournalDE => ToJournalResponseAsync(GetEntitiesAsync(_journalDERepository, request), request.MaxChunkSize),
                    (long) JournalTypes.JournalFR => ToJournalResponseAsync(GetEntitiesAsync(_journalFRRepository, request), request.MaxChunkSize),
                    (long) JournalTypes.Configuration => new List<JournalResponse> {
                    new JournalResponse
                    {
                        Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(GetConfiguration().Result)).ToList()
                    }
                }.ToAsyncEnumerable(),
                    _ => new List<JournalResponse> {
                        new JournalResponse
                        {
                            Chunk = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                                {
                                    Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
                                    Version = typeof(JournalProcessor).Assembly.GetName().Version
                                }
                            )).ToList()
                        }
                }.ToAsyncEnumerable()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while processing the Journal request.");
                throw;
            }
        }

        private void ThrowIfQueueHasIncorrectCountrySet(string countryCode, long journalType)
        {
            if (countryCode != LocalizedQueueBootStrapperFactory.GetQueueLocalization(_middlewareConfiguration.QueueId, _middlewareConfiguration.Configuration))
            {
                throw new Exception($"The given journal type 0x'{journalType:x}' cannot be used with the current Queue, as this export type is not supported in this country.");
            }
        }

        private async Task<object> GetConfiguration()
        {
            return new
            {
                Assembly = typeof(JournalProcessor).Assembly.GetName().FullName,
                Version = typeof(JournalProcessor).Assembly.GetName().Version,
                CashBoxList = await _configurationRepository.GetCashBoxListAsync().ConfigureAwait(false),
                QueueList = await _configurationRepository.GetQueueListAsync().ConfigureAwait(false),
                QueueATList = await _configurationRepository.GetQueueATListAsync().ConfigureAwait(false),
                QueueDEList = await _configurationRepository.GetQueueDEListAsync().ConfigureAwait(false),
                QueueFRList = await _configurationRepository.GetQueueFRListAsync().ConfigureAwait(false),
                SignaturCreationUnitATList = await _configurationRepository.GetSignaturCreationUnitATListAsync().ConfigureAwait(false),
                SignaturCreationUnitDEList = await _configurationRepository.GetSignaturCreationUnitDEListAsync().ConfigureAwait(false),
                SignaturCreationUnitFRList = await _configurationRepository.GetSignaturCreationUnitFRListAsync().ConfigureAwait(false)
            };
        }

        private async IAsyncEnumerable<JournalResponse> ToJournalResponseAsync<T>(IAsyncEnumerable<T> asyncEnumerable, int chunkSize)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var jsonWriter = new JsonTextWriter(writer);
            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, await asyncEnumerable.ToArrayAsync().ConfigureAwait(false));
            jsonWriter.Flush();
            if (memoryStream.Length < chunkSize)
            {
                yield return new JournalResponse
                {
                    Chunk = memoryStream.ToArray().ToList()
                };
            }
            else
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                var buffer = new byte[chunkSize];
                int readAmount;
                while ((readAmount = await memoryStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    yield return new JournalResponse
                    {
                        Chunk = buffer.Take(readAmount).ToList()
                    };
                    buffer = new byte[chunkSize];
                }
            }
        }

        private IAsyncEnumerable<T> GetEntitiesAsync<T>(IMiddlewareRepository<T> repository, JournalRequest request)
        {
            if (request.To < 0)
            {
                return repository.GetEntriesOnOrAfterTimeStampAsync(request.From, take: (int) -request.To);
            }
            else if (request.To == 0)
            {
                return repository.GetEntriesOnOrAfterTimeStampAsync(request.From);
            }
            else
            {
                return repository.GetByTimeStampRangeAsync(request.From, request.To);
            }
        }
    }
}