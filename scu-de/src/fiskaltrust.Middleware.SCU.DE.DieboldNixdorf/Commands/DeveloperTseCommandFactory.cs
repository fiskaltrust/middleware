using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class DeveloperTseCommandsProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;

        public DeveloperTseCommandsProvider(TseCommunicationCommandHelper tseCommunicationHelper)
        {
            _tseCommunicationHelper = tseCommunicationHelper;
        }

        public void FactoryReset()
        {
            _tseCommunicationHelper.SetManagementClientId();
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.FactoryReset, timeoutMs: TimeSpan.FromSeconds(120).TotalMilliseconds);
        }
    }
}
