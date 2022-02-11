using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class ExportTseCommandsProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;

        public ExportTseCommandsProvider(TseCommunicationCommandHelper tseCommunicationHelper)
        {
            _tseCommunicationHelper = tseCommunicationHelper;
        }

        public List<string> ExportCertificatesAsBase64()
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();
                var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportCertificates, 1, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        public List<string> ExportSerialNumbers()
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();
                var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
            };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportSerialNumbers, 1, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        public List<string> ExportAll()
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();

                var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
                };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportAll, 1, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        public List<string> ExportByTransactionNo(ulong transactionNumber, string clientId)
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();
                var parameters = new List<string> {
                        RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                        RequestHelper.AsAsciiDigit((long)transactionNumber).PadLeft(10, '0'),
                        RequestHelper.AsAsn1Printable(clientId ?? string.Empty)
                    };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportByTransactionNo, (int) transactionNumber, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        public List<string> ExportByTransactionNoInterval(ulong startTransactionNumber, ulong endTransactionNumber, string clientId)
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();
                var parameters = new List<string> {
                        RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                        RequestHelper.AsAsciiDigit((long)startTransactionNumber).PadLeft(10, '0'),
                        RequestHelper.AsAsciiDigit((long)endTransactionNumber).PadLeft(10, '0'),
                        RequestHelper.AsAsn1Printable(clientId ?? string.Empty)
                    };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportByTransactionNoInterval, 1, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        public List<string> ExportByTimePeriod(DateTime startDateTime, DateTime endDateTime, string clientId)
        {
            try
            {
                _tseCommunicationHelper.SetManagementClientId();
                var parameters = new List<string> {
                        RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                        RequestHelper.AsDateTime(startDateTime),
                        RequestHelper.AsDateTime(endDateTime),
                        RequestHelper.AsAsn1Printable(clientId ?? string.Empty)
                    };
                var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportByTimePeriod, 1, parameters);
                var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
                return resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            }
            finally
            {
                // 2020-10-06 Stefan Kert: We reset the exportbuffer as soon as export is done because otherwise we probably run into issues
                ResetExport();
            }
        }

        // 2020-10-06 Stefan Kert: This Command is not documented, but used by the Webservice. It looks like it can be used for reseting the export if it crashed and brought the TSE into an invalid state
        // Bytes Written:  1b 90 33 30 39 1b 92 31 1b 9d 1b 9f  
        // Bytes Returned: 1b 91 33 30 39 1b 99 1b 9f
        // We are ignoring potential issues, because it is possible, that this functionality is removed in prod mode
        public void ResetExport()
        {
            try
            {
                var parameters = new List<string> {
                RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber) // I guess that this is the slotnumber.. not sure if that is true
            };
                _ = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.ExportWeird_NoClueWhatThisIsUnDocumented1, parameters);
                return;
            }
            catch { }
        }

        public long DeleteExportedData(long exportSize)
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber),
                    RequestHelper.AsAsciiDigit(exportSize)
                };
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.DeleteExportedData, parameters);
            return ResponseHelper.GetResultForAsciiDigit(resultParameters[0]);
        }

        public string ExportPublicKey()
        {
            _tseCommunicationHelper.SetManagementClientId();
            var parameters = new List<string> {
                    RequestHelper.GetParameterForSlotNumber(_tseCommunicationHelper.SlotNumber)
                };
            var requestId = _tseCommunicationHelper.ExecuteCommandWithBatchResponse(DieboldNixdorfCommand.ExportPublicKey, 1, parameters);
            var resultParameters = _tseCommunicationHelper.LoadResponse(requestId, 1);
            var results = resultParameters.Select(value => ResponseHelper.GetResultForBase64(value)).ToList();
            return results[1];
        }
    }
}