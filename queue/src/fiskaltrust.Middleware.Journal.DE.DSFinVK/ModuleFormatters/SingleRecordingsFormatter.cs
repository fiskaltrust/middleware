using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters.Interfaces;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.ifPOS.v1;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters
{
    public class SingleRecordingsFormatter : IModuleFormatter
    {
        private readonly ILogger _logger;
        private readonly CsvGenerator _csvGenerator;
        private readonly BonposFactory _bonposFactory;
        private readonly BonposUstFactory _bonposUstFactory;
        private readonly BonposZusatzinfoFactory _bonposZusatzInfoFactory;
        private readonly BonkopfFactory _bonkopfFactory;
        private readonly BonkopfUstFactory _bonkopfUstFactory;
        private readonly BonkopfAbrKreisFactory _bonkopfAbrKreisFactory;
        private readonly BonkopfZahlartFactory _bonkopfZahlartFactory;
        private readonly BonReferenzFactory _bonReferenzFactory;
        private readonly TseTransaktionFactory _tseTransaktionFactory;
        private readonly Common.Helpers.ReferencesLookUpType _referencesLookUpTyp;
        private readonly bool _includeOrders;

        public SingleRecordingsFormatter(ILogger logger, DSFinVKParameters parameters)
        {
            _logger = logger;

            _csvGenerator = new CsvGenerator();
            _bonposFactory = new BonposFactory(parameters, logger);
            _bonposUstFactory = new BonposUstFactory(parameters, _logger);
            _bonposZusatzInfoFactory = new BonposZusatzinfoFactory(parameters, _logger);
            _bonkopfFactory = new BonkopfFactory(parameters);
            _bonkopfUstFactory = new BonkopfUstFactory(parameters, _logger);
            _bonkopfAbrKreisFactory = new BonkopfAbrKreisFactory(parameters);
            _bonkopfZahlartFactory = new BonkopfZahlartFactory(parameters);
            _bonReferenzFactory = new BonReferenzFactory(parameters);
            _tseTransaktionFactory = new TseTransaktionFactory(parameters);
            _referencesLookUpTyp = parameters.ReferencesLookUpType;
            _includeOrders = parameters.IncludeOrders;
        }

        public async Task ProcessAsync(string targetDirectory, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters, List<ReceiptReferencesGroupedData> receiptReferences, TseData tseData)
        {
            _logger.LogDebug($"Creating single recordings module to {targetDirectory}.");

            Directory.CreateDirectory(targetDirectory);

            var kassenbelege = GetKassenBelegReceipts(receiptEntries);
            if (_includeOrders)
            {
                kassenbelege = receiptEntries;
            }

            await CreateBonposFileAsync(targetDirectory, kassenbelege, parameters);
            await CreateBonposUstFileAsync(targetDirectory, kassenbelege, parameters);
            await CreateBonposZusatzInfoFileAsync(targetDirectory, kassenbelege, parameters);
            await CreateBonposPreisfindungFileAsync(targetDirectory);

            await CreateBonkopfFileAsync(targetDirectory, kassenbelege, parameters);
            await CreateBonkopfUstFileAsync(targetDirectory, kassenbelege, parameters);
            await CreateBonkopfZahlartFileAsync(targetDirectory, kassenbelege, parameters);

            await CreateBonreferenzenFileAsync(targetDirectory, receiptReferences);

            await CreateTseTransaktionFileAsync(targetDirectory, receiptEntries, parameters);

            await CreateBonkopfAbrKreisFileAsync(targetDirectory, receiptEntries, parameters);

            _logger.LogDebug("Finished creating single recordings module.");
        }

        private async Task CreateBonposFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = receipts.SelectMany(x => _bonposFactory.Create(x, parameters.ZNumber, parameters.DailyClosingMoment));
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "lines.csv"), records);
        }

        private async Task CreateBonposUstFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = receipts.SelectMany(x => _bonposUstFactory.Create(x, parameters.ZNumber, parameters.DailyClosingMoment));
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "lines_vat.csv"), records);
        }

        private async Task CreateBonposZusatzInfoFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = receipts.SelectMany(x => _bonposZusatzInfoFactory.Create(x, parameters.ZNumber, parameters.DailyClosingMoment));
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "subitems.csv"), records);
        }

        private async Task CreateBonposPreisfindungFileAsync(string targetDirectory)
        {
            // We do not support generating this data; hence, we generate an empty CSV file to fulfill the standard
            var emptyRecords = new List<BonposPreisfindung>();
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "itemamounts.csv"), emptyRecords);
        }

        private async Task CreateTseTransaktionFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> dailyReceipts, DailyClosingParameters parameters)
        {
            var records = new List<TseTransaktion>();
            foreach (var dailyReceipt in dailyReceipts)
            {
                records.AddRange(_tseTransaktionFactory.Create(dailyReceipt, parameters.ZNumber, parameters.DailyClosingMoment));
            }

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "transactions_tse.csv"), records);
        }

        private async Task CreateBonkopfFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = new List<Bonkopf>();

            foreach (var receipt in receipts)
            {
                var transaction = _bonkopfFactory.Create(receipt, parameters.ZNumber, parameters.DailyClosingMoment);
                if (transaction != null)
                {
                    records.Add(transaction);
                }
            }

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "transactions.csv"), records);
        }

        private async Task CreateBonkopfUstFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = new List<BonkopfUst>();

            foreach (var receipt in receipts)
            {
                records.AddRange(_bonkopfUstFactory.Create(receipt, parameters.ZNumber, parameters.DailyClosingMoment));
            }

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "transactions_vat.csv"), records);
        }

        private async Task CreateBonkopfAbrKreisFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> dailyReceipts, DailyClosingParameters parameters)
        {
            var records = new List<BonkopfAbrKreis>();

            foreach (var receipt in dailyReceipts)
            {
                records.AddRange(_bonkopfAbrKreisFactory.Create(receipt, parameters.ZNumber, parameters.DailyClosingMoment));
            }

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "allocation_groups.csv"), records);
        }

        private async Task CreateBonkopfZahlartFileAsync(string targetDirectory, IEnumerable<ReceiptEntry> receipts, DailyClosingParameters parameters)
        {
            var records = new List<BonkopfZahlart>();

            foreach (var receipt in receipts)
            {
                records.AddRange(_bonkopfZahlartFactory.Create(receipt, parameters.ZNumber, parameters.DailyClosingMoment));
            }

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "datapayment.csv"), records);
        }

        private async Task CreateBonreferenzenFileAsync(string targetDirectory, List<ReceiptReferencesGroupedData> receiptReferences)
        {
            if (_referencesLookUpTyp == Common.Helpers.ReferencesLookUpType.NoReferences)
            {
                return;
            }
            else
            {
                if (receiptReferences == null)
                {
                    return;
                }
                var records = _bonReferenzFactory.Create(receiptReferences);
                await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "references.csv"), records);
            }
        }

        private IEnumerable<ReceiptEntry> GetKassenBelegReceipts(IEnumerable<ReceiptEntry> dailyReceipts) =>
            dailyReceipts.Where(x => x.ReceiptRequest.IsReceiptProcessType());

    }
}
