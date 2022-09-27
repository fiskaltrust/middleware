using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Exceptions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Models;
using Microsoft.Extensions.Logging;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;

// IntPtr might be null in < NET6
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop
{
    public class SwissbitProxy : ISwissbitProxy, IDisposable
    {
        public const string ManagementClientId = "fiskaltrust.Middleware";

        public IntPtr Context => context;
        private IntPtr context = new IntPtr();

        private readonly SemaphoreSlim _hwSemaphore = new SemaphoreSlim(1, 1);
        private readonly IntPtr _adminPinPtr = IntPtr.Zero;
        private readonly int _adminPinLength = 0;
        private readonly IntPtr _timeAdminPinPtr = IntPtr.Zero;
        private readonly int _timeAdminPinLength = 0;
        private readonly NativeFunctionPointer _nativeFunctionPointer;
        private readonly LockingHelper _lockingHelper;
        private readonly ILogger _logger;
        private readonly string _mountPoint;

        public SwissbitProxy(string mountPoint, byte[] adminPin, byte[] timeAdminPin, INativeFunctionPointerFactory nativeFunctionPointerFactory, LockingHelper lockingHelper, ILogger logger)
        {
            _mountPoint = mountPoint;
            _adminPinLength = adminPin.Length;
            _adminPinPtr = Marshal.AllocHGlobal(_adminPinLength);
            Marshal.Copy(adminPin, 0, _adminPinPtr, _adminPinLength);

            _timeAdminPinLength = timeAdminPin.Length;
            _timeAdminPinPtr = Marshal.AllocHGlobal(_timeAdminPinLength);
            Marshal.Copy(timeAdminPin, 0, _timeAdminPinPtr, _timeAdminPinLength);

            _nativeFunctionPointer = nativeFunctionPointerFactory.LoadLibrary();
            _lockingHelper = lockingHelper;
            _logger = logger;
        }

        ~SwissbitProxy()
        {
            Dispose(false);
        }

        public async Task<bool> UpdateFirmwareAsync(bool firmwareUpdateEnabled)
        {
            var performedUpdate = false;
            await _lockingHelper.PerformWithLock(_hwSemaphore, async () =>
            {
                var fwPtr = Marshal.AllocHGlobal(sizeof(Int32));

                try
                {
                    _nativeFunctionPointer.func_worm_tse_firmwareUpdate_isBundledAvailable(context, fwPtr).ThrowIfError();

                    if ((WormTseFirmwareUpdate) Marshal.ReadInt32(fwPtr) != WormTseFirmwareUpdate.WORM_FW_NONE)
                    {
                        _logger.LogInformation("A Swissbit TSE firmware update is available. For more infos, please visit https://link.fiskaltrust.cloud/de/swisssbit-tse/update.");
                        if (firmwareUpdateEnabled)
                        {
                            _logger.LogWarning("Updating Swissbit TSE firmware, this may take several minutes. DON'T TURN OFF THE MIDDLEWARE WHILE THIS PROCESS IS RUNNING!");
                            _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_ADMIN, _adminPinPtr, _adminPinLength, IntPtr.Zero).ThrowIfError();
                            _nativeFunctionPointer.func_worm_tse_firmwareUpdate_applyBundled(context).ThrowIfError();
                            performedUpdate = true;
                            _logger.LogInformation($"Updated to Swissbit TSE firmware version {await GetVersionAsync()}.");
                        }
                    }
                }
                catch (SwissbitException ex)
                {
                    switch (ex.Error)
                    {
                        case WormError.WORM_ERROR_WRONG_STATE_NEEDS_PUK_CHANGE:
                            _logger.LogWarning($"The TSE needs to be initialized once before the firmware can be updated.");
                            break;
                        case WormError.WORM_ERROR_FWU_NOT_AVAILABLE:
                            _logger.LogInformation($"No firmware update available.");
                            break;
                        default:
                            _logger.LogError($"Swissbit TSE firmware update failed: {ex.Message}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Swissbit TSE firmware update failed: {ex.Message}");
                }
                finally
                {
                    Marshal.FreeHGlobal(fwPtr);
                }
            });
            return performedUpdate;
        }

        public async Task InitAsync()
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var mountPointPtr = Marshal.StringToHGlobalAnsi(_mountPoint);
                try
                {
                    _nativeFunctionPointer.func_worm_init(ref context, mountPointPtr).ThrowIfError();
                }
                finally
                {
                    Marshal.FreeHGlobal(mountPointPtr);
                }
            });
        }

        public async Task CleanupAsync(bool throwException = false)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var result = _nativeFunctionPointer.func_worm_cleanup(context);
                if (throwException)
                {
                    result.ThrowIfError();
                }
            });
        }

        public async Task<string> GetVersionAsync() => await _lockingHelper.PerformWithLock(_hwSemaphore, () => Marshal.PtrToStringAnsi(_nativeFunctionPointer.func_worm_getVersion()));

        public async Task<string> GetSignatureAlgorithmAsync() => await _lockingHelper.PerformWithLock(_hwSemaphore, () => Marshal.PtrToStringAnsi(_nativeFunctionPointer.func_worm_signatureAlgorithm()));

        public async Task<string> GetLogTimeFormatAsync() => await _lockingHelper.PerformWithLock(_hwSemaphore, () => Marshal.PtrToStringAnsi(_nativeFunctionPointer.func_worm_logTimeFormat()));

        public async Task<TseStatusInformation> GetTseStatusAsync()
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var infoPtr = new IntPtr();
                var idPtr = new IntPtr();
                var idLengthPtr = Marshal.AllocHGlobal(sizeof(Int32));
                var publicKeyPtr = new IntPtr();
                var publicKeyLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                var serialNumberPtr = new IntPtr();
                var serialNumberLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));

                try
                {
                    _logger.LogTrace("GetTseStatusAsync: Creating context..");
                    infoPtr = _nativeFunctionPointer.func_worm_info_new(context);
                    
                    _logger.LogTrace("GetTseStatusAsync: Reading infos from TSE..");
                    _nativeFunctionPointer.func_worm_info_read(infoPtr).ThrowIfError();
                    _logger.LogTrace("GetTseStatusAsync: Successfully read infos.");

                    var status = new TseStatusInformation
                    {
                        CapacityInBlocks = _nativeFunctionPointer.func_worm_info_capacity(infoPtr),
                        CertificateExpirationDate = _nativeFunctionPointer.func_worm_info_certificateExpirationDate(infoPtr),
                        CreatedSignatures = _nativeFunctionPointer.func_worm_info_createdSignatures(infoPtr),
                        HardwareVersion = _nativeFunctionPointer.func_worm_info_hardwareVersion(infoPtr),
                        SoftwareVersion = _nativeFunctionPointer.func_worm_info_softwareVersion(infoPtr),
                        HasChangedAdminPin = _nativeFunctionPointer.func_worm_info_hasChangedAdminPin(infoPtr) != 0,
                        HasChangedPuk = _nativeFunctionPointer.func_worm_info_hasChangedPuk(infoPtr) != 0,
                        HasChangedTimeAdminPin = _nativeFunctionPointer.func_worm_info_hasChangedTimeAdminPin(infoPtr) != 0,
                        HasPassedSelfTest = _nativeFunctionPointer.func_worm_info_hasPassedSelfTest(infoPtr) != 0,
                        HasValidTime = _nativeFunctionPointer.func_worm_info_hasValidTime(infoPtr) != 0,
                        initializationState = _nativeFunctionPointer.func_worm_info_initializationState(infoPtr),
                        IsCtssInterfaceActive = _nativeFunctionPointer.func_worm_info_isCtssInterfaceActive(infoPtr) != 0,
                        IsDataImportInProgress = _nativeFunctionPointer.func_worm_info_isDataImportInProgress(infoPtr) != 0,
                        IsDevelopmentFirmware = _nativeFunctionPointer.func_worm_info_isDevelopmentFirmware(infoPtr) != 0,
                        IsExportEnabledIfCspTestFails = _nativeFunctionPointer.func_worm_info_isExportEnabledIfCspTestFails(infoPtr) != 0,
                        MaxRegisteredClients = _nativeFunctionPointer.func_worm_info_maxRegisteredClients(infoPtr),
                        MaxSignatures = _nativeFunctionPointer.func_worm_info_maxSignatures(infoPtr),
                        MaxStartedTransactions = _nativeFunctionPointer.func_worm_info_maxStartedTransactions(infoPtr),
                        MaxTimeSynchronizationDelay = _nativeFunctionPointer.func_worm_info_maxTimeSynchronizationDelay(infoPtr),
                        MaxUpdateDelay = _nativeFunctionPointer.func_worm_info_maxUpdateDelay(infoPtr),
                        RegisteredClients = _nativeFunctionPointer.func_worm_info_registeredClients(infoPtr),
                        RemainingSignatures = _nativeFunctionPointer.func_worm_info_remainingSignatures(infoPtr),
                        SizeInBlocks = _nativeFunctionPointer.func_worm_info_size(infoPtr),
                        StartedTransactions = _nativeFunctionPointer.func_worm_info_startedTransactions(infoPtr),
                        TarExportSizeInBytes = _nativeFunctionPointer.func_worm_info_tarExportSize(infoPtr),
                        TarExportSizeInSectors = _nativeFunctionPointer.func_worm_info_tarExportSizeInSectors(infoPtr),
                        TimeUntilNextSelfTest = _nativeFunctionPointer.func_worm_info_timeUntilNextSelfTest(infoPtr)
                    };

                    _logger.LogTrace("GetTseStatusAsync: Reading customization identifier from TSE..");
                    _nativeFunctionPointer.func_worm_info_customizationIdentifier(infoPtr, ref idPtr, idLengthPtr);
                    status.CustomizationIdentifier = Marshal.PtrToStringAnsi(idPtr, Marshal.ReadInt16(idLengthPtr));

                    _logger.LogTrace("GetTseStatusAsync: Reading public key from TSE..");
                    _nativeFunctionPointer.func_worm_info_tsePublicKey(infoPtr, ref publicKeyPtr, publicKeyLengthPtr);
                    var publicKeyLength = (UInt64) Marshal.ReadInt64(publicKeyLengthPtr);
                    var publicKeyBytes = new byte[publicKeyLength];
                    Marshal.Copy(publicKeyPtr, publicKeyBytes, 0, (int) publicKeyLength);
                    status.TsePublicKey = publicKeyBytes;

                    _logger.LogTrace("GetTseStatusAsync: Reading serial number from TSE..");
                    _nativeFunctionPointer.func_worm_info_tseSerialNumber(infoPtr, ref serialNumberPtr, serialNumberLengthPtr);
                    var serialNumberLength = (UInt64) Marshal.ReadInt64(serialNumberLengthPtr);
                    var serialNumberBytes = new byte[serialNumberLength];
                    Marshal.Copy(serialNumberPtr, serialNumberBytes, 0, (int) serialNumberLength);
                    status.TseSerialNumber = serialNumberBytes;

                    status.TseDescription = Marshal.PtrToStringAnsi(_nativeFunctionPointer.func_worm_info_tseDescription(infoPtr));

                    status.FormFactor = Marshal.PtrToStringAnsi(_nativeFunctionPointer.func_worm_info_formFactor(infoPtr));

                    //all data-ptr will be freed by freeing info-object

                    return status;
                }
                finally
                {
                    _logger.LogTrace("GetTseStatusAsync: Freeing memory..");
                    if (infoPtr != null && infoPtr != IntPtr.Zero)
                    {
                        _nativeFunctionPointer.func_worm_info_free(infoPtr);
                    }
                    Marshal.FreeHGlobal(idLengthPtr);
                    Marshal.FreeHGlobal(publicKeyLengthPtr);
                    Marshal.FreeHGlobal(serialNumberLengthPtr);
                    _logger.LogTrace("GetTseStatusAsync: Memory freed successfully.");
                }
            });
        }
        public async Task TseSetupAsync(byte[] credentialSeed, byte[] adminPuk, byte[] adminPin, byte[] timeAdminPin)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var credentialSeedPtr = Marshal.AllocHGlobal(credentialSeed.Length);
                var adminPukPtr = Marshal.AllocHGlobal(adminPuk.Length);
                var adminPinPtr = Marshal.AllocHGlobal(adminPin.Length);
                var timeAdminPinPtr = Marshal.AllocHGlobal(timeAdminPin.Length);
                var clientIdPtr = Marshal.StringToHGlobalAnsi(ManagementClientId);
                try
                {
                    Marshal.Copy(credentialSeed, 0, credentialSeedPtr, credentialSeed.Length);
                    Marshal.Copy(adminPuk, 0, adminPukPtr, adminPuk.Length);
                    Marshal.Copy(adminPin, 0, adminPinPtr, adminPin.Length);
                    Marshal.Copy(timeAdminPin, 0, timeAdminPinPtr, timeAdminPin.Length);

                    //no error check, will return error "Client not registered."
                    _nativeFunctionPointer.func_worm_tse_runSelfTest(context, clientIdPtr);

                    _nativeFunctionPointer.func_worm_tse_setup(context,
                        credentialSeedPtr, credentialSeed.Length,
                        adminPukPtr, adminPuk.Length,
                        adminPinPtr, adminPin.Length,
                        timeAdminPinPtr, timeAdminPin.Length,
                        clientIdPtr)
                        .ThrowIfError();

                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN).ThrowIfError();
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_TIME_ADMIN).ThrowIfError();
                }
                finally
                {
                    Marshal.FreeHGlobal(credentialSeedPtr);
                    Marshal.FreeHGlobal(adminPukPtr);
                    Marshal.FreeHGlobal(adminPinPtr);
                    Marshal.FreeHGlobal(timeAdminPinPtr);
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task TseDecommissionAsync()
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                try
                {
                    _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_ADMIN, _adminPinPtr, _adminPinLength, IntPtr.Zero).ThrowIfError();
                    _nativeFunctionPointer.func_worm_tse_decommission(context).ThrowIfError();
                }
                finally
                {
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN);
                }
            });
        }

        public async Task TseUpdateTimeAsync()
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                try
                {
                    _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_TIME_ADMIN, _timeAdminPinPtr, _timeAdminPinLength, IntPtr.Zero);
                    _nativeFunctionPointer.func_worm_tse_updateTime(context, DateTime.UtcNow.ToTimestamp()).ThrowIfError();
                }
                finally
                {
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN);
                }
            });
        }

        public async Task TseRunSelfTestAsnyc(bool throwException = true)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(ManagementClientId);
                try
                {
                    var error = _nativeFunctionPointer.func_worm_tse_runSelfTest(context, clientIdPtr);
                    if (throwException)
                    {
                        error.ThrowIfError();
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task TseRegisterClientAsync(string clientId)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                try
                {
                    _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_ADMIN, _adminPinPtr, _adminPinLength, IntPtr.Zero).ThrowIfError();
                    _nativeFunctionPointer.func_worm_tse_registerClient(context, clientIdPtr).ThrowIfError();
                }
                finally
                {
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN);
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task TseDeregisterClientAsync(string clientId)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                try
                {
                    _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_ADMIN, _adminPinPtr, _adminPinLength, IntPtr.Zero).ThrowIfError();
                    _nativeFunctionPointer.func_worm_tse_deregisterClient(context, clientIdPtr).ThrowIfError();
                }
                finally
                {
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN);
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task<List<string>> TseGetRegisteredClientsAsync()
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientsPtr = Marshal.AllocHGlobal(sizeof(int) + (16 * 31));
                try
                {
                    var clientList = new List<string>();
                    _nativeFunctionPointer.func_worm_user_login(context, WormUserId.WORM_USER_ADMIN, _adminPinPtr, _adminPinLength, IntPtr.Zero).ThrowIfError();
                    var clientAmount = 0;
                    do
                    {
                        _nativeFunctionPointer.func_worm_tse_listRegisteredClients(context, clientList.Count, clientsPtr)
                            .ThrowIfError();
                        clientAmount = Marshal.ReadInt32(clientsPtr);
                        for (var i = 0; i < clientAmount; i++)
                        {
                            clientList.Add(Marshal.PtrToStringAnsi(IntPtr.Add(clientsPtr, sizeof(int) + (i * 31))));
                        }
                    } while (clientAmount > 0);
                    return clientList;
                }
                finally
                {
                    _nativeFunctionPointer.func_worm_user_logout(context, WormUserId.WORM_USER_ADMIN);
                    Marshal.FreeHGlobal(clientsPtr);
                }
            });
        }

        public async Task UserLoginAsync(WormUserId id, byte[] pin)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var pinPtr = Marshal.AllocHGlobal(pin.Length);
                try
                {
                    Marshal.Copy(pin, 0, pinPtr, pin.Length);
                    _nativeFunctionPointer.func_worm_user_login(context, id, pinPtr, pin.Length, IntPtr.Zero).ThrowIfError();
                }
                finally
                {
                    Marshal.FreeHGlobal(pinPtr);
                }
            });
        }

        public async Task UserLogoutAsync(WormUserId id) => await _lockingHelper.PerformWithLock(_hwSemaphore, () => _nativeFunctionPointer.func_worm_user_logout(context, id).ThrowIfError());

        public async Task<TransactionResponse> TransactionStartAsync(string clientId, byte[] processData, string processType)
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                var processDataPtr = Marshal.AllocHGlobal(processData.Length);
                var processTypePtr = Marshal.StringToHGlobalAnsi(processType);
                var transactionResponsePtr = IntPtr.Zero;
                var serialNumberPtr = new IntPtr();
                var serialNumberLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                var signaturePtr = new IntPtr();
                var signatureLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                try
                {
                    Marshal.Copy(processData, 0, processDataPtr, processData.Length);

                    transactionResponsePtr = _nativeFunctionPointer.func_worm_transaction_response_new(context);

                    var result = _nativeFunctionPointer.func_worm_transaction_start(context, clientIdPtr, processDataPtr, (UInt64) processData.Length, processTypePtr, transactionResponsePtr);
                    if (result == WormError.WORM_ERROR_CLIENT_NOT_REGISTERED)
                    {
                        throw new SwissbitException($"The client with the id {clientId} is not registered.");
                    }
                    else
                    {
                        result.ThrowIfError();
                    }

                    var transaction = new TransactionResponse()
                    {
                        LogTime = _nativeFunctionPointer.func_worm_transaction_response_logTime(transactionResponsePtr),
                        SignatureCounter = _nativeFunctionPointer.func_worm_transaction_response_signatureCounter(transactionResponsePtr),
                        TransactionNumber = _nativeFunctionPointer.func_worm_transaction_response_transactionNumber(transactionResponsePtr)
                    };

                    _nativeFunctionPointer.func_worm_transaction_response_serialNumber(transactionResponsePtr, ref serialNumberPtr, serialNumberLengthPtr);
                    var serialNumberLength = (UInt64) Marshal.ReadInt64(serialNumberLengthPtr);
                    var serialNumberBytes = new byte[serialNumberLength];
                    Marshal.Copy(serialNumberPtr, serialNumberBytes, 0, (int) serialNumberLength);
                    transaction.SerialNumber = serialNumberBytes;

                    _nativeFunctionPointer.func_worm_transaction_response_signature(transactionResponsePtr, ref signaturePtr, signatureLengthPtr);
                    var signatureLength = (UInt64) Marshal.ReadInt64(signatureLengthPtr);
                    var signatureBytes = new byte[signatureLength];
                    Marshal.Copy(signaturePtr, signatureBytes, 0, (int) signatureLength);
                    transaction.SignatureBase64 = Convert.ToBase64String(signatureBytes);

                    return transaction;
                }
                finally
                {
                    if (transactionResponsePtr != IntPtr.Zero)
                    {
                        _nativeFunctionPointer.func_worm_transaction_response_free(transactionResponsePtr);
                    }

                    Marshal.FreeHGlobal(clientIdPtr);
                    Marshal.FreeHGlobal(processDataPtr);
                    Marshal.FreeHGlobal(processTypePtr);
                    Marshal.FreeHGlobal(serialNumberLengthPtr);
                    Marshal.FreeHGlobal(signatureLengthPtr);
                }
            });
        }

        public async Task<TransactionResponse> TransactionUpdateAsync(string clientId, UInt64 transactionNumber, byte[] processData, string processType)
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                var processDataPtr = Marshal.AllocHGlobal(processData.Length);
                var processTypePtr = Marshal.StringToHGlobalAnsi(processType);
                var transactionResponsePtr = IntPtr.Zero;
                var serialNumberPtr = new IntPtr();
                var serialNumberLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                var signaturePtr = new IntPtr();
                var signatureLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                try
                {
                    Marshal.Copy(processData, 0, processDataPtr, processData.Length);

                    transactionResponsePtr = _nativeFunctionPointer.func_worm_transaction_response_new(context);

                    var result = _nativeFunctionPointer.func_worm_transaction_update(context, clientIdPtr, transactionNumber, processDataPtr, (UInt64) processData.Length, processTypePtr, transactionResponsePtr);
                    if (result == WormError.WORM_ERROR_TRANSACTION_NOT_STARTED)
                    {
                        throw new SwissbitException($"The transaction with the number {transactionNumber} is either not started or has been finished already.");
                    }
                    else if (result == WormError.WORM_ERROR_CLIENT_NOT_REGISTERED)
                    {
                        throw new SwissbitException($"The client with the id {clientId} is not registered.");
                    }
                    else
                    {
                        result.ThrowIfError();
                    }
                    var transaction = new TransactionResponse()
                    {
                        LogTime = _nativeFunctionPointer.func_worm_transaction_response_logTime(transactionResponsePtr),
                        SignatureCounter = _nativeFunctionPointer.func_worm_transaction_response_signatureCounter(transactionResponsePtr),
                        TransactionNumber = _nativeFunctionPointer.func_worm_transaction_response_transactionNumber(transactionResponsePtr)
                    };

                    _nativeFunctionPointer.func_worm_transaction_response_serialNumber(transactionResponsePtr, ref serialNumberPtr, serialNumberLengthPtr);
                    var serialNumberLength = (UInt64) Marshal.ReadInt64(serialNumberLengthPtr);
                    var serialNumberBytes = new byte[serialNumberLength];
                    Marshal.Copy(serialNumberPtr, serialNumberBytes, 0, (int) serialNumberLength);
                    transaction.SerialNumber = serialNumberBytes;

                    _nativeFunctionPointer.func_worm_transaction_response_signature(transactionResponsePtr, ref signaturePtr, signatureLengthPtr);
                    var signatureLength = (UInt64) Marshal.ReadInt64(signatureLengthPtr);
                    var signatureBytes = new byte[signatureLength];
                    Marshal.Copy(signaturePtr, signatureBytes, 0, (int) signatureLength);
                    transaction.SignatureBase64 = Convert.ToBase64String(signatureBytes);

                    return transaction;
                }
                finally
                {
                    if (transactionResponsePtr != IntPtr.Zero)
                    {
                        _nativeFunctionPointer.func_worm_transaction_response_free(transactionResponsePtr);
                    }

                    Marshal.FreeHGlobal(clientIdPtr);
                    Marshal.FreeHGlobal(processDataPtr);
                    Marshal.FreeHGlobal(processTypePtr);
                    Marshal.FreeHGlobal(serialNumberLengthPtr);
                    Marshal.FreeHGlobal(signatureLengthPtr);
                }
            });
        }

        public async Task<TransactionResponse> TransactionFinishAsync(string clientId, UInt64 transactionNumber, byte[] processData, string processType)
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                var processDataPtr = Marshal.AllocHGlobal(processData.Length);
                var processTypePtr = Marshal.StringToHGlobalAnsi(processType);
                var transactionResponsePtr = IntPtr.Zero;
                var serialNumberPtr = new IntPtr();
                var serialNumberLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                var signaturePtr = new IntPtr();
                var signatureLengthPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                try
                {
                    Marshal.Copy(processData, 0, processDataPtr, processData.Length);

                    transactionResponsePtr = _nativeFunctionPointer.func_worm_transaction_response_new(context);

                    var result = _nativeFunctionPointer.func_worm_transaction_finish(context, clientIdPtr, transactionNumber, processDataPtr, (UInt64) processData.Length, processTypePtr, transactionResponsePtr);
                    if (result == WormError.WORM_ERROR_TRANSACTION_NOT_STARTED)
                    {
                        throw new SwissbitException($"The transaction with the number {transactionNumber} is either not started or has been finished already.");
                    }
                    else if (result == WormError.WORM_ERROR_CLIENT_NOT_REGISTERED)
                    {
                        throw new SwissbitException($"The client with the id {clientId} is not registered.");
                    }
                    else
                    {
                        result.ThrowIfError();
                    }

                    var transaction = new TransactionResponse()
                    {
                        LogTime = _nativeFunctionPointer.func_worm_transaction_response_logTime(transactionResponsePtr),
                        SignatureCounter = _nativeFunctionPointer.func_worm_transaction_response_signatureCounter(transactionResponsePtr),
                        TransactionNumber = _nativeFunctionPointer.func_worm_transaction_response_transactionNumber(transactionResponsePtr)
                    };

                    _nativeFunctionPointer.func_worm_transaction_response_serialNumber(transactionResponsePtr, ref serialNumberPtr, serialNumberLengthPtr);
                    var serialNumberLength = (UInt64) Marshal.ReadInt64(serialNumberLengthPtr);
                    var serialNumberBytes = new byte[serialNumberLength];
                    Marshal.Copy(serialNumberPtr, serialNumberBytes, 0, (int) serialNumberLength);
                    transaction.SerialNumber = serialNumberBytes;

                    _nativeFunctionPointer.func_worm_transaction_response_signature(transactionResponsePtr, ref signaturePtr, signatureLengthPtr);
                    var signatureLength = (UInt64) Marshal.ReadInt64(signatureLengthPtr);
                    var signatureBytes = new byte[signatureLength];
                    Marshal.Copy(signaturePtr, signatureBytes, 0, (int) signatureLength);
                    transaction.SignatureBase64 = Convert.ToBase64String(signatureBytes);

                    return transaction;
                }
                finally
                {
                    if (transactionResponsePtr != IntPtr.Zero)
                    {
                        _nativeFunctionPointer.func_worm_transaction_response_free(transactionResponsePtr);
                    }

                    Marshal.FreeHGlobal(clientIdPtr);
                    Marshal.FreeHGlobal(processDataPtr);
                    Marshal.FreeHGlobal(processTypePtr);
                    Marshal.FreeHGlobal(serialNumberLengthPtr);
                    Marshal.FreeHGlobal(signatureLengthPtr);
                }
            });
        }

        public async Task<List<ulong>> GetStartedTransactionsAsync(string clientId)
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                const int fetchSize = 60;  // maximum possible is 62
                var responseSizePtr = Marshal.AllocHGlobal(sizeof(Int32));
                var responseListPtr = Marshal.AllocHGlobal(fetchSize * sizeof(UInt64));
                var startedTransactionsList = new List<UInt64>();

                try
                {
                    var responseSize = 0;
                    var skipSize = 0;
                    do
                    {
                        _nativeFunctionPointer.func_worm_transaction_listStartedTransactions(context, clientIdPtr, skipSize, responseListPtr, fetchSize, responseSizePtr)
                            .ThrowIfError();

                        responseSize = Marshal.ReadInt32(responseSizePtr);
                        var responseList = new long[responseSize];
                        Marshal.Copy(responseListPtr, responseList, 0, responseSize);
                        startedTransactionsList.AddRange(responseList.Select(i => (UInt64) i));
                        skipSize += responseSize;
                    } while (responseSize == fetchSize);

                    return startedTransactionsList;
                }
                finally
                {
                    Marshal.FreeHGlobal(clientIdPtr);
                    Marshal.FreeHGlobal(responseListPtr);
                    Marshal.FreeHGlobal(responseSizePtr);
                }
            });
        }

        public async Task ExportTarAsync(Stream stream)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                _nativeFunctionPointer.func_worm_export_tar(
                    context,
                    Marshal.GetFunctionPointerForDelegate(new WormExportTarCallback((IntPtr chunk, uint chunkLength, IntPtr callbackData) =>
                    {
                        var chunkBytes = new byte[chunkLength];
                        Marshal.Copy(chunk, chunkBytes, 0, (int) chunkLength);
                        stream.Write(chunkBytes, 0, (int) chunkLength);
                        return WormError.WORM_ERROR_NOERROR;
                    })),
                    IntPtr.Zero)
                    .ThrowIfError();
            });
        }

        public async Task<byte[]> ExportTarIncrementalAsync(Stream stream, byte[] lastStateBytes)
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                Int16 stateSize = 16;

                if (lastStateBytes.Length > 0 && lastStateBytes.Length != stateSize)
                {
                    throw new ArgumentException($"lastStateBytes needs to be empty or {stateSize} long", nameof(lastStateBytes));
                }

                var oldStatePtr = Marshal.AllocHGlobal(stateSize);
                var newStatePtr = Marshal.AllocHGlobal(stateSize);
                var firstSignatureCounterPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                var lastSignatureCounterPtr = Marshal.AllocHGlobal(sizeof(UInt64));
                try
                {
                    var callback = Marshal.GetFunctionPointerForDelegate(new WormExportTarIncrementalCallback((IntPtr chunk, uint chunkLength, UInt32 processedBlocks, UInt32 totalBlocks, IntPtr callbackData) =>
                    {
                        var chunkBytes = new byte[chunkLength];
                        Marshal.Copy(chunk, chunkBytes, 0, (int) chunkLength);
                        stream.Write(chunkBytes, 0, (int) chunkLength);
                        return WormError.WORM_ERROR_NOERROR;
                    }));

                    if (lastStateBytes.Length == 0)
                    {
                        _nativeFunctionPointer.func_worm_export_tar_incremental(
                            context,
                            IntPtr.Zero, 0,
                            newStatePtr, stateSize,
                            firstSignatureCounterPtr, lastSignatureCounterPtr,
                            callback, IntPtr.Zero)
                            .ThrowIfError();
                    }
                    else
                    {
                        Marshal.Copy(lastStateBytes, 0, oldStatePtr, stateSize);
                        _nativeFunctionPointer.func_worm_export_tar_incremental(
                            context,
                            oldStatePtr, stateSize,
                            newStatePtr, stateSize,
                            firstSignatureCounterPtr, lastSignatureCounterPtr,
                            callback, IntPtr.Zero)
                            .ThrowIfError();
                    }

                    var newStateBytes = new byte[stateSize];
                    Marshal.Copy(newStatePtr, newStateBytes, 0, stateSize);
                    return newStateBytes;
                }
                finally
                {
                    Marshal.FreeHGlobal(oldStatePtr);
                    Marshal.FreeHGlobal(newStatePtr);
                }
            });
        }

        public async Task ExportTarFilteredTimeAsync(Stream stream, UInt64 startDateUnixTime, UInt64 endDateUnixTime, string clientId)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                try
                {

                    var callback = Marshal.GetFunctionPointerForDelegate(new WormExportTarCallback((IntPtr chunk, uint chunkLength, IntPtr callbackData) =>
                   {
                       var chunkBytes = new byte[chunkLength];
                       Marshal.Copy(chunk, chunkBytes, 0, (int) chunkLength);
                       stream.Write(chunkBytes, 0, (int) chunkLength);
                       return WormError.WORM_ERROR_NOERROR;
                   }));

                    _nativeFunctionPointer.func_worm_export_tar_filtered_time(context, startDateUnixTime, endDateUnixTime, clientIdPtr, callback, IntPtr.Zero)
                        .ThrowIfError();
                }
                finally
                {
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task ExportTarFilteredTransactionAsync(Stream stream, UInt64 startTransactionNumber, UInt64 endTransactionNumber, string clientId)
        {
            await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                var clientIdPtr = Marshal.StringToHGlobalAnsi(clientId);
                try
                {
                    var callback = Marshal.GetFunctionPointerForDelegate(new WormExportTarCallback((IntPtr chunk, uint chunkLength, IntPtr callbackData) =>
                    {
                        var chunkBytes = new byte[chunkLength];
                        Marshal.Copy(chunk, chunkBytes, 0, (int) chunkLength);
                        stream.Write(chunkBytes, 0, (int) chunkLength);
                        return WormError.WORM_ERROR_NOERROR;
                    }));

                    _nativeFunctionPointer.func_worm_export_tar_filtered_transaction(context, startTransactionNumber, endTransactionNumber, clientIdPtr, callback, IntPtr.Zero).ThrowIfError();
                }
                finally
                {
                    Marshal.FreeHGlobal(clientIdPtr);
                }
            });
        }

        public async Task<byte[]> GetLogMessageCertificateAsync()
        {
            return await _lockingHelper.PerformWithLock(_hwSemaphore, () =>
            {
                UInt32 initialCertificatePtrSize = 16 * 1024;
                var certificatePtr = Marshal.AllocHGlobal((int) initialCertificatePtrSize);
                var certificateLengthPtr = Marshal.AllocHGlobal(sizeof(UInt32));
                try
                {
                    Marshal.WriteInt32(certificateLengthPtr, (int) initialCertificatePtrSize);

                    _nativeFunctionPointer.func_worm_getLogMessageCertificate(context, certificatePtr, certificateLengthPtr)
                        .ThrowIfError();

                    var certificateLength = (UInt32) Marshal.ReadInt32(certificateLengthPtr);
                    var certificateBytes = new byte[certificateLength];
                    Marshal.Copy(certificatePtr, certificateBytes, 0, (int) certificateLength);

                    return certificateBytes;
                }
                finally
                {
                    Marshal.FreeHGlobal(certificateLengthPtr);
                    Marshal.FreeHGlobal(certificatePtr);
                }
            });
        }

        public async Task DeleteStoredDataAsync() => await _lockingHelper.PerformWithLock(_hwSemaphore, () => _nativeFunctionPointer.func_worm_export_deleteStoredData(context).ThrowIfError());

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {



                }

                try
                {
                    if (context != IntPtr.Zero)
                    {
                        _nativeFunctionPointer.func_worm_cleanup(context);
                    }
                }
                finally
                {
                    context = IntPtr.Zero;
                }

                if (_adminPinPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_adminPinPtr);
                }
                if (_timeAdminPinPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_timeAdminPinPtr);
                }

                disposed = true;
            }
        }

        public async Task<bool> HasValidTimeAsync()
        {
            var infoPtr = _nativeFunctionPointer.func_worm_info_new(context);
            _nativeFunctionPointer.func_worm_info_read(infoPtr).ThrowIfError();
            return await Task.FromResult(_nativeFunctionPointer.func_worm_info_hasValidTime(infoPtr) != 0);
        }

        public async Task<bool> HasPassedSelfTestAsync()
        {
            var infoPtr = _nativeFunctionPointer.func_worm_info_new(context);
            _nativeFunctionPointer.func_worm_info_read(infoPtr).ThrowIfError();
            return await Task.FromResult(_nativeFunctionPointer.func_worm_info_hasPassedSelfTest(infoPtr) != 0);
        }

        public async Task<TseStates> GetInitializationState()
        {
            var infoPtr = _nativeFunctionPointer.func_worm_info_new(context);
            _nativeFunctionPointer.func_worm_info_read(infoPtr).ThrowIfError();
            return await Task.FromResult(_nativeFunctionPointer.func_worm_info_initializationState(infoPtr).ToTseStates());
        }
    }
}
