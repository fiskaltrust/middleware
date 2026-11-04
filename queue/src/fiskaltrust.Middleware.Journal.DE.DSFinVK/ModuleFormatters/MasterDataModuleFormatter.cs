using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters.Interfaces;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters
{
    public class MasterDataModuleFormatter : IModuleFormatter
    {
        private readonly ILogger _logger;
        private readonly CsvGenerator _csvGenerator;
        private readonly StammAbschlussFactory _stammAbschlussFactory;
        private readonly StammOrteFactory _stammOrteFactory;
        private readonly StammKassenFactory _stammKassenFactory;
        private readonly StammAgenturenFactory _stammAgenturenFactory;
        private readonly StammUstFactory _stammUstFactory;
        private readonly StammTSEFactory _stammTSEFactory;
        private readonly MasterDataConfiguration _fallbackMasterData;

        public MasterDataModuleFormatter(ILogger logger, DSFinVKParameters parameters, MasterDataConfiguration fallbackMasterData)
        {
            _logger = logger;
            _csvGenerator = new CsvGenerator();
            _stammAbschlussFactory = new StammAbschlussFactory(parameters);
            _stammOrteFactory = new StammOrteFactory(parameters);
            _stammKassenFactory = new StammKassenFactory(parameters, _logger);
            _stammAgenturenFactory = new StammAgenturenFactory(parameters);
            _stammUstFactory = new StammUstFactory(parameters, _logger);
            _stammTSEFactory = new StammTSEFactory(parameters);
            _fallbackMasterData = fallbackMasterData;
        }

        public async Task ProcessAsync(string targetDirectory, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters, List<ReceiptReferencesGroupedData> receiptReferences = null, TseData tseData = null)
        {
            _logger.LogDebug("Creating master data module.");

            var queueItems = receiptEntries.Select(x => x.QueueItem);
            var dailyClosingResponse = receiptEntries.Last().ReceiptResponse;
            var masterData = SerializationHelper.GetStateData(dailyClosingResponse)?.MasterData;
            masterData = EnrichWithFallbackMasterData(masterData);

            var cashboxIdentification = dailyClosingResponse.ftSignatures?.FirstOrDefault(x => x.ftSignatureType == (long) SignatureTypesDE.CashBoxIdentification)?.Data;

            Directory.CreateDirectory(targetDirectory);
            await CreateStammAbschlussFileAsync(targetDirectory, masterData, receiptEntries, parameters);
            await CreateStammOrteFileAsync(targetDirectory, masterData, parameters);
            await CreateStammKassenFileAsync(targetDirectory, masterData, receiptEntries, parameters);
            await CreateStammTerminalsFileAsync(targetDirectory);
            await CreateStammAgenturenFileAsync(targetDirectory, masterData, parameters);
            await CreateStammUstFileAsync(targetDirectory, receiptEntries, parameters);
            await CreateStammTSEFileAsync(targetDirectory, dailyClosingResponse, parameters, tseData);

            _logger.LogDebug("Finished creating master data module.");
        }

        private MasterDataConfiguration EnrichWithFallbackMasterData(MasterDataConfiguration masterData)
        {
            if (masterData?.Account == null && _fallbackMasterData != null)
            {
                _logger.LogWarning("No master data detected in daily closing receipt, using fallback data from configuration.");
                masterData = _fallbackMasterData;
            }
            if (masterData?.Account != null && string.IsNullOrEmpty(masterData.Account.TaxId) && !string.IsNullOrEmpty(_fallbackMasterData?.Account?.TaxId))
            {
                _logger.LogWarning("No tax ID was set in the daily closing receipt, using fallback data from configuration.");
                masterData.Account.TaxId = _fallbackMasterData.Account.TaxId;
            }
            if (masterData?.Account != null && string.IsNullOrEmpty(masterData.Account.VatId) && !string.IsNullOrEmpty(_fallbackMasterData?.Account?.VatId))
            {
                _logger.LogWarning("No VAT ID was set in the daily closing receipt, using fallback data from configuration.");
                masterData.Account.VatId = _fallbackMasterData.Account.VatId;
            }

            if ((masterData?.PosSystems == null || !masterData.PosSystems.Any()) && _fallbackMasterData?.PosSystems != null && _fallbackMasterData.PosSystems.Any())
            {
                _logger.LogWarning("No POS systems detected in daily closing receipt, using fallback data from configuration.");
                masterData.PosSystems = _fallbackMasterData.PosSystems;
            }

            return masterData;
        }

        private async Task CreateStammAbschlussFileAsync(string targetDirectory, MasterDataConfiguration masterData, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters)
        {
            var record = _stammAbschlussFactory.Create(masterData, receiptEntries, parameters.ZNumber, parameters.DailyClosingMoment);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "cashpointclosing.csv"), new[] { record });
        }

        private async Task CreateStammOrteFileAsync(string targetDirectory, MasterDataConfiguration masterData, DailyClosingParameters parameters)
        {
            var record = _stammOrteFactory.Create(masterData, parameters.ZNumber, parameters.DailyClosingMoment);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "location.csv"), new[] { record });
        }

        private async Task CreateStammKassenFileAsync(string targetDirectory, MasterDataConfiguration masterData, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters)
        {
            var records = _stammKassenFactory.Create(masterData, receiptEntries, parameters.ZNumber, parameters.DailyClosingMoment);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "cashregister.csv"), records);
        }

        private async Task CreateStammTerminalsFileAsync(string targetDirectory)
        {
            // We do not support generating this data; hence, we generate an empty CSV file to fulfill the standard
            // In DSFinV-K 2.3, terminals are defined in a way that  sounds like they need to have separate client IDs.
            // As this is not the case in our architecture, we're treating POS systems as separate cash registers only now.
            var emptyRecords = new List<StammTerminal>();
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "slaves.csv"), emptyRecords);
        }

        private async Task CreateStammAgenturenFileAsync(string targetDirectory, MasterDataConfiguration masterData, DailyClosingParameters parameters)
        {
            var records = _stammAgenturenFactory.Create(masterData, parameters.ZNumber, parameters.DailyClosingMoment);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "pa.csv"), records);
        }

        private async Task CreateStammUstFileAsync(string targetDirectory, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters)
        {
            var records = _stammUstFactory.Create(receiptEntries, parameters.ZNumber, parameters.DailyClosingMoment);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "vat.csv"), records);
        }

        private async Task CreateStammTSEFileAsync(string targetDirectory, ReceiptResponse dailyClosingResponse, DailyClosingParameters parameters, TseData tseData)
        {
            var record = _stammTSEFactory.Create(dailyClosingResponse, parameters.ZNumber, parameters.DailyClosingMoment, tseData);
            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "tse.csv"), new[] { record });
        }
    }
}
