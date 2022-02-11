using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class ConfigurationCommandProvider
    {
        private readonly OperationalCommandProvider _operationalCommandProvider;

        public ConfigurationCommandProvider(OperationalCommandProvider operationalCommandProvider)
        {
            _operationalCommandProvider = operationalCommandProvider;
        }

        public async Task SetUpAsync(string puk, string adminPin, string timeAdminPin) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Configuraton.SetUp,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"puk", puk },
                {"adminPin", adminPin },
                {"timeAdminPin", timeAdminPin }
            }
        });

        public async Task SetUpForPrinterAsync() => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand(Constants.Functions.Configuraton.SetUpForPrinter));

        public async Task RunTSESelfTestAsync() => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand(Constants.Functions.Configuraton.RunTSESelfTest));

        public async Task UpdateTimeAsync(string userId, bool useTimeSync) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Configuraton.UpdateTime,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"userId", userId },
                {"newDateTime", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ") },
                {"useTimeSync", useTimeSync }
            }
        });

        public async Task UpdateTimeForFirstAsync(string userId, DateTime dateTime, bool useTimeSync) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Configuraton.UpdateTimeForFirst,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"userId", userId },
                {"newDateTime", dateTime.ToString("yyyy-MM-ddThh:mm:ssZ") },
                {"useTimeSync", useTimeSync }
            }
        });

        public async Task RegisterClientAsync(string clientId) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Configuraton.RegisterClient,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId }
            }
        });

        public async Task DeregisterClientAsync(string clientId) => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand
        {
            Function = Constants.Functions.Configuraton.DeregisterClient,
            Storage = StoragePayload.CreateTSE(),
            Input = new Dictionary<string, object>
            {
                {"clientId", clientId }
            }
        });

        public async Task<OutputGetRegisteredClientListResult> GetRegisteredClientsAsnc() => await _operationalCommandProvider.ExecuteRequestAsync<OutputGetRegisteredClientListResult>(new EpsonTSEJsonCommand(Constants.Functions.Configuraton.GetRegisteredClientList));

        // TODO
        // RegisterSecretKey
        // UnlockTSE
        // LockTSE
        // SetTimeOutInterval
        // GetTimeOutInterval
        // EnableExportIfCspTestFails
        // DisableExportIfCspTestFails

        public async Task DisableAsync() => await _operationalCommandProvider.ExecuteRequestAsync(new EpsonTSEJsonCommand(Constants.Functions.Configuraton.DisableSecureElement));
    }
}
