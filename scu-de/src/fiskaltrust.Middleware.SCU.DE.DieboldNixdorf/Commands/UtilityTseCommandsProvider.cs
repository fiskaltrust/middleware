using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class UtilityTseCommandsProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;

        public UtilityTseCommandsProvider(TseCommunicationCommandHelper tseCommunicationHelper) => _tseCommunicationHelper = tseCommunicationHelper;

        public NumberOfClientsResult GetNumberOfClients()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetNumberOfClients);
            return new NumberOfClientsResult
            {
                MaxNumClients = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                CurrentNumClients = ResponseHelper.GetResultForAsciiDigit(resultParameters[1])
            };
        }

        public NumberOfTransactionsResult GetNumberOfTransactions()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetNumberOfTransactions);
            return new NumberOfTransactionsResult
            {
                MaxNumTransactions = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                CurrentNumTransactions = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
            };
        }

        public List<long> GetStartedTransactions(string clientId = null)
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string>();
            if (clientId != null)
            {
                parameters.Add(RequestHelper.AsAsn1Printable(clientId));
            }
            var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.GetStartedTransactions, 2, parameters);
            var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 2, 10000);
            return resultParameters.Select(value => ResponseHelper.GetResultForAsciiDigit(value)).ToList();
        }

        public List<string> GetRegisteredClients()
        {
            _tseCommunicationHelper.SetManagementClientId();

            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
            var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.GetRegisteredClients, 2, parameters);
            var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 2);
            return resultParameters.Select(value => ResponseHelper.GetResultForAsciiPrintable(value)).ToList();
        }

        public long GetUpdateTimeInterval()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetUpdateTimeInterval);
            return ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
        }

        public long GetTimeUntilNextSelfTest()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetTimeUntilNextSelfTest);
            return ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
        }
    }
}
