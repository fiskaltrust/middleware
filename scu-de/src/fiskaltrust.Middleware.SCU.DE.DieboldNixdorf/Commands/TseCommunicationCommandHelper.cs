using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class TseCommunicationCommandHelper
    {
        public const string ManagementClientId = "fiskaltrust.Middleware";
        public const string DieboldNixdorfDefaultClientId = "DN TSEProduction ef82abcedf";

        private string _lastSetClientId = null;

        private readonly ILogger<TseCommunicationCommandHelper> _logger;
        private readonly ISerialCommunicationQueue _serialPortCommunicationProvider;

        public int SlotNumber { get; }

        public bool NeedsSelfTest => _serialPortCommunicationProvider.SerialPortDeviceUnavailable;

        public bool TseConnected => _serialPortCommunicationProvider.DeviceConnected;

        public TseCommunicationCommandHelper(ILogger<TseCommunicationCommandHelper> logger, ISerialCommunicationQueue serialPortCommunicationProvider, int slotNumber)
        {
            _logger = logger;
            _serialPortCommunicationProvider = serialPortCommunicationProvider;
            SlotNumber = slotNumber;
        }

        public MfcStatus GetMfcStatus()
        {
            var resultParameters = ExecuteCommandWithResponse(DieboldNixdorfCommand.GetMfcStatus, timeoutMs: 1000);

            if (resultParameters.Count > 2)
            {
                _logger.LogWarning("More than two results were returned by GetMfcStatus ({GetMfcStatusReturnCount}). This might occur when a previous call was not executed succesfully.", resultParameters.Count);
            }

            return new MfcStatus
            {
                MfcError = ResponseHelper.FromAsciiHexDigitToBytes(resultParameters[resultParameters.Count - 2]),
                MfcState = (MfcStates) ResponseHelper.FromAsciiHexDigitToByte(resultParameters[resultParameters.Count - 1])
            };
        }

        public CommandResponse GetCommandResponse(int bufferNumber, int packageSequenceNumber, double timeoutMs = 2000)
        {
            var parameters = new List<string> {
                RequestHelper.GetParameterValueAsAsciiDigit(bufferNumber),
                RequestHelper.GetParameterValueAsAsciiDigit(packageSequenceNumber)
            };

            var resultParameters = ExecuteCommandWithResponse(DieboldNixdorfCommand.GetCommandResponse, parameters, timeoutMs);
            var response = new CommandResponse
            {
                BufferNo = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                PacketSeqNo = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                BufferStatus = ResponseHelper.GetResultForAsciiHexDigit(resultParameters[2]),
                BufferDataSize = ResponseHelper.GetResultForAsciiDigit(resultParameters[3]),
                BufferData = resultParameters[4].ToArray()
            };
            return response;
        }

        public void SetManagementClientId() => SetClientId(ManagementClientId);

        public void SetClientId(string clientId)
        {
            if (_lastSetClientId == clientId)
            {
                return;
            }
            try
            {
                _lastSetClientId = clientId;
                var parameters = new List<string> {
                    RequestHelper.AsAsn1Printable(clientId),
                };
                ExecuteCommandWithoutResponse(DieboldNixdorfCommand.SetClientId, parameters);
            }
            catch (Exception)
            {
                _lastSetClientId = null;
                throw;
            }
        }

        public void ExecuteCommandWithoutResponseIgnoreCertificateError(DieboldNixdorfCommand command, List<string> parameters = null, double timeoutMs = 2000)
        {
            try
            {
                (var buffer, var requestId) = RequestHelpers.BuildRequest(command, parameters);
                var tseResults = _serialPortCommunicationProvider.SendCommandWithResult(buffer, requestId, command, timeoutMs);
                ErrorHandler.ThrowExceptionForMfcState(GetMfcStatus().MfcError);
            }
            catch (DieboldNixdorfException exception) when (exception.Message == "E_CERTIFICATE_EXPIRED")
            {
                _logger.LogError(exception, "The certificate is expired. We still are able to login, but we should replace the certificate.");
            }
            catch (NoResponseException)
            {
                try
                {
                    var mfcStatus = GetMfcStatus();
                    ErrorHandler.ThrowExceptionForMfcState(mfcStatus.MfcError);
                    throw;
                }
                catch (DieboldNixdorfException exception) when (exception.Message == "E_CERTIFICATE_EXPIRED")
                {
                    _logger.LogError(exception, "The certificate is expired. We still are able to login, but we should replace the certificate.");
                }
            }
        }

        public void ExecuteCommandWithoutResponse(DieboldNixdorfCommand command, List<string> parameters = null, double timeoutMs = 2000)
        {
            try
            {
                (var buffer, var requestId) = RequestHelpers.BuildRequest(command, parameters);
                var tseResults = _serialPortCommunicationProvider.SendCommandWithResult(buffer, requestId, command, timeoutMs);
            }
            catch (NoResponseException)
            {
                if (command != DieboldNixdorfCommand.GetMfcStatus)
                {
                    ErrorHandler.ThrowExceptionForMfcState(GetMfcStatus().MfcError);
                }
                throw;
            }
        }

        public Guid ExecuteCommandWithBatchResponse(DieboldNixdorfCommand command, int bufferIdentifier, List<string> parameters = null)
        {
            (var buffer, var requestId) = RequestHelpers.BuildRequestWithGetCommandResponseAnswer(command, bufferIdentifier, parameters);
            try
            {
                _serialPortCommunicationProvider.SendCommand(buffer, command, requestId);
                return requestId;
            }
            catch (NoResponseException)
            {
                ErrorHandler.ThrowExceptionForMfcState(GetMfcStatus().MfcError);
                throw;
            }
        }

        public Dictionary<int, List<byte>> ExecuteCommandWithResponse(DieboldNixdorfCommand command, List<string> parameters = null, double timeoutMs = 2000)
        {
            (var buffer, var requestId) = RequestHelpers.BuildRequest(command, parameters);
            try
            {
                var tseResults = _serialPortCommunicationProvider.SendCommandWithResult(buffer, requestId, command, timeoutMs);
                return tseResults.Parameters.ToDictionary(x => tseResults.Parameters.IndexOf(x), x => x);
            }
            catch (NoResponseException)
            {
                if (command != DieboldNixdorfCommand.GetMfcStatus)
                {
                    ErrorHandler.ThrowExceptionForMfcState(GetMfcStatus().MfcError);
                }
                throw;
            }
        }

        public List<List<byte>> LoadResponse(Guid requestId, int bufferIdentifier, double timeoutMs = 2000)
        {
            var responseData = new List<CommandResponse>();
            CommandResponse response = null;
            var i = 0;
            do
            {
                response = GetCommandResponse(bufferIdentifier, i, timeoutMs);
                i++;
                if (i > 255)
                {
                    i = 0;
                }
                responseData.Add(response);
            } while (response.BufferStatus == "00");

            var tseresult = TseResultHelper.CreateTseResult(requestId, responseData.SelectMany(x => x.BufferData).ToArray());
            return tseresult.Parameters.ToList();
        }
    }
}
