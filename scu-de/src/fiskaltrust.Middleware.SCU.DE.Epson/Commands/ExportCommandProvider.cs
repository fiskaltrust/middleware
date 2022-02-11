using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class ExportCommandProvider
    {
        private readonly OperationalCommandProvider _operationalCommandProvider;

        public ExportCommandProvider(OperationalCommandProvider operationalCommandProvider)
        {
            _operationalCommandProvider = operationalCommandProvider;
        }

        public async Task<ArchiveExportResult> ArchiveExportAsync(string clientId = "") => await _operationalCommandProvider.ExecuteRequestAsync<ArchiveExportResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Export.ArchiveExport,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId }
            }
        });

        public async Task<ArchiveExportResult> ExportFilteredByTransactionNumberAsync(int transactionNumber, string clientId = "") => await _operationalCommandProvider.ExecuteRequestAsync<ArchiveExportResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Export.ExportFilteredByTransactionNumber,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId },
                {"transactionNumber", transactionNumber },
            }
        });

        public async Task<ArchiveExportResult> ExportFilteredByTransactionNumberIntervalAsync(ulong startTransactionNumber, ulong endTransactionNumber, string clientId = "") => await _operationalCommandProvider.ExecuteRequestAsync<ArchiveExportResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Export.ExportFilteredByTransactionNumberInterval,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId },
                {"startTransactionNumber", startTransactionNumber },
                {"endTransactionNumber", endTransactionNumber }
            }
        });

        public async Task<ArchiveExportResult> ExportFilteredByPeriodOfTimeAsync(DateTime startDate, DateTime endDate, string clientId = "") => await _operationalCommandProvider.ExecuteRequestAsync<ArchiveExportResult>(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Export.ExportFilteredByPeriodOfTime,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId },
                {"startDate", startDate.ToString("yyyy-MM-ddThh:mm:ssZ") },
                {"endDate", endDate.ToString("yyyy-MM-ddThh:mm:ssZ") }
            }
        });

        public async Task<OutputGetExportDataResult> GetExportDataAsync() => await _operationalCommandProvider.ExecuteRequestAsync<OutputGetExportDataResult>(new EpsonTSEJsonCommand(Constants.Functions.Export.GetExportData));

        public async Task FinalizeExportAsync(bool deleteData) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Export.FinalizeExport,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"deleteData", deleteData }
            }
        });

        public async Task CancelExportAsync() => await _operationalCommandProvider.ExecuteRequestAsync<AuthenticateUserForAdminResult>(new EpsonTSEJsonCommand(Constants.Functions.Export.CancelExport));

        public async Task<GetLogMessageCertificate> GetLogMessageCertificateAsync() => await _operationalCommandProvider.ExecuteRequestAsync<GetLogMessageCertificate>(new EpsonTSEJsonCommand(Constants.Functions.Export.GetLogMessageCertificate));
    }
}
