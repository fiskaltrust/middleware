using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters.Interfaces;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters
{
    public class CashpointClosingFormatter : IModuleFormatter
    {
        private readonly ILogger _logger;
        private readonly CsvGenerator _csvGenerator;
        private readonly ZGVTypFactory _gvTypFactory;
        private readonly ZZahlartFactory _zahlartFactory;
        private readonly ZWaehrungenFactory _waehrungenFactory;

        public CashpointClosingFormatter(ILogger logger, DSFinVKParameters parameters)
        {
            _logger = logger;

            _csvGenerator = new CsvGenerator();
            _gvTypFactory = new ZGVTypFactory(parameters, _logger);
            _zahlartFactory = new ZZahlartFactory(parameters);
            _waehrungenFactory = new ZWaehrungenFactory(parameters);
        }

        public async Task ProcessAsync(string targetDirectory, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters, List<ReceiptReferencesGroupedData> receiptReferences, TseData tseData)
        {
            _logger.LogDebug($"Creating cashpoint closing module to {targetDirectory}.");

            var requests = receiptEntries
                .Select(x => x.ReceiptRequest)
                .Where(x => x.GetBonType() == DSFinVKConstants.BonTypes.Receipt);

            await CreateBusinessCasesFileAsync(targetDirectory, requests, parameters);
            await CreatePaymentFileAsync(targetDirectory, requests, parameters);
            await CreateCashPerCurrencyFileAsync(targetDirectory, requests, parameters);

            _logger.LogDebug("Finished creating cashpoint closing module.");
        }

        private async Task CreateBusinessCasesFileAsync(string targetDirectory, IEnumerable<ReceiptRequest> requests, DailyClosingParameters parameters)
        {
            var records = _gvTypFactory.Create(requests, parameters.ZNumber, parameters.DailyClosingMoment);

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "businesscases.csv"), records);
        }

        private async Task CreatePaymentFileAsync(string targetDirectory, IEnumerable<ReceiptRequest> requests, DailyClosingParameters parameters)
        {
            var records = _zahlartFactory.Create(requests, parameters.ZNumber, parameters.DailyClosingMoment);

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "payment.csv"), records);
        }

        private async Task CreateCashPerCurrencyFileAsync(string targetDirectory, IEnumerable<ReceiptRequest> requests, DailyClosingParameters parameters)
        {
            var records = _waehrungenFactory.Create(requests, parameters.ZNumber, parameters.DailyClosingMoment);

            await _csvGenerator.WriteAsync(Path.Combine(targetDirectory, "cash_per_currency.csv"), records);
        }
    }
}
