using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public class ExportService : IExportService
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IMiddlewareRepository<ftJournalAT> _journalATRepository;
        private readonly ILogger<ExportService> _logger;

        public ExportService(IReadOnlyConfigurationRepository configurationRepository, IMiddlewareRepository<ftJournalAT> readOnlyJournalATRepository, ILogger<ExportService> logger)
        {
            _configurationRepository = configurationRepository;
            _journalATRepository = readOnlyJournalATRepository;
            _logger = logger;
        }

        public async Task PerformRksvJournalExportAsync(long fromTimestamp, long toTimestamp, string targetFilePath)
        {
            try
            {
                var receiptGroups = new RksvDepExport
                {
                    ReceiptGroups = new List<RksvDepReceiptGroup>()
                };
                var sscdIds = new HashSet<Guid>();
                var receipts = new List<string>();

                var journals = _journalATRepository.GetByTimeStampRangeAsync(fromTimestamp, toTimestamp);
                await foreach (var journal in journals.OrderBy(x => x.Number))
                {
                    if (journal.ftSignaturCreationUnitId != Guid.Empty)
                    {
                        sscdIds.Add(journal.ftSignaturCreationUnitId);
                    }
                    receipts.Add($"{journal.JWSHeaderBase64url}.{journal.JWSPayloadBase64url}.{journal.JWSSignatureBase64url}");
                }
                foreach (var queue in await _configurationRepository.GetQueueATListAsync())
                {
                    if (sscdIds.Count == 1)
                    {
                        var scu = await _configurationRepository.GetSignaturCreationUnitATAsync(sscdIds.First());
                        receiptGroups.ReceiptGroups.Add(new RksvDepReceiptGroup
                        {
                            CashboxIdentification = queue.CashBoxIdentification,
                            Certificate = scu.CertificateBase64,
                            CertificateAuthorities = Array.Empty<string>(),
                            Receipts = receipts
                        });
                    }
                    else
                    {
                        receiptGroups.ReceiptGroups.Add(new RksvDepReceiptGroup
                        {
                            CashboxIdentification = queue.CashBoxIdentification,
                            Certificate = null,
                            CertificateAuthorities = Array.Empty<string>(),
                            Receipts = receipts
                        });
                        var scus = (await _configurationRepository.GetSignaturCreationUnitATListAsync())
                            .Where(s => sscdIds.Contains(s.ftSignaturCreationUnitATId) && !string.IsNullOrEmpty(s.CertificateBase64));

                        foreach (var scu in scus)
                        {
                            receiptGroups.ReceiptGroups.Add(new RksvDepReceiptGroup 
                            {
                                CashboxIdentification = queue.CashBoxIdentification,
                                Certificate = scu.CertificateBase64,
                                CertificateAuthorities = Array.Empty<string>(),
                                Receipts = Array.Empty<string>()
                            });
                        }
                    }
                }

                using (var file = File.CreateText(targetFilePath))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, receiptGroups);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export RKSV Journal.");
            }
        }
    }
}
