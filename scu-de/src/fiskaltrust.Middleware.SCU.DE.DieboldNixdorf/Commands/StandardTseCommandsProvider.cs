using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class StandardTseCommandsProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;

        public StandardTseCommandsProvider(TseCommunicationCommandHelper tseCommunicationHelper) => _tseCommunicationHelper = tseCommunicationHelper;

        public CountryInfo GetCountryInfo()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetCountryInfo);
            return new CountryInfo
            {
                ApiMajorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                ApiMinorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                CountryId = ResponseHelper.GetResultForAsciiDigit(resultParameters[2]),
                HardwareId = (DieboldNixdorfHardwareId) ResponseHelper.GetResultForAsciiDigit(resultParameters[3])
            };
        }

        public SingleTSEFirmwareInfo GetFirmwareInfoForSingleTSE()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetFirmwareInfo);
            return new SingleTSEFirmwareInfo
            {
                FwMajorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                FwMinorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                FwBuildNo = ResponseHelper.GetResultForAsciiDigit(resultParameters[2]),
                LdrMajorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[3]),
                LdrMinorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[4]),
                LdrBuildNo = ResponseHelper.GetResultForAsciiDigit(resultParameters[5])
            };
        }

        public ConnectBoxFirmwareInfo GetFirmwareInfoForConnectBox()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.GetFirmwareInfo);
            return new ConnectBoxFirmwareInfo
            {
                AppMajorVersion  = ResponseHelper.GetResultForAsciiDigit(resultParameters[0]),
                AppMinorVersion  = ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                AppBuildNo  = ResponseHelper.GetResultForAsciiDigit(resultParameters[2]),
                OsUpdMajorVersion  = ResponseHelper.GetResultForAsciiDigit(resultParameters[3]),
                OsUpdMinorVersion  = ResponseHelper.GetResultForAsciiDigit(resultParameters[4]),
                OsUpdBuildVersion  = ResponseHelper.GetResultForAsciiDigit(resultParameters[5]),
                OsMainMajorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[6]),
                OsMainMinorVersion = ResponseHelper.GetResultForAsciiDigit(resultParameters[7]),
                OsMainBuildNo = ResponseHelper.GetResultForAsciiDigit(resultParameters[8])
            };
        }

        public void DisableAsb()
        {
            var parameters = new List<string> {
                RequestHelper.GetParameterValueAsAsciiDigit(false)
            };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.SetAsb, parameters);
        }
    }
}