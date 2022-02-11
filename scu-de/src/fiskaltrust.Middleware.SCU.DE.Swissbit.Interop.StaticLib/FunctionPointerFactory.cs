using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.StaticLib
{
    public class FunctionPointerFactory : INativeFunctionPointerFactory
    {
        public FunctionPointerFactory()
        {
            //TODO maybe set path related to architecture using kernel32 SetDllDirectory
            // see example here: https://github.com/Jorgemagic/NetStandard_And_NativeLibraries/blob/master/NativeLibWrapper/DLLRegister.cs
        }

        public FunctionPointerFactory(Action libraryLoader)
        {
            libraryLoader();
        }

        public NativeFunctionPointer LoadLibrary() => new NativeFunctionPointer
        {
            func_worm_getVersion = Shared.NativeWormAPI.worm_getVersion,
            func_worm_signatureAlgorithm = Shared.NativeWormAPI.worm_signatureAlgorithm,
            func_worm_logTimeFormat = Shared.NativeWormAPI.worm_logTimeFormat,
            func_worm_init = Shared.NativeWormAPI.worm_init,
            func_worm_cleanup = Shared.NativeWormAPI.worm_cleanup,
            func_worm_info_new = Shared.NativeWormAPI.worm_info_new,
            func_worm_info_free = Shared.NativeWormAPI.worm_info_free,
            func_worm_info_read = Shared.NativeWormAPI.worm_info_read,
            func_worm_info_customizationIdentifier = Shared.NativeWormAPI.worm_info_customizationIdentifier,
            func_worm_info_isDevelopmentFirmware = Shared.NativeWormAPI.worm_info_isDevelopmentFirmware,
            func_worm_info_capacity = Shared.NativeWormAPI.worm_info_capacity,
            func_worm_info_size = Shared.NativeWormAPI.worm_info_size,
            func_worm_info_hasValidTime = Shared.NativeWormAPI.worm_info_hasValidTime,
            func_worm_info_hasPassedSelfTest = Shared.NativeWormAPI.worm_info_hasPassedSelfTest,
            func_worm_info_isCtssInterfaceActive = Shared.NativeWormAPI.worm_info_isCtssInterfaceActive,
            func_worm_info_isExportEnabledIfCspTestFails = Shared.NativeWormAPI.worm_info_isExportEnabledIfCspTestFails,
            func_worm_info_initializationState = Shared.NativeWormAPI.worm_info_initializationState,
            func_worm_info_isDataImportInProgress = Shared.NativeWormAPI.worm_info_isDataImportInProgress,
            func_worm_info_hasChangedPuk = Shared.NativeWormAPI.worm_info_hasChangedPuk,
            func_worm_info_hasChangedAdminPin = Shared.NativeWormAPI.worm_info_hasChangedAdminPin,
            func_worm_info_hasChangedTimeAdminPin = Shared.NativeWormAPI.worm_info_hasChangedTimeAdminPin,
            func_worm_info_timeUntilNextSelfTest = Shared.NativeWormAPI.worm_info_timeUntilNextSelfTest,
            func_worm_info_startedTransactions = Shared.NativeWormAPI.worm_info_startedTransactions,
            func_worm_info_maxStartedTransactions = Shared.NativeWormAPI.worm_info_maxStartedTransactions,
            func_worm_info_createdSignatures = Shared.NativeWormAPI.worm_info_createdSignatures,
            func_worm_info_maxSignatures = Shared.NativeWormAPI.worm_info_maxSignatures,
            func_worm_info_remainingSignatures = Shared.NativeWormAPI.worm_info_remainingSignatures,
            func_worm_info_maxTimeSynchronizationDelay = Shared.NativeWormAPI.worm_info_maxTimeSynchronizationDelay,
            func_worm_info_maxUpdateDelay = Shared.NativeWormAPI.worm_info_maxUpdateDelay,
            func_worm_info_tsePublicKey = Shared.NativeWormAPI.worm_info_tsePublicKey,
            func_worm_info_tseSerialNumber = Shared.NativeWormAPI.worm_info_tseSerialNumber,
            func_worm_info_tseDescription = Shared.NativeWormAPI.worm_info_tseDescription,
            func_worm_info_registeredClients = Shared.NativeWormAPI.worm_info_registeredClients,
            func_worm_info_maxRegisteredClients = Shared.NativeWormAPI.worm_info_maxRegisteredClients,
            func_worm_info_certificateExpirationDate = Shared.NativeWormAPI.worm_info_certificateExpirationDate,
            func_worm_info_tarExportSizeInSectors = Shared.NativeWormAPI.worm_info_tarExportSizeInSectors,
            func_worm_info_tarExportSize = Shared.NativeWormAPI.worm_info_tarExportSize,
            func_worm_info_hardwareVersion = Shared.NativeWormAPI.worm_info_hardwareVersion,
            func_worm_info_softwareVersion = Shared.NativeWormAPI.worm_info_softwareVersion,
            func_worm_info_formFactor = Shared.NativeWormAPI.worm_info_formFactor,
            func_worm_flash_health_summary = Shared.NativeWormAPI.worm_flash_health_summary,
            func_worm_flash_health_needs_replacement = Shared.NativeWormAPI.worm_flash_health_needs_replacement,
            func_worm_tse_factoryReset = Shared.NativeWormAPI.worm_tse_factoryReset,
            func_worm_tse_setup = Shared.NativeWormAPI.worm_tse_setup,
            func_worm_tse_ctss_enable = Shared.NativeWormAPI.worm_tse_ctss_enable,
            func_worm_tse_ctss_disable = Shared.NativeWormAPI.worm_tse_ctss_disable,
            func_worm_tse_initialize = Shared.NativeWormAPI.worm_tse_initialize,
            func_worm_tse_decommission = Shared.NativeWormAPI.worm_tse_decommission,
            func_worm_tse_updateTime = Shared.NativeWormAPI.worm_tse_updateTime,
            func_worm_transaction_openStore = Shared.NativeWormAPI.worm_transaction_openStore,
            func_worm_tse_firmwareUpdate_transfer = Shared.NativeWormAPI.worm_tse_firmwareUpdate_transfer,
            func_worm_tse_firmwareUpdate_apply = Shared.NativeWormAPI.worm_tse_firmwareUpdate_apply,
            func_worm_tse_firmwareUpdate_isBundledAvailable = Shared.NativeWormAPI.worm_tse_firmwareUpdate_isBundledAvailable,
            func_worm_tse_firmwareUpdate_applyBundled = Shared.NativeWormAPI.worm_tse_firmwareUpdate_applyBundled,
            func_worm_tse_enableExportIfCspTestFails = Shared.NativeWormAPI.worm_tse_enableExportIfCspTestFails,
            func_worm_tse_disableExportIfCspTestFails = Shared.NativeWormAPI.worm_tse_disableExportIfCspTestFails,
            func_worm_tse_runSelfTest = Shared.NativeWormAPI.worm_tse_runSelfTest,
            func_worm_tse_registerClient = Shared.NativeWormAPI.worm_tse_registerClient,
            func_worm_tse_deregisterClient = Shared.NativeWormAPI.worm_tse_deregisterClient,
            func_worm_tse_listRegisteredClients = Shared.NativeWormAPI.worm_tse_listRegisteredClients,
            func_worm_user_login = Shared.NativeWormAPI.worm_user_login,
            func_worm_user_logout = Shared.NativeWormAPI.worm_user_logout,
            func_worm_user_unblock = Shared.NativeWormAPI.worm_user_unblock,
            func_worm_user_change_puk = Shared.NativeWormAPI.worm_user_change_puk,
            func_worm_user_change_pin = Shared.NativeWormAPI.worm_user_change_pin,
            func_worm_user_deriveInitialCredentials = Shared.NativeWormAPI.worm_user_deriveInitialCredentials,
            func_worm_transaction_start = Shared.NativeWormAPI.worm_transaction_start,
            func_worm_transaction_update = Shared.NativeWormAPI.worm_transaction_update,
            func_worm_transaction_finish = Shared.NativeWormAPI.worm_transaction_finish,
            func_worm_transaction_lastResponse = Shared.NativeWormAPI.worm_transaction_lastResponse,
            func_worm_transaction_listStartedTransactions = Shared.NativeWormAPI.worm_transaction_listStartedTransactions,
            func_worm_transaction_response_new = Shared.NativeWormAPI.worm_transaction_response_new,
            func_worm_transaction_response_free = Shared.NativeWormAPI.worm_transaction_response_free,
            func_worm_transaction_response_logTime = Shared.NativeWormAPI.worm_transaction_response_logTime,
            func_worm_transaction_response_serialNumber = Shared.NativeWormAPI.worm_transaction_response_serialNumber,
            func_worm_transaction_response_signatureCounter = Shared.NativeWormAPI.worm_transaction_response_signatureCounter,
            func_worm_transaction_response_signature = Shared.NativeWormAPI.worm_transaction_response_signature,
            func_worm_transaction_response_transactionNumber = Shared.NativeWormAPI.worm_transaction_response_transactionNumber,
            func_worm_entry_new = Shared.NativeWormAPI.worm_entry_new,
            func_worm_entry_free = Shared.NativeWormAPI.worm_entry_free,
            func_worm_entry_iterate_first = Shared.NativeWormAPI.worm_entry_iterate_first,
            func_worm_entry_iterate_last = Shared.NativeWormAPI.worm_entry_iterate_last,
            func_worm_entry_iterate_id = Shared.NativeWormAPI.worm_entry_iterate_id,
            func_worm_entry_iterate_next = Shared.NativeWormAPI.worm_entry_iterate_next,
            func_worm_entry_isValid = Shared.NativeWormAPI.worm_entry_isValid,
            func_worm_entry_id = Shared.NativeWormAPI.worm_entry_id,
            func_worm_entry_type = Shared.NativeWormAPI.worm_entry_type,
            func_worm_entry_logMessageLength = Shared.NativeWormAPI.worm_entry_logMessageLength,
            func_worm_entry_readLogMessage = Shared.NativeWormAPI.worm_entry_readLogMessage,
            func_worm_entry_processDataLength = Shared.NativeWormAPI.worm_entry_processDataLength,
            func_worm_entry_readProcessData = Shared.NativeWormAPI.worm_entry_readProcessData,
            func_worm_getLogMessageCertificate = Shared.NativeWormAPI.worm_getLogMessageCertificate,
            func_worm_export_tar = Shared.NativeWormAPI.worm_export_tar,
            func_worm_export_tar_incremental = Shared.NativeWormAPI.worm_export_tar_incremental,
            func_worm_export_tar_filtered_time = Shared.NativeWormAPI.worm_export_tar_filtered_time,
            func_worm_export_tar_filtered_transaction = Shared.NativeWormAPI.worm_export_tar_filtered_transaction,
            func_worm_export_deleteStoredData = Shared.NativeWormAPI.worm_export_deleteStoredData,
        };
    }
}