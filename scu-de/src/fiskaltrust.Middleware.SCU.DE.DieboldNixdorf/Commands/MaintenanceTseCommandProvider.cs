using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class MaintenanceTseCommandProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;
        private readonly ILogger<MaintenanceTseCommandProvider> _logger;

        public MaintenanceTseCommandProvider(TseCommunicationCommandHelper tseCommunicationHelper, ILogger<MaintenanceTseCommandProvider> logger)
        {
            _tseCommunicationHelper = tseCommunicationHelper;
            _logger = logger;
        }

        public MemoryInfo GetMemoryInfo()
        {
            _tseCommunicationHelper.SetManagementClientId();

            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetMemoryInfo);
            return new MemoryInfo
            {
                Capacity = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                FreeSpace = ResponseHelper.GetResultForAsciiDigit(resultParameters[1])
            };
        }

        public DeviceInfo GetDeviceInfo()
        {
            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetDeviceInfo, parameters);
            return new DeviceInfo
            {
                SerialNo = ResponseHelper.GetResultForAsciiHexDigit(resultParameters[0]),
                NumSlots = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                NumSlotsOccupied = ResponseHelper.GetResultForAsciiDigit(resultParameters[2]),
                NumSlotsMaint = ResponseHelper.GetResultForAsciiDigit(resultParameters[3]),
                NumSlotsTseInserted = ResponseHelper.GetResultForAsciiDigit(resultParameters[4]),
                PcbVersion = ResponseHelper.GetResultForAsciiAlpha(resultParameters[5]),
                IpAddress = ResponseHelper.GetResultForAsciiPrintable(resultParameters[6])
            };
        }

        public SlotInfo GetSlotInfo()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetSlotInfo, parameters);

            if(!ResponseHelper.TryGetResultForAsciiDigit(resultParameters[4], out var capacity))
            {
                _logger.LogWarning("Could not read TSE capacity, this might be caused by an outdated TSE firmware version. Please update the device to resolve this warning.");
            }

            if (!ResponseHelper.TryGetResultForAsciiDigit(resultParameters[5], out var freeSpace))
            {
                _logger.LogWarning("Could not read free TSE space, this might be caused by an outdated TSE firmware version. Please update the device to resolve this warning.");
            }

            return new SlotInfo
            {
                SlotStatus = (SlotStates) ResponseHelper.FromAsciiHexDigitToByte(resultParameters[0]),
                TseStatus = (SlotTseStates) ResponseHelper.FromAsciiHexDigitToByte(resultParameters[1]),
                CryptoVendor = ResponseHelper.GetResultForAsciiDigit(resultParameters[2]),
                CryptoInfo = ResponseHelper.GetResultForAsciiPrintable(resultParameters[3]),
                Capacity = capacity,
                FreeSpace = freeSpace,
                CertExpDate = ResponseHelper.FromDateTime(resultParameters[6]),
                AvailSig = ResponseHelper.GetResultForAsciiDigit(resultParameters[7]),
                SigAlgorithm = ResponseHelper.GetResultForAsciiPrintable(resultParameters[8]),
                CryptoFwType = resultParameters.Count >= 10 ? ResponseHelper.GetResultForAsciiDigit(resultParameters[9]) : -1,
                TSEDescription = resultParameters.Count >= 11 ? ResponseHelper.GetResultForAsciiPrintable(resultParameters[10]) : null
            };
        }

        public SelfTestResult RunSelfTest(string clientId = null)
        {
            if (clientId == null)
            {
                _tseCommunicationHelper.SetManagementClientId();
            }
            else
            {
                _tseCommunicationHelper.SetClientId(clientId);
            }
            var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.RunSelfTest, parameters, TimeSpan.FromSeconds(120).TotalMilliseconds);
            return (SelfTestResult) ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
        }

        public void Initialize(string description = "")
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                    description
                };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.Initialize, parameters);
        }

        public void UpdateTime()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                    RequestHelper.AsDateTime(DateTime.UtcNow)
                };
            _tseCommunicationHelper.ExecuteCommandWithoutResponseIgnoreCertificateError(DieboldNixdorfCommand.UpdateTime, parameters);
        }

        public void Disable()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
                };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.Disable, parameters);
        }

        public void RegisterClient(string clientId)
        {
            _tseCommunicationHelper.SetClientId(clientId);
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
                };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.RegisterClient, parameters);
        }

        public void DeregisterClient(string clientId)
        {
            _tseCommunicationHelper.SetClientId(clientId);
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
                };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.DeregisterClient, parameters);
        }
    }
}
