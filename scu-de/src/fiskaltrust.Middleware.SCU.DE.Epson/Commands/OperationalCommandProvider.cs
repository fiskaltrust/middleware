using System;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.Communication;
using fiskaltrust.Middleware.SCU.DE.Epson.Exceptions;
using fiskaltrust.Middleware.SCU.DE.Epson.Models;
using fiskaltrust.Middleware.SCU.DE.Epson.ResultModels;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Helpers;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Commands
{
    public class OperationalCommandProvider
    {
        private readonly TcpCommunicationQueue _communicationQueue;
        private string _vendor;

        protected EpsonConfiguration Configuration { get; }

        public bool IsConnected => _communicationQueue.IsConnected;

        public OperationalCommandProvider(TcpCommunicationQueue communicationQueue, EpsonConfiguration configuration)
        {
            _communicationQueue = communicationQueue;
            Configuration = configuration;
        }

        public async Task OpenDeviceAsync(bool tryClose = true)
        {
            var command = $"<open_device><device_id>{Configuration.DeviceId}</device_id><data><type>{Configuration.StorageType}</type></data></open_device>\"\0\"";
            var payload = Encoding.UTF8.GetBytes(command);
            var result = await _communicationQueue.SendCommandWithResultAsync(payload);
            var resultData = EpsonUtilities.GetXmlFieldValue(result, "code");
            if (!resultData.Equals("OK", StringComparison.InvariantCulture))
            {
                if (tryClose)
                {
                    try
                    {
                        await CloseDeviceAsync();
                    }
                    catch { }
                    await OpenDeviceAsync();
                }
                else
                {
                    EnsureNoProtocolError(result);
                }
            }
            else
            {
                var storageInformation = await GetStorageInformationAsync();
                _vendor = storageInformation.TseInformation.VendorType;
            }
        }

        public async Task CloseDeviceAsync()
        {
            var command = $"<close_device><device_id>{Configuration.DeviceId}</device_id></close_device>\"\0\"";
            var payload = Encoding.UTF8.GetBytes(command);
            var result = await _communicationQueue.SendCommandWithResultAsync(payload);
            var resultData = EpsonUtilities.GetXmlFieldValue(result, "code");
            if (!resultData.Equals("OK", StringComparison.InvariantCulture) && !resultData.Equals("DEVICE_NOT_OPEN", StringComparison.InvariantCulture))
            {
                throw new EpsonException(resultData);
            }
        }

        public async Task<StorageInfoResult> GetStorageInformationAsync() => await ExecuteRequestAsync<StorageInfoResult>(new EpsonTSEJsonCommand(Constants.Functions.Information.GetStorageInfo, StoragePayload.CreateCommon()));

        public async Task<T> ExecuteRequestAsync<T>(EpsonTSEJsonCommand requestData)
        {
            requestData.Storage.Vendor = _vendor;
            var requestDataEscaped = EpsonUtilities.EscapeJsonString(requestData);
            var command = $"<device_data><device_id>{Configuration.DeviceId}</device_id><data><type>operate</type><timeout>{Configuration.Timeout}</timeout><requestdata>{requestDataEscaped}</requestdata></data></device_data>\"\0\"";
            var payload = Encoding.UTF8.GetBytes(command);
            var result = await _communicationQueue.SendCommandWithResultAsync(payload);
            EnsureNoProtocolError(result);
            var resultData = EpsonUtilities.GetXmlFieldValue(result, "resultdata");
            var resultPayload = JsonConvert.DeserializeObject<EpsonResultPayload<T>>(resultData);
            if (!resultPayload.Result.Equals("EXECUTION_OK", StringComparison.InvariantCulture))
            {
                throw new EpsonException(resultPayload.Result);
            }
            return resultPayload.Output;
        }

        public async Task ExecuteRequestAsync(EpsonTSEJsonCommand requestData)
        {
            requestData.Storage.Vendor = _vendor;
            var requestDataEscaped = EpsonUtilities.EscapeJsonString(requestData);
            var command = $"<device_data><device_id>{Configuration.DeviceId}</device_id><data><type>operate</type><timeout>{Configuration.Timeout}</timeout><requestdata>{requestDataEscaped}</requestdata></data></device_data>\"\0\"";
            var payload = Encoding.UTF8.GetBytes(command);
            var result = await _communicationQueue.SendCommandWithResultAsync(payload);
            EnsureNoProtocolError(result);
            var resultData = EpsonUtilities.GetXmlFieldValue(result, "resultdata");
            var resultPayload = JsonConvert.DeserializeObject<EpsonResult>(resultData);
            if (!resultPayload.Result.Equals("EXECUTION_OK", StringComparison.InvariantCulture))
            {
                throw new EpsonException(resultPayload.Result);
            }
            return;
        }

        private void EnsureNoProtocolError(string result)
        {
            if (bool.TryParse(EpsonUtilities.GetXmlFieldValue(result, "success"), out var success) && success)
            {
                return;
            }
            var errorCode = EpsonUtilities.GetXmlFieldValue(result, "code");
            if(errorCode == "OK")
            {
                return;
            }
            if(Enum.TryParse<EpsonError>(errorCode, out var epsonErrorCode))
            {
                switch (epsonErrorCode)
                {
                    case EpsonError.DEVICE_NOT_FOUND:
                    case EpsonError.TSE1_ERROR_NO_TSE:
                    case EpsonError.TSE1_ERROR_IO:
                        throw new EpsonException($"The TSE is not available. Unable to connect to endpoint at {Configuration.Host}:{Configuration.Port}.");
                    default:
                        EpsonExceptionHelper.ThrowError(epsonErrorCode);
                        break;
                }
            }
            throw new EpsonException(errorCode);
        }
    }
}
