using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using static fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.NativeFunctionPointer;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.DynamicLib
{
    public class FunctionPointerFactory : INativeFunctionPointerFactory
    {

        private readonly INativeLibrary nativeLibraryHandler;
        private readonly IntPtr libraryPtr;

        private const string win32LibraryFile = "runtimes\\win-x86\\native\\WormAPI.dll";
        private const string win64LibraryFile = "runtimes\\win-x64\\native\\WormAPI.dll";
        private const string linux32LibraryFile = "runtimes/linux/native/libWormAPI.so";
        private const string linux64LibraryFile = "runtimes/linux-x64/native/libWormAPI.so";
        private const string linuxArm32LibraryFile = "runtimes/linux-arm/native/libWormAPI.so";

        public FunctionPointerFactory(string libraryFile = null)
        {
            var arch = RuntimeInformation.ProcessArchitecture;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                {
                    nativeLibraryHandler = new LinuxNativeLibrary();
                    if (string.IsNullOrEmpty(libraryFile))
                    {
                        libraryFile = arch switch
                        {
                            Architecture.X86 => linux32LibraryFile,
                            Architecture.X64 => linux64LibraryFile,
                            Architecture.Arm => linuxArm32LibraryFile,
                            Architecture.Arm64 => throw new NotImplementedException("Arm64 is currently not supported by the Swissbit hardware TSE SDK."),
                            _ => throw new NotImplementedException($"The CPU architecture {arch} is not supported on Linux by the Swissbit hardware TSE SDK.")
                        };
                    }
                };
                break;
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                default:
                {
                    nativeLibraryHandler = new WindowsNativeLibrary();
                    if (string.IsNullOrEmpty(libraryFile))
                    {
                        libraryFile = arch switch
                        {
                            Architecture.X86 => win32LibraryFile,
                            Architecture.X64 => win64LibraryFile,
                            _ => throw new NotImplementedException($"The CPU architecture {arch} is currently not supported on Windows by the Swissbit hardware TSE SDK.")
                        };
                    }
                };
                break;
            }

            //try to load by file
            libraryPtr = nativeLibraryHandler.Load(libraryFile);

            //try to add currentDirectory to file
            if (libraryPtr == IntPtr.Zero)
            {
                var currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
                libraryPtr = nativeLibraryHandler.Load(System.IO.Path.Combine(currentDirectory, libraryFile));
                if (libraryPtr == IntPtr.Zero)
                {
                    throw new NativeLibraryException($"error loading library from file {libraryFile}");
                }
            }

        }


        public NativeFunctionPointer LoadLibrary() => new NativeFunctionPointer
        {
            func_worm_getVersion = Marshal.GetDelegateForFunctionPointer<worm_getVersion>(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_getVersion))),
            func_worm_signatureAlgorithm = (worm_signatureAlgorithm) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_signatureAlgorithm)), typeof(worm_signatureAlgorithm)),
            func_worm_logTimeFormat = (worm_logTimeFormat) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_logTimeFormat)), typeof(worm_logTimeFormat)),
            func_worm_init = (worm_init) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_init)), typeof(worm_init)),
            func_worm_cleanup = (worm_cleanup) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_cleanup)), typeof(worm_cleanup)),
            func_worm_info_new = (worm_info_new) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_new)), typeof(worm_info_new)),
            func_worm_info_free = (worm_info_free) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_free)), typeof(worm_info_free)),
            func_worm_info_read = (worm_info_read) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_read)), typeof(worm_info_read)),
            func_worm_info_customizationIdentifier = (worm_info_customizationIdentifier) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_customizationIdentifier)), typeof(worm_info_customizationIdentifier)),
            func_worm_info_isDevelopmentFirmware = (worm_info_isDevelopmentFirmware) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_isDevelopmentFirmware)), typeof(worm_info_isDevelopmentFirmware)),
            func_worm_info_capacity = (worm_info_capacity) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_capacity)), typeof(worm_info_capacity)),
            func_worm_info_size = (worm_info_size) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_size)), typeof(worm_info_size)),
            func_worm_info_hasValidTime = (worm_info_hasValidTime) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hasValidTime)), typeof(worm_info_hasValidTime)),
            func_worm_info_hasPassedSelfTest = (worm_info_hasPassedSelfTest) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hasPassedSelfTest)), typeof(worm_info_hasPassedSelfTest)),
            func_worm_info_isCtssInterfaceActive = (worm_info_isCtssInterfaceActive) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_isCtssInterfaceActive)), typeof(worm_info_isCtssInterfaceActive)),
            func_worm_info_isExportEnabledIfCspTestFails = (worm_info_isExportEnabledIfCspTestFails) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_isExportEnabledIfCspTestFails)), typeof(worm_info_isExportEnabledIfCspTestFails)),
            func_worm_info_initializationState = (worm_info_initializationState) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_initializationState)), typeof(worm_info_initializationState)),
            func_worm_info_isDataImportInProgress = (worm_info_isDataImportInProgress) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_isDataImportInProgress)), typeof(worm_info_isDataImportInProgress)),
            func_worm_info_hasChangedPuk = (worm_info_hasChangedPuk) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hasChangedPuk)), typeof(worm_info_hasChangedPuk)),
            func_worm_info_hasChangedAdminPin = (worm_info_hasChangedAdminPin) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hasChangedAdminPin)), typeof(worm_info_hasChangedAdminPin)),
            func_worm_info_hasChangedTimeAdminPin = (worm_info_hasChangedTimeAdminPin) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hasChangedTimeAdminPin)), typeof(worm_info_hasChangedTimeAdminPin)),
            func_worm_info_timeUntilNextSelfTest = (worm_info_timeUntilNextSelfTest) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_timeUntilNextSelfTest)), typeof(worm_info_timeUntilNextSelfTest)),
            func_worm_info_startedTransactions = (worm_info_startedTransactions) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_startedTransactions)), typeof(worm_info_startedTransactions)),
            func_worm_info_maxStartedTransactions = (worm_info_maxStartedTransactions) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_maxStartedTransactions)), typeof(worm_info_maxStartedTransactions)),
            func_worm_info_createdSignatures = (worm_info_createdSignatures) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_createdSignatures)), typeof(worm_info_createdSignatures)),
            func_worm_info_maxSignatures = (worm_info_maxSignatures) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_maxSignatures)), typeof(worm_info_maxSignatures)),
            func_worm_info_remainingSignatures = (worm_info_remainingSignatures) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_remainingSignatures)), typeof(worm_info_remainingSignatures)),
            func_worm_info_maxTimeSynchronizationDelay = (worm_info_maxTimeSynchronizationDelay) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_maxTimeSynchronizationDelay)), typeof(worm_info_maxTimeSynchronizationDelay)),
            func_worm_info_maxUpdateDelay = (worm_info_maxUpdateDelay) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_maxUpdateDelay)), typeof(worm_info_maxUpdateDelay)),
            func_worm_info_tsePublicKey = (worm_info_tsePublicKey) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_tsePublicKey)), typeof(worm_info_tsePublicKey)),
            func_worm_info_tseSerialNumber = (worm_info_tseSerialNumber) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_tseSerialNumber)), typeof(worm_info_tseSerialNumber)),
            func_worm_info_tseDescription = (worm_info_tseDescription) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_tseDescription)), typeof(worm_info_tseDescription)),
            func_worm_info_registeredClients = (worm_info_registeredClients) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_registeredClients)), typeof(worm_info_registeredClients)),
            func_worm_info_maxRegisteredClients = (worm_info_maxRegisteredClients) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_maxRegisteredClients)), typeof(worm_info_maxRegisteredClients)),
            func_worm_info_certificateExpirationDate = (worm_info_certificateExpirationDate) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_certificateExpirationDate)), typeof(worm_info_certificateExpirationDate)),
            func_worm_info_tarExportSizeInSectors = (worm_info_tarExportSizeInSectors) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_tarExportSizeInSectors)), typeof(worm_info_tarExportSizeInSectors)),
            func_worm_info_tarExportSize = (worm_info_tarExportSize) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_tarExportSize)), typeof(worm_info_tarExportSize)),
            func_worm_info_hardwareVersion = (worm_info_hardwareVersion) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_hardwareVersion)), typeof(worm_info_hardwareVersion)),
            func_worm_info_softwareVersion = (worm_info_softwareVersion) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_softwareVersion)), typeof(worm_info_softwareVersion)),
            func_worm_info_formFactor = (worm_info_formFactor) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_info_formFactor)), typeof(worm_info_formFactor)),
            func_worm_flash_health_summary = (worm_flash_health_summary) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_flash_health_summary)), typeof(worm_flash_health_summary)),
            func_worm_flash_health_needs_replacement = (worm_flash_health_needs_replacement) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_flash_health_needs_replacement)), typeof(worm_flash_health_needs_replacement)),
            func_worm_tse_factoryReset = (worm_tse_factoryReset) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_factoryReset)), typeof(worm_tse_factoryReset)),
            func_worm_tse_setup = (worm_tse_setup) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_setup)), typeof(worm_tse_setup)),
            func_worm_tse_ctss_enable = (worm_tse_ctss_enable) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_ctss_enable)), typeof(worm_tse_ctss_enable)),
            func_worm_tse_ctss_disable = (worm_tse_ctss_disable) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_ctss_disable)), typeof(worm_tse_ctss_disable)),
            func_worm_tse_initialize = (worm_tse_initialize) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_initialize)), typeof(worm_tse_initialize)),
            func_worm_tse_decommission = (worm_tse_decommission) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_decommission)), typeof(worm_tse_decommission)),
            func_worm_tse_updateTime = (worm_tse_updateTime) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_updateTime)), typeof(worm_tse_updateTime)),
            func_worm_transaction_openStore = (worm_transaction_openStore) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_openStore)), typeof(worm_transaction_openStore)),
            func_worm_tse_firmwareUpdate_transfer = (worm_tse_firmwareUpdate_transfer) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_firmwareUpdate_transfer)), typeof(worm_tse_firmwareUpdate_transfer)),
            func_worm_tse_firmwareUpdate_isBundledAvailable = (worm_tse_firmwareUpdate_isBundledAvailable) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_firmwareUpdate_isBundledAvailable)), typeof(worm_tse_firmwareUpdate_isBundledAvailable)),
            func_worm_tse_firmwareUpdate_applyBundled = (worm_tse_firmwareUpdate_applyBundled) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_firmwareUpdate_applyBundled)), typeof(worm_tse_firmwareUpdate_applyBundled)),
            func_worm_tse_firmwareUpdate_apply = (worm_tse_firmwareUpdate_apply) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_firmwareUpdate_apply)), typeof(worm_tse_firmwareUpdate_apply)),
            func_worm_tse_enableExportIfCspTestFails = (worm_tse_enableExportIfCspTestFails) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_enableExportIfCspTestFails)), typeof(worm_tse_enableExportIfCspTestFails)),
            func_worm_tse_disableExportIfCspTestFails = (worm_tse_disableExportIfCspTestFails) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_disableExportIfCspTestFails)), typeof(worm_tse_disableExportIfCspTestFails)),
            func_worm_tse_runSelfTest = (worm_tse_runSelfTest) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_runSelfTest)), typeof(worm_tse_runSelfTest)),
            func_worm_tse_registerClient = (worm_tse_registerClient) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_registerClient)), typeof(worm_tse_registerClient)),
            func_worm_tse_deregisterClient = (worm_tse_deregisterClient) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_deregisterClient)), typeof(worm_tse_deregisterClient)),
            func_worm_tse_listRegisteredClients = (worm_tse_listRegisteredClients) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_tse_listRegisteredClients)), typeof(worm_tse_listRegisteredClients)),
            func_worm_user_login = (worm_user_login) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_login)), typeof(worm_user_login)),
            func_worm_user_logout = (worm_user_logout) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_logout)), typeof(worm_user_logout)),
            func_worm_user_unblock = (worm_user_unblock) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_unblock)), typeof(worm_user_unblock)),
            func_worm_user_change_puk = (worm_user_change_puk) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_change_puk)), typeof(worm_user_change_puk)),
            func_worm_user_change_pin = (worm_user_change_pin) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_change_pin)), typeof(worm_user_change_pin)),
            func_worm_user_deriveInitialCredentials = (worm_user_deriveInitialCredentials) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_user_deriveInitialCredentials)), typeof(worm_user_deriveInitialCredentials)),
            func_worm_transaction_start = (worm_transaction_start) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_start)), typeof(worm_transaction_start)),
            func_worm_transaction_update = (worm_transaction_update) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_update)), typeof(worm_transaction_update)),
            func_worm_transaction_finish = (worm_transaction_finish) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_finish)), typeof(worm_transaction_finish)),
            func_worm_transaction_lastResponse = (worm_transaction_lastResponse) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_lastResponse)), typeof(worm_transaction_lastResponse)),
            func_worm_transaction_listStartedTransactions = (worm_transaction_listStartedTransactions) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_listStartedTransactions)), typeof(worm_transaction_listStartedTransactions)),
            func_worm_transaction_response_new = (worm_transaction_response_new) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_new)), typeof(worm_transaction_response_new)),
            func_worm_transaction_response_free = (worm_transaction_response_free) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_free)), typeof(worm_transaction_response_free)),
            func_worm_transaction_response_logTime = (worm_transaction_response_logTime) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_logTime)), typeof(worm_transaction_response_logTime)),
            func_worm_transaction_response_serialNumber = (worm_transaction_response_serialNumber) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_serialNumber)), typeof(worm_transaction_response_serialNumber)),
            func_worm_transaction_response_signatureCounter = (worm_transaction_response_signatureCounter) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_signatureCounter)), typeof(worm_transaction_response_signatureCounter)),
            func_worm_transaction_response_signature = (worm_transaction_response_signature) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_signature)), typeof(worm_transaction_response_signature)),
            func_worm_transaction_response_transactionNumber = (worm_transaction_response_transactionNumber) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_transaction_response_transactionNumber)), typeof(worm_transaction_response_transactionNumber)),
            func_worm_entry_new = (worm_entry_new) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_new)), typeof(worm_entry_new)),
            func_worm_entry_free = (worm_entry_free) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_free)), typeof(worm_entry_free)),
            func_worm_entry_iterate_first = (worm_entry_iterate_first) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_iterate_first)), typeof(worm_entry_iterate_first)),
            func_worm_entry_iterate_last = (worm_entry_iterate_last) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_iterate_last)), typeof(worm_entry_iterate_last)),
            func_worm_entry_iterate_id = (worm_entry_iterate_id) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_iterate_id)), typeof(worm_entry_iterate_id)),
            func_worm_entry_iterate_next = (worm_entry_iterate_next) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_iterate_next)), typeof(worm_entry_iterate_next)),
            func_worm_entry_isValid = (worm_entry_isValid) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_isValid)), typeof(worm_entry_isValid)),
            func_worm_entry_id = (worm_entry_id) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_id)), typeof(worm_entry_id)),
            func_worm_entry_type = (worm_entry_type) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_type)), typeof(worm_entry_type)),
            func_worm_entry_logMessageLength = (worm_entry_logMessageLength) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_logMessageLength)), typeof(worm_entry_logMessageLength)),
            func_worm_entry_readLogMessage = (worm_entry_readLogMessage) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_readLogMessage)), typeof(worm_entry_readLogMessage)),
            func_worm_entry_processDataLength = (worm_entry_processDataLength) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_processDataLength)), typeof(worm_entry_processDataLength)),
            func_worm_entry_readProcessData = (worm_entry_readProcessData) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_entry_readProcessData)), typeof(worm_entry_readProcessData)),
            func_worm_getLogMessageCertificate = (worm_getLogMessageCertificate) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_getLogMessageCertificate)), typeof(worm_getLogMessageCertificate)),
            func_worm_export_tar = (worm_export_tar) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_export_tar)), typeof(worm_export_tar)),
            func_worm_export_tar_incremental = (worm_export_tar_incremental) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_export_tar_incremental)), typeof(worm_export_tar_incremental)),
            func_worm_export_tar_filtered_time = (worm_export_tar_filtered_time) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_export_tar_filtered_time)), typeof(worm_export_tar_filtered_time)),
            func_worm_export_tar_filtered_transaction = (worm_export_tar_filtered_transaction) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_export_tar_filtered_transaction)), typeof(worm_export_tar_filtered_transaction)),
            func_worm_export_deleteStoredData = (worm_export_deleteStoredData) Marshal.GetDelegateForFunctionPointer(nativeLibraryHandler.GetSymbolAddress(libraryPtr, nameof(worm_export_deleteStoredData)), typeof(worm_export_deleteStoredData)),
        };
    }
}
