using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands;
using static fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands.TransportAdapterCommands;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop
{
    public partial class CryptoVisionFileProxy : ICryptoVisionProxy
    {
        private readonly ITseTransportAdapter _transportAdapter;

        public CryptoVisionFileProxy(ITseTransportAdapter transportAdapter)
        {
            _transportAdapter = transportAdapter;
        }

        public async Task CloseSeConnectionAsync()
        {
            try
            {
                _ = await SeShutdownAsync();
            }
            catch { }
            _transportAdapter.CloseFile();
        }

        public async Task ResetSeConnectionAsync()
        {
            await CloseSeConnectionAsync();
            _transportAdapter.ReopenFile();
            (var lastResult, _, _) = await SeStartAsync();
            lastResult.ThrowIfError();
        }

        public void ReOpen()
        {
            _transportAdapter.ReopenFile();
        }

        public async Task<SeResult> SeActivateAsync() => await CommandRunner.ExecuteSimpleCommandAsync(_transportAdapter, TseCommandCodeEnum.Activate);

        public async Task<(SeResult, SeAuthenticationResult, short remainingRetries)> SeAuthenticateUserAsync(string userId, byte[] pin)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AuthenticationCommands.CreateAuthenticateUserTseCommand(userId, pin);
                var response = await _transportAdapter.ExecuteAsync(command);
                var authenticationResult = (SeAuthenticationResult) ((TseByteParameter) response[0]).DataValue;
                var remainingRetries = (short) ((TseShortParameter) response[1]).DataValue;
                return (SeResult.ExecutionOk, authenticationResult, remainingRetries);
            });
        }

        public async Task<SeResult> SeDeactivateAsync() => await CommandRunner.ExecuteSimpleCommandAsync(_transportAdapter, TseCommandCodeEnum.Deactivate);

        public async Task<SeResult> SeDeleteDataUpToAsync(byte[] serialNumber, uint signatureCounter)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateDeleteDataUpToTseCommand(serialNumber, signatureCounter);
                await _transportAdapter.ExecuteAsync(command);
                return SeResult.ExecutionOk;
            });
        }

        public async Task<SeResult> SeDeleteStoredDataAsync() => await CommandRunner.ExecuteSimpleCommandAsync(_transportAdapter, TseCommandCodeEnum.Erase);

        public async Task<SeResult> SeDisableSecureElementAsync() => await CommandRunner.ExecuteSimpleCommandAsync(_transportAdapter, TseCommandCodeEnum.Disable);

        public async Task<(SeResult, byte[])> SeExportCertificatesAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new SimpleTseCommand(TseCommandCodeEnum.GetCertificates);
                var response = await _transportAdapter.ExecuteAsync(command);
                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<SeResult> SeExportDataAsync(Stream stream, string clientId = null, int maximumNumberOfRecords = 0)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new ExportCommands.ExportDataTseCommand(clientId, (uint) maximumNumberOfRecords);

                foreach (var item in await _transportAdapter.ExecuteAsync(command))
                {
                    await stream.WriteAsync(item.DataBytes, 0, item.DataBytes.Length);
                }
                return SeResult.ExecutionOk;
            });
        }

        public async Task<SeResult> SeExportMoreDataAsync(Stream stream, byte[] serialNumber, long previousSignatureCounter, long maxNumberOfRecords)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateExportMoreDataTseCommand(serialNumber, (uint) previousSignatureCounter, (uint) maxNumberOfRecords);

                foreach (var item in await _transportAdapter.ExecuteAsync(command))
                {
                    await stream.WriteAsync(item.DataBytes, 0, item.DataBytes.Length);
                }
                return SeResult.ExecutionOk;
            });
        }


        public async Task<SeResult> SeExportDateRangeDataAsync(Stream stream, ulong startUnixTime, ulong endUnixTime, string clientId = null, int maximumNumberOfRecords = 0)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new ExportCommands.ExportDataTseCommand((long) startUnixTime, (long) endUnixTime, clientId, (uint) maximumNumberOfRecords);
                foreach (var item in await _transportAdapter.ExecuteAsync(command))
                {
                    await stream.WriteAsync(item.DataBytes, 0, item.DataBytes.Length);
                }
                return SeResult.ExecutionOk;
            });
        }

        public async Task<(SeResult, byte[])> SeExportPublicKeyAsync(byte[] serialNumber)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateExportPublicKeyTseCommand(serialNumber);
                var response = await _transportAdapter.ExecuteAsync(command);
                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<(SeResult, byte[])> SeExportSerialNumbersAsnyc()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new SimpleTseCommand(TseCommandCodeEnum.GetSerialNumbers);
                var response = await _transportAdapter.ExecuteAsync(command);
                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<SeResult> SeExportTransactionDataAsync(Stream stream, uint transactionNumber, string clientId = null, int maximumNumberOfRecords = 0)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new ExportCommands.ExportDataTseCommand(transactionNumber, clientId, (uint) maximumNumberOfRecords);
                foreach (var item in await _transportAdapter.ExecuteAsync(command))
                {
                    await stream.WriteAsync(item.DataBytes, 0, item.DataBytes.Length);
                }
                return SeResult.ExecutionOk;
            });
        }

        public async Task<SeResult> SeExportTransactionRangeDataAsync(Stream stream, uint startTransactionNumber, uint endTransactionNumber, string clientId = null, int maximumNumberOfRecords = 0)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new ExportCommands.ExportDataTseCommand(startTransactionNumber, endTransactionNumber, clientId, (uint) maximumNumberOfRecords);
                foreach (var item in await _transportAdapter.ExecuteAsync(command))
                {
                    await stream.WriteAsync(item.DataBytes, 0, item.DataBytes.Length);
                }
                return SeResult.ExecutionOk;
            });
        }

        public async Task<(SeResult, SeTransactionResult)> SeFinishTransactionAsync(string clientId, uint transactionNumber, byte[] processData, string processType)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = InputCommands.CreateFinishTransactionTseCommand(clientId, transactionNumber, processData, processType);
                var response = await _transportAdapter.ExecuteAsync(command);

                var transactionResult = new SeTransactionResult
                {
                    SignatureCounter = ((TseByteArrayParameter) response[0]).DataValue.ToUInt32(),
                    LogUnixTime = ((TseByteArrayParameter) response[1]).DataValue.ToUInt64(),
                    SignatureValue = ((TseByteArrayParameter) response[2]).DataValue,
                    SerialNumber = ((TseByteArrayParameter) response[3]).DataValue
                };

                return (SeResult.ExecutionOk, transactionResult);
            });
        }

        public async Task<(SeResult, ulong)> SeGetAvailableLogMemoryAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetAvailableLogMemoryTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var availableLogMemory = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, availableLogMemory.ToUInt64());
            });
        }

        public async Task<(SeResult, ulong unixTime)> SeGetCertificateExpirationDateAsync(byte[] serialNumber)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateGetCertificateExpirationDateTseCommand(serialNumber);
                var response = await _transportAdapter.ExecuteAsync(command);
                var expirationTimestamp = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, expirationTimestamp.ToUInt64());
            });
        }

        public async Task<(SeResult, string certificationId)> SeGetCertificationIdAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetCertificationIdTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);

                return (SeResult.ExecutionOk, ((TseStringParameter) response[0]).DataValue);
            });
        }

        public async Task<(SeResult, uint)> SeGetCurrentNumberOfClientsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetCurrentNumberOfClientsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var currentNumberOfClients = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, currentNumberOfClients.ToUInt32());
            });
        }

        public async Task<(SeResult, uint)> SeGetCurrentNumberOfTransactionsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetCurrentNumberOfTransactionsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var currentNumberOfTransactions = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, currentNumberOfTransactions.ToUInt32());
            });
        }

        public async Task<(SeResult, byte[])> SeGetERSMappingsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new SimpleTseCommand(TseCommandCodeEnum.GetERSMappings);
                var response = await _transportAdapter.ExecuteAsync(command);
                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<(SeResult, SeLifeCycleState)> SeGetLifeCycleStateAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetLifeCycleStateTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var lifeCycleState = (SeLifeCycleState) ((TseByteArrayParameter) response[0]).DataValue.ToUInt16();

                return (SeResult.ExecutionOk, lifeCycleState);
            });
        }

        public async Task<(SeResult, uint maxNumberOfClients)> SeGetMaxNumberOfClientsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetMaxNumberOfClientsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var maxNumberOfClients = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, maxNumberOfClients.ToUInt32());
            });
        }

        public async Task<(SeResult, uint maxNumberOfTransactions)> SeGetMaxNumberOfTransactionsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetMaxNumberOfTransactionsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var maxNumberOfTransactions = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, maxNumberOfTransactions.ToUInt32());
            });
        }

        public async Task<(SeResult, uint[])> SeGetOpenTransactionsAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetOpenTransactionsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                // destroys transportation layer because of response size missmatch by tse
                return (SeResult.ExecutionOk, ((TseLongArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<(SeResult, bool adminPinInTransportState, bool adminPukInTransportState, bool timeAdminPinInTransportState, bool timeAdminPukInTransportState)> SeGetPinStatesAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
               {
                   var command = new SimpleTseCommand(TseCommandCodeEnum.GetPinStates);
                   var response = await _transportAdapter.ExecuteAsync(command);
                   var pinStates = ((TseByteArrayParameter) response[0]).DataValue;


                   return (SeResult.ExecutionOk, pinStates[0] != 0, pinStates[1] != 0, pinStates[2] != 0, pinStates[3] != 0);
               });
        }

        public async Task<(SeResult, byte[] signatureAlgorithmOid)> SeGetSignatureAlgorithmAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetSignatureAlgorithmTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);

                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<(SeResult, uint)> SeGetSignatureCounterAsync(byte[] serialNumber)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateGetSignatureCounterTseCommand(serialNumber);
                var response = await _transportAdapter.ExecuteAsync(command);
                var signatureCounter = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, signatureCounter.ToUInt32());
            });
        }

        public async Task<(SeResult, SeUpdateVariant)> SeGetSupportedTransactionUpdateVariantAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetSupportedTransactionUpdateVariantsTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var supportedTransactionUpdateVariants = (SeUpdateVariant) ((TseByteArrayParameter) response[0]).DataValue.ToUInt16();

                return (SeResult.ExecutionOk, supportedTransactionUpdateVariants);
            });
        }

        public async Task<(SeResult, uint timeSyncIntervalSeconds)> SeGetTimeSyncIntervalAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetTimeSyncIntervalTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var timeSyncIntervalSeconds = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, timeSyncIntervalSeconds.ToUInt16());
            });
        }

        public async Task<(SeResult, SeSyncVariant)> SeGetTimeSyncVariantAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetConfigDataCommands.CreateGetTimeSyncVariantTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var syncVariant = (SeSyncVariant) ((TseByteArrayParameter) response[0]).DataValue.ToUInt16();

                return (SeResult.ExecutionOk, syncVariant);
            });
        }

        public async Task<(SeResult, ulong)> SeGetTotalLogMemoryAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetTotalLogMemoryTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var totalLogMemory = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, totalLogMemory.ToUInt64());
            });
        }

        public async Task<(SeResult, uint)> SeGetTransactionCounterAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = ConfigurationAndStatusInformationCommands.GetStatusCommands.CreateGetTransactionCounterTseCommand();
                var response = await _transportAdapter.ExecuteAsync(command);
                var transactionCounter = ((TseByteArrayParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, transactionCounter.ToUInt32());
            });
        }

        public async Task<(SeResult, ushort)> SeGetWearIndicatorAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new SimpleTseCommand(TseCommandCodeEnum.GetWearIndicator);
                var response = await _transportAdapter.ExecuteAsync(command);
                var transactionCounter = ((TseShortParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, transactionCounter);
            });
        }

        public async Task<SeResult> SeInitializeAsync() => await CommandRunner.ExecuteSimpleCommandAsync(_transportAdapter, TseCommandCodeEnum.Initialize);

        public async Task<SeResult> SeInitializePinsAsync(byte[] adminPuk, byte[] adminPin, byte[] timeAdminPuk, byte[] timeAdminPin)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateInitializePinsTseCommand(adminPuk, adminPin, timeAdminPuk, timeAdminPin);
                var response = await _transportAdapter.ExecuteAsync(command);

                return SeResult.ExecutionOk;
            });
        }

        /// <summary>
        /// This is the overload of the InitializePinAsync command which is used ONLY with V2 hardware
        /// </summary>
        public async Task<SeResult> SeInitializePinsAsync(string userId, byte[] userPuk)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateInitializePinsTseCommand(userId, userPuk);
                var response = await _transportAdapter.ExecuteAsync(command);

                return SeResult.ExecutionOk;
            });
        }

        public async Task<SeResult> SeLogOutAsync(string userId)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AuthenticationCommands.CreateLogoutTseCommand(userId);
                var response = await _transportAdapter.ExecuteAsync(command);

                return SeResult.ExecutionOk;
            });
        }

        public async Task<SeResult> SeMapERStoKeyAsync(string clientId, byte[] serialNumber)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AdditionalCommands.CreateMapERStoKeyTseCommand(clientId, serialNumber);
                var response = await _transportAdapter.ExecuteAsync(command);

                return SeResult.ExecutionOk;
            });
        }

        public async Task<(SeResult, byte[])> SeReadLogMessageAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = new SimpleTseCommand(TseCommandCodeEnum.ReadLogMessage);
                var response = await _transportAdapter.ExecuteAsync(command);
                return (SeResult.ExecutionOk, ((TseByteArrayParameter) response[0]).DataValue);
            });
        }

        public async Task<SeResult> SeShutdownAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                _ = await _transportAdapter.ExecuteAsync(new SimpleTseCommand(TseCommandCodeEnum.Shutdown));

                _ = await _transportAdapter.ExecuteAsync(new EnableSuspendModeTseCommand());

                return SeResult.ExecutionOk;
            });
        }

        public async Task<(SeResult, string deviceVersion, byte[] deviceUniqueId)> SeStartAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                _ = await _transportAdapter.ExecuteAsync(new DisableSuspendModeTseCommand());

                var command = new SimpleTseCommand(TseCommandCodeEnum.Start);
                var response = await _transportAdapter.ExecuteAsync(command);

                var DeviceFirmwareId = ((TseStringParameter) response[0]).DataValue;
                var DeviceUniqueId = ((TseByteArrayParameter) response[1]).DataValue;

                //TODO
                ///* version check (API 2.0 is not compatible with API v1.0 TSE firmware) */
                //{
                //    const char* api1_fw_version = "f44a9e", *api1_fw_version_2 = "aee640";
                //    if (strstr(ctx->version, api1_fw_version) != NULL
                //    || strstr(ctx->version, api1_fw_version_2) != NULL)
                //        return ErrorTSEFirmwareVersion;
                //}
                ///* version check (API 2.1 is not fully compatible with API v2.0 TSE firmware) */
                //{
                //    const char* api2_fw_version = "52376";
                //    if (strstr(ctx->version, api2_fw_version) != NULL)
                //        return ErrorTSEFirmwareVersion;
                //}


                return (SeResult.ExecutionOk, DeviceFirmwareId, DeviceUniqueId);
            });
        }

        public async Task<(SeResult, SeStartTransactionResult)> SeStartTransactionAsync(string clientId, byte[] processData, string processType)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = InputCommands.CreateStartTransactionTseCommand(clientId, processData, processType);
                var response = await _transportAdapter.ExecuteAsync(command);

                // documentation doesn't match c-implementation, this implementation doesn't follow implementation by purpose
                var transactionResult = new SeStartTransactionResult
                {
                    TransactionNumber = ((TseByteArrayParameter) response[0]).DataValue.ToUInt32(),
                    SignatureCounter = ((TseByteArrayParameter) response[1]).DataValue.ToUInt32(),
                    LogUnixTime = ((TseByteArrayParameter) response[2]).DataValue.ToUInt64(),
                    SignatureValue = ((TseByteArrayParameter) response[3]).DataValue,
                    SerialNumber = ((TseByteArrayParameter) response[4]).DataValue,
                };

                return (SeResult.ExecutionOk, transactionResult);
            });
        }

        public async Task<(SeResult, SeAuthenticationResult)> SeUnblockUserAsync(string userId, byte[] puk, byte[] newPin)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = AuthenticationCommands.CreateUnblockUserTseCommand(userId, puk, newPin);
                var response = await _transportAdapter.ExecuteAsync(command);

                var authenticationResult = (SeAuthenticationResult) ((TseByteParameter) response[0]).DataValue;

                return (SeResult.ExecutionOk, authenticationResult);
            });
        }

        public Task<(SeResult, byte[])> SeUpdateCertificateAsync(byte[] data) => throw new NotImplementedException();

        public Task<(SeResult, byte[])> SeUpdateFirmwareAsync(byte[] data) => throw new NotImplementedException();

        public async Task<SeResult> SeUpdateTimeAsync()
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = MaintenanceAndTimeSynchronizationCommands.CreateUpdateTimeTseCommand((long)DateTime.UtcNow.ToTimestamp());
                _ = await _transportAdapter.ExecuteAsync(command);

                return SeResult.ExecutionOk;
            });
        }

        public async Task<(SeResult, SeTransactionResult)> SeUpdateTransactionAsync(string clientId, uint transactionNumber, byte[] processData, string processType)
        {
            return await CommandRunner.ExecuteAsync(async () =>
            {
                var command = InputCommands.CreateUpdateTransactionTseCommand(clientId, transactionNumber, processData, processType);
                var response = await _transportAdapter.ExecuteAsync(command);

                var transactionResult = new SeTransactionResult
                {
                    SignatureCounter = ((TseByteArrayParameter) response[0]).DataValue.ToUInt32(),
                    LogUnixTime = ((TseByteArrayParameter) response[1]).DataValue.ToUInt64(),
                    SignatureValue = ((TseByteArrayParameter) response[2]).DataValue,
                    SerialNumber = ((TseByteArrayParameter) response[3]).DataValue
                };

                return (SeResult.ExecutionOk, transactionResult);
            });
        }
    }
}
