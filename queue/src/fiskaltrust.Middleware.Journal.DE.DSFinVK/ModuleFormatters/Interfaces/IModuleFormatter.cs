using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters.Interfaces
{
    public interface IModuleFormatter
    {
        Task ProcessAsync(string targetDirectory, IList<ReceiptEntry> receiptEntries, DailyClosingParameters parameters, List<ReceiptReferencesGroupedData> receiptReferences, TseData tseData);
    }
}
