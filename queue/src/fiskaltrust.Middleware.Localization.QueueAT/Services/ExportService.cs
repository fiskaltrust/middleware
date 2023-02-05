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

namespace fiskaltrust.Middleware.Localization.QueueAT.Services
{
    public class ExportService
    {
        private readonly IReadOnlyConfigurationRepository _configurationRepository;
        private readonly IReadOnlyJournalATRepository _journalATRepository;
        private readonly ILogger<ExportService> _logger;

        public ExportService(IReadOnlyConfigurationRepository configurationRepository, IReadOnlyJournalATRepository readOnlyJournalATRepository, ILogger<ExportService> logger)
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

                // TODO: Clarify why we're not just adding the receipts to the respective SCUs
                foreach (var queue in await _configurationRepository.GetQueueATListAsync())
                {
                    // TODO: Implement DB calls to increase performance
                    var journals = (await _journalATRepository.GetAsync())
                        .Where(x => x.TimeStamp >= fromTimestamp && x.TimeStamp <= toTimestamp);
                    var sscdIds = journals
                        .Select(j => j.ftSignaturCreationUnitId)
                        .Distinct()
                        .Where(x => x != Guid.Empty)
                        .ToList();
                    var receipts = journals
                        .OrderBy(x => x.Number)
                        .Select(x => $"{x.JWSHeaderBase64url}.{x.JWSPayloadBase64url}.{x.JWSSignatureBase64url}");

                    if (sscdIds.Count == 1)
                    {
                        var scu = await _configurationRepository.GetSignaturCreationUnitATAsync(sscdIds[0]);
                                                
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
