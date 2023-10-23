using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop
{
    public class NativeFunctionPointer
    {
        //WORMAPI const char *WORMAPI_CALL worm_getVersion(void)
        //Returns the library version.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_getVersion();

        //WORMAPI const char* WORMAPI_CALL worm_signatureAlgorithm(void)
        //Returns the signature algorithm that is used by the TSE.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_signatureAlgorithm();

        //WORMAPI const char* WORMAPI_CALL worm_logTimeFormat(void)
        //Returns the log time format used by the TSE.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_logTimeFormat();

        //WORMAPI WormError WORMAPI_CALL worm_init(WormContext** context, const char* mountPoint)
        //Initializes the library by setting up a new context.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_init(ref IntPtr context, IntPtr mountPoint);

        //WORMAPI WormError WORMAPI_CALL worm_cleanup(WormContext* context)
        //Releases all allocated resources belonging to a context.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_cleanup(IntPtr context);

        /* Allocates a new WormInfo.
            * 
        The returned object will not contain any data until
        @ref worm_info_read has been called.

        @param[in] context Library context
        @returns an allocated object, or `NULL` on errors
        @see @ref worm_info_free
        */
        //WORMAPI WormInfo *WORMAPI_CALL worm_info_new(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_info_new(IntPtr context);

        /* Frees a previously allocated WormInfo.

        The object MUST NOT be used anymore after this method returned.

        @param[in] info Info reference. Might be `NULL`.
        @see @ref worm_info_new
        */
        //WORMAPI void WORMAPI_CALL worm_info_free(WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_info_free(IntPtr info);

        /* Reads the TSE Status information from the TSE.

        The passed @p info must have been allocated by @ref worm_info_new
        and will be populated in place with the current TSE Status information.

        @param[in,out] info Info reference
        */
        //WORMAPI WormError WORMAPI_CALL worm_info_read(WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_info_read(IntPtr info);

        /* Retrieves the customization identifier of the TSE.

        @param[in] info Info reference
        @param[out] id The customization identifier.
                    The buffer is NOT `NULL` terminated, but exactly @p idLength bytes
                    long. Also, the buffer MUST NOT be freed by the caller and will be valid
                    until the @ref WormInfo parent gets freed.
        @param[out] idLength Length of @p id.
        */
        //WORMAPI void WORMAPI_CALL worm_info_customizationIdentifier(const WormInfo* info, const unsigned char** id, int* idLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_info_customizationIdentifier(IntPtr info, ref IntPtr id, IntPtr idLength);

        /* Returns whether the TSE is running a development firmware.

        @param[in] info Info reference
        @returns whether the TSE is running a development firmware
        */
        //WORMAPI int WORMAPI_CALL worm_info_isDevelopmentFirmware(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_isDevelopmentFirmware(IntPtr info);

        /* Returns the capacity of the TSE Store in blocks.

        @param[in] info Info reference
        @returns the TSE Store capacity
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_capacity(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_capacity(IntPtr info);

        /* Returns the currently used size of the TSE Store in blocks.

        Only readable if the CTSS interface is active, otherwise 0.

        @param[in] info Info reference
        @returns the currently used TSE Store size
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_size(const WormInfo* info);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_size(IntPtr info);

        /* Returns whether a valid time is set in the TSE.

        @param[in] info Info reference
        @returns whether a valid time is set
        @see @ref worm_tse_updateTime
        */
        //WORMAPI int WORMAPI_CALL worm_info_hasValidTime(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_hasValidTime(IntPtr info);

        /* Returns whether the TSE passed its self-test.

        @param[in] info Info reference
        @returns whether the self-test has been passed
        @see @ref worm_tse_runSelfTest
        */
        //WORMAPI int WORMAPI_CALL worm_info_hasPassedSelfTest(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_hasPassedSelfTest(IntPtr info);

        /* Returns whether the CTSS interface is active.

        This will only be true, if the CTSS interface has been enabled and the
        self test has been successful.

        @param[in] info Info reference
        @returns whether the self-test has been passed
        @see @ref worm_tse_runSelfTest
        @see @ref worm_tse_ctss_enable
        */
        //WORMAPI int WORMAPI_CALL worm_info_isCtssInterfaceActive(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_isCtssInterfaceActive(IntPtr info);

        /* Returns whether data export is enabled if the CSP test failed.

        @param[in] info Info reference
        @returns whether data export is enabled if the CSP test failed
        @see @ref worm_tse_enableExportIfCspTestFails
        @see @ref worm_tse_disableExportIfCspTestFails
        */
        //WORMAPI int WORMAPI_CALL worm_info_isExportEnabledIfCspTestFails(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_isExportEnabledIfCspTestFails(IntPtr info);

        /* Returns the initialization state of the TSE.

        @param[in] info Info reference
        @see @ref worm_tse_initialize
        @see @ref worm_tse_decommission
        */
        //WORMAPI WormInitializationState WORMAPI_CALL worm_info_initializationState(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormInitializationState worm_info_initializationState(IntPtr info);

        /* Returns whether a Data Import is currently in progress.

        @param[in] info Info reference
        @returns `1` if a Data Import is in progress, `0` otherwise
        */
        //WORMAPI int WORMAPI_CALL worm_info_isDataImportInProgress(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_isDataImportInProgress(IntPtr info);

        /* Returns whether the initial PUK has been changed.

        @param[in] info Info reference
        */
        //WORMAPI int WORMAPI_CALL worm_info_hasChangedPuk(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_hasChangedPuk(IntPtr info);

        /* Returns whether the initial *Admin* PIN has been changed.

        @param[in] info Info reference
        */
        //WORMAPI int WORMAPI_CALL worm_info_hasChangedAdminPin(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_hasChangedAdminPin(IntPtr info);





        /* Returns whether the initial *TimeAdmin* PIN has been changed.

        @param[in] info Info reference
        */
        //WORMAPI int WORMAPI_CALL worm_info_hasChangedTimeAdminPin(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_info_hasChangedTimeAdminPin(IntPtr info);


        /* Returns the timeout in seconds after which the next self test must be run.

        If this reaches 0, all following commands will fail until the self test is
        executed again.

        @param[in] info Info reference
        @returns the timeout
        @see @ref worm_tse_runSelfTest
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_timeUntilNextSelfTest(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_timeUntilNextSelfTest(IntPtr info);

        /* Returns the number of started transactions.

        Only readable if the CTSS interface is active, otherwise 0.

        If this is equal to @ref worm_info_maxStartedTransactions, no new transactions
        can be started until at least one already started transaction has been finished.
        To finish started transactions, query their transaction numbers with
        @ref worm_transaction_listStartedTransactions and then finish them with
        @ref worm_transaction_finish.

        @param[in] info Info reference
        @returns the number of started transactions
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_startedTransactions(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_startedTransactions(IntPtr info);

        /* Returns the maximum number of transactions that can be started
            simultaneously, i.e. the maximum number of parallel transactions.

        @param[in] info Info reference
        @returns the maximum number of parallel transactions
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_maxStartedTransactions(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_maxStartedTransactions(IntPtr info);

        /* Returns the number of signatures that have been performed with this
            TSE.

        This value might get bigger than @ref worm_info_maxSignatures, since
        the latter is only a soft-cap and it might actually be possible to
        perform more signatures.

        @param[in] info Info reference
        @returns number of created signatures
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_createdSignatures(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_createdSignatures(IntPtr info);

        /* Returns the maximum number of signatures that can be performed with this
            TSE.

        @param[in] info Info reference
        @returns the maximum number of signatures
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_maxSignatures(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_maxSignatures(IntPtr info);


        /* Returns the number of remaining signatures that can be performed with this
            TSE.

        If this value reaches 0, no transactions are possible anymore and the TSE
        should be decommissioned.

        @param[in] info Info reference
        @returns number of remaining signatures
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_remainingSignatures(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_remainingSignatures(IntPtr info);

        /* Returns the interval in seconds at which @ref worm_tse_updateTime
            must be called.

        @param[in] info Info reference
        @returns the interval in seconds at which @ref worm_tse_updateTime
                    must be called
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_maxTimeSynchronizationDelay(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_maxTimeSynchronizationDelay(IntPtr info);


        /* Returns the interval in seconds after which a started transaction
        must be updated in case new data regarding this transaction is available.

        This is the value defined as `MAX_UPDATE_DELAY` in [BSI TR-03153](https://www
        .bsi.bund.de/DE/Publikationen/TechnischeRichtlinien/tr03153/index_htm.html).

        @param[in] info Info reference
        @returns the interval in seconds after which a transaction must be updated
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_maxUpdateDelay(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_maxUpdateDelay(IntPtr info);

        /* Retrieves the TSE's public key.

        Returns the public key that belongs to the private key generating signatures,
        formatted according to [BSI TR-03111](https://www.bsi.bund.de/DE/Publikationen/
        TechnischeRichtlinien/tr03111/index_htm.html] 3.2.1 Uncompressed Encoding.

        This key can be used to verify all signatures created by the TSE.

        @param[in] info Info reference
        @param[out] publicKey The public key.
                    The buffer is NOT `NULL` terminated, but exactly
                    @p publicKeyLength bytes long. Also, the buffer MUST NOT be freed by the
                    caller and will be valid until the @ref WormInfo parent gets freed.
        @param[out] publicKeyLength Length of @p publicKey.
        */
        //WORMAPI void WORMAPI_CALL worm_info_tsePublicKey(const WormInfo* info, const unsigned char** publicKey,worm_uint *publicKeyLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_info_tsePublicKey(IntPtr info, ref IntPtr publicKey, IntPtr publicKeyLength);


        /* Retrieves the serial number of the TSE.

        This is a hash over the public key that is used to generate signatures.

        @param[in] info Info reference
        @param[out] serialNumber The serial number.
                    The buffer is NOT `NULL` terminated, but exactly
                    @p serialNumberLength bytes long. Also, the buffer MUST NOT be freed by the
                    caller and will be valid until the @ref WormInfo parent gets freed.
        @param[out] serialNumberLength Length of @p serialNumber.
        */
        //WORMAPI void WORMAPI_CALL worm_info_tseSerialNumber(const WormInfo* info, const unsigned char** serialNumber,worm_uint *serialNumberLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_info_tseSerialNumber(IntPtr info, ref IntPtr serialNumber, IntPtr serialNumberLength);

        /* Returns the TSE's description.

        The TSE description is the TR-03153 certificate id, which is also called
        the "Art der zertifizierten technischen Sicherheitseinrichtung", which is
        needed for registering the TSE at fiscal authorities.

        The returned string MUST NOT be freed by the caller and will be valid
        until the @ref WormInfo parent gets freed.

        @param[in] info Info reference
        @returns the description of the TSE as a NULL terminated string
        */
        //WORMAPI const char* WORMAPI_CALL worm_info_tseDescription(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_info_tseDescription(IntPtr info);

        /* Returns the number of registered clients.

        @param[in] info Info reference
        @returns the number of registered clients
        @see @ref worm_tse_registerClient
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_registeredClients(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_registeredClients(IntPtr info);

        /* Returns the number of maximum registered clients.

        @param[in] info Info reference
        @returns the number of maximum registered clients
        @see @ref worm_tse_registerClient
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_maxRegisteredClients(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_maxRegisteredClients(IntPtr info);

        /* Returns the certificate's expiration date.

        This is the timestamp (as seconds since Unix Epoch) after which the
        certificate of this TSE will be invalid. Afterwards, no new
        Log Messages can be signed, but already stored Log Messages can still be
        exported (access control restrictions are still applied).

        @param[in] info Info reference
        @returns the certificate's expiration date
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_info_certificateExpirationDate(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_info_certificateExpirationDate(IntPtr info);

        /* Returns the size of the TSE Store when it gets exported as TAR in sectors.

        To get the actual size in bytes, this value must be multiplied with 512.

        Only readable if the CTSS interface is active, otherwise 0.

        @param[in] info Info reference
        @returns the export size in sectors
        @see @ref worm_export_tar
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_info_tarExportSizeInSectors(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_info_tarExportSizeInSectors(IntPtr info);


        /** Returns the size of the TSE Store when it gets exported as TAR in bytes.

        Only readable if the CTSS interface is active, otherwise 0.

        @param[in] info Info reference
        @returns the export size in bytes
        @see @ref worm_export_tar
        @note This method does not work for stdcall calling convention, use
                    @ref worm_info_tarExportSizeInSectors instead.
        */
        //WORMAPI uint64_t worm_info_tarExportSize(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_info_tarExportSize(IntPtr info);

        /* Returns the TSE's hardware version.

        The version is split into three parts:
        - 2 bytes major (highest bits)
        - 1 byte minor
        - 1 byte patch (lowest bits)

        @param[in] info Info reference
        @returns the hardware version
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_hardwareVersion(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_hardwareVersion(IntPtr info);

        /* Returns the TSE's software version.

        The version is split into three parts:
        - 2 bytes major (highest bits)
        - 1 byte minor
        - 1 byte patch (lowest bits)

        @param[in] info Info reference
        @returns the software version
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_info_softwareVersion(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_info_softwareVersion(IntPtr info);

        /* Returns the TSE's form factor.

        This will be either "SD", "uSD", or "USB".

        @param[in] info Info reference
        @returns the form factor
        */
        //WORMAPI const char* WORMAPI_CALL worm_info_formFactor(const WormInfo* info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_info_formFactor(IntPtr info);


        /* Returns the flash health summary.

        This command can be used to monitor the flash storage health and detect
        possible future defects before they occurr and apply predective maintenance.

        As a recommendation, the following simple guidance is provided:

                    1. If @p uncorrectableEccErrors is different from 0, the TSE should be
                        replaced.
                    2. If @p percentageRemainingSpareBlocks gets below 25%, the TSE
                        should be replaced.
                    3. If @p percentageRemainingEraseCounts gets below 2%, the TSE
                        should be replaced.

        Please note that based on the use case of the TSE, which does not involve
        many flash read or write operations compared to other use cases, it is not
        expected that any of these conditions will ever be fulfilled during the
        lifetime of the TSE.

        @ref worm_flash_health_needs_replacement should be used as a helper method to
        decide whether the TSE needs a replacement. The values returned by
        @ref worm_flash_health_summary should only be used to show a percentage
        indicator to the end user in order to easily visualize the flash health.

        The output parameter @p percentageRemainingTenYearsDataRetention is only of
        interest if the ERS does not perform regular backups of the TSE data.
        In case no backups are performed, the TSE itself guarantees ten years data
        retention unless this value reaches 0.
        If regular backups are performed, this value can be ignored as the TSE will
        guarantee at least 1 year data retention after this value reaches 0.

        @param[in] context Library context
        @param[out] uncorrectableEccErrors Number of uncorrectable ECC errors.
                    This should always be 0 during operation of the TSE and does not need
                    to be displayed to the user. If it is different from 0, it indicates a
                    hardware fault and the TSE should be immediately replaced.
        @param[out] percentageRemainingSpareBlocks Percentage of remaining spare blocks.
                    This will be a value between 0 and 100.
        @param[out] percentageRemainingEraseCounts Percentage of remaining erase counts.
                    This will be a value between 0 and 100.
        @param[out] percentageRemainingTenYearsDataRetention Percentage of remaining
                    erase counts until the ten year data retention can not be guaranteed
                    anymore. This will be a value between 0 and 100.
        */
        //WORMAPI WormError WORMAPI_CALL worm_flash_health_summary(
        //    WormContext* context, uint32_t* uncorrectableEccErrors,
        //    uint8_t* percentageRemainingSpareBlocks,
        //    uint8_t* percentageRemainingEraseCounts,
        //    uint8_t* percentageRemainingTenYearsDataRetention);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_flash_health_summary(
            IntPtr context, IntPtr uncorrectableEccErrors,
            IntPtr percentageRemainingSpareBlocks,
            IntPtr percentageRemainingEraseCounts,
            IntPtr percentageRemainingTenYearsDataRetention);

        /* Returns whether the TSE should be replaced with a new one based on the
            current flash health.

        @param[in] uncorrectableEccErrors Obtained from @ref worm_flash_health_summary.
        @param[in] percentageRemainingSpareBlocks  Obtained from @ref
                    worm_flash_health_summary.
        @param[in] percentageRemainingEraseCounts  Obtained from @ref
                    worm_flash_health_summary.
        @returns whether the TSE should be replaced
        @see @ref worm_flash_health_summary
        */
        //WORMAPI int WORMAPI_CALL worm_flash_health_needs_replacement(
        //    uint32_t uncorrectableEccErrors, uint8_t percentageRemainingSpareBlocks,
        //    uint8_t percentageRemainingEraseCounts);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_flash_health_needs_replacement(
            UInt32 uncorrectableEccErrors, byte percentageRemainingSpareBlocks,
            byte percentageRemainingEraseCounts);


        /* Resets the TSE to factory default.

        Afterwards, the TSE will be uninitialized, the TSE Store will
        be empty, and the PUK and all user PINs will be reset to factory default.
        @note This method only works on development TSEs and will be removed from the
                    final product.

        @param[in] context Library context
        @see @ref worm_info_isDevelopmentFirmware
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_factoryReset(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_factoryReset(IntPtr context);

        /* Performs all necessary actions to bring the TSE from a factory default state
            into an operational state.

        This is just a helper method that:
        - changes the PUK (@ref worm_user_change_puk)
        - changes all user PINs (@ref worm_user_change_pin)
        - registers the first client (@ref worm_tse_registerClient)
        - runs the self test (@ref worm_tse_runSelfTest)
        - enables the CTSS interface (@ref worm_tse_ctss_enable)
        - enables data export if the CSP test fails
                    (@ref worm_tse_enableExportIfCspTestFails)
        - initializes the TSE (@ref worm_tse_initialize)

        If any of these steps fails, this method will transparently return the error.
        In case this was a transient error (e.g. because the TSE was removed from the
        system during execution of the above steps), this method can be called again
        and it will continue on the step that previously failed (e.g. if the PUK
        was already changed, it will not be changed again, but all following steps
        will be performed).
        To allow this mechanism to work correctly, this method must always be called
        with the exact same arguments as on the first call (i.e. the same
        credential seed, Admin PIN, Admin PUK, and TimeAdmin PIN). Otherwise, the TSE
        might be configured in an inconsistent state, which could lead to a bricked
        TSE, if e.g. the previous PUK value is not known anymore.

        After this method returns successfully, users *Admin* and <em>Time Admin</em>
        will be logged in and more clients can be registered with
        @ref worm_tse_registerClient.
        For performing transactions, the only thing left to do is to call
        @ref worm_tse_updateTime and then starting the first transaction with
        @ref worm_transaction_start.

        @note Before running this method, the self test must have already been executed
                    at least once (which will fail, since the TSE is not setup correctly, yet).

        @param[in] context Library context
        @param[in] credentialSeed Seed to derive initial credentials from.
        @param[in] credentialSeedLength Length of @p credentialSeed.
        @param[in] adminPuk Admin PUK to set.
        @param[in] adminPukLength Length of @p adminPuk.
        @param[in] adminPin Admin PIN to set.
        @param[in] adminPinLength Length of @p adminPin.
        @param[in] timeAdminPin Admin PIN to set.
        @param[in] timeAdminPinLength Length of @p timeAdminPin.
        @param[in] clientId Client ID of the machine where the TSE is plugged in.
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_setup(
        //    WormContext* context, const unsigned char* credentialSeed,
        //    int credentialSeedLength, const unsigned char* adminPuk,
        //    int adminPukLength, const unsigned char* adminPin,
        //    int adminPinLength, const unsigned char* timeAdminPin,
        //    int timeAdminPinLength, const char* clientId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_setup(
            IntPtr context,
            IntPtr credentialSeed, int credentialSeedLength,
            IntPtr adminPuk, int adminPukLength,
            IntPtr adminPin, int adminPinLength,
            IntPtr timeAdminPin, int timeAdminPinLength,
            IntPtr clientId);

        /* Enables the CTSS interface.

        By default, the CTSS interface is disabled, which means that no
        transactions can be performed and no data can be read from the
        TSE Store. In fact, all commands except of user authentication and self
        test commands will be rejected.
        This setting is persisted across power cycles.

        @note This method requires the user @e Admin to be logged in (see
                    @ref userauth).

        @param[in] context Library context
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_ctss_enable(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_ctss_enable(IntPtr context);


        /* Disables the CTSS Interface.

        When sending the TSE to maintenance, it is recommended to disable
        the CTSS interface in order to prevent reading the recorded
        transactions.
        This setting is persisted across power cycles.

        @note This method requires the user @e Admin to be logged in (see
                    @ref userauth).

        @param[in] context Library context
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_ctss_disable(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_ctss_disable(IntPtr context);

        /** Initializes the TSE in order to enable transactions.

        @note This method can only be issued once during the life time of
                    the TSE.
        @note This command requires an active CTSS interface and the user
                    *Admin* to be logged in (see @ref userauth).

        @param[in] context Library context
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_initialize(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_initialize(IntPtr context);

        /* Decommisions the TSE.

        Afterwards, no new transactions can be stored and most commands
        will be disabled as if the TSE is not initialized.

        Decommissioning is only allowed if there are no unfinished transaction,
        otherwise the command will fail.

        @note This command can not be undone except with a
                    @ref worm_tse_factoryReset "factory reset".
        @note This command requires an active CTSS interface and the user
                    *Admin* to be logged in (see @ref userauth).

        @param[in] context Library context
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_decommission(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_decommission(IntPtr context);

        /* Updates the time on the TSE.

        After each Power Cycle, the TSE Store is locked and no transactions
        are possible until the time of the host has been synchronized with
        the time of the TSE using this command.

        Depending on the accuracy of the internal clock, this command must
        also be repeated regularly to keep the host and TSE time synchronized.
        How often the time must be synchronized is announced in
        @ref worm_info_maxTimeSynchronizationDelay.

        @note It is strongly recommended to call this method at the frequency
                    announced in @ref worm_info_maxTimeSynchronizationDelay. Otherwise, the
                    TSE will enter a low power mode and will (with very low probability) return
                    an error code on the next attempt of calling this method (or any other
                    method that interacts with the CSP). After having received the error,
                    running the self test is required to remedy the situation.

        @note This command requires an active CTSS interface and the user
                    @e Admin or <em>Time Admin</em> to be logged in (see @ref userauth).

        @param[in] context Library context
        @param[in] timestamp The time to set as Unix Time (number of seconds since
                    Unix Epoch)
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_updateTime(WormContext* context, worm_uint timestamp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_updateTime(IntPtr context, UInt64 timestamp);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_openStore(IntPtr context, UInt64 timestamp);


        /* Transfers a new firmware to the TSE.

        Since a firmware update package might be quite big, it must be transfered to
        the TSE in multiple steps. To do so, this method must be called repeatedly with
        an increasing @p chunkOffset until all parts of the package have been sent
        to the TSE. Afterwards, call @ref worm_tse_firmwareUpdate_apply to apply the
        update.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).
        @param[in] context Library context
        @param[in] chunkOffset Offset in the firmware package where @p chunkData is
                    stored. Must be a multiple of 16.
        @param[in] chunkData Raw data of the current chunk.
        @param[in] chunkLength Size of @p chunkData in bytes. Must be a multiple of
                    16 and at most @ref WORM_TSE_FW_UPDATE_MAX_CHUNK_SIZE.
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_firmwareUpdate_transfer(
        //    WormContext* context, uint32_t chunkOffset, const unsigned char* chunkData,
        //    int chunkLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_firmwareUpdate_transfer(IntPtr context, UInt32 chunkOffset, IntPtr chunkData, int chunkLength);

        /** Checks whether a bundled firmware update is available for a specific TSE.

        A bundled firmware update is a firmware update that is embedded into this
        library, i.e. this method can be performed without an internet connection.
        The method will return the available firmware version in the @p fw output
        parameter. If no update is available @p fw is @ref WORM_FW_NONE.
        The returned firmware version can be used to notify the end user about the
        specific version that is available.
        Call @ref worm_tse_firmwareUpdate_applyBundled to update the TSE to the
        provided firmware version.

        This method might return different firmware update version numbers depending
        on the form factor and the currently installed software version of the given
        TSE.

        @param[in] context Library context
        @param[out] fw The firmware to which an update is available
        */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_firmwareUpdate_isBundledAvailable(IntPtr context, IntPtr fw);

        /** Applies the latest available bundled firmware update.

        If the firmware of the TSE is the same or more recent than the latest
        available firmware bundled with this library
        @ref WORM_ERROR_FWU_NOT_AVAILABLE is returned.
        You can call @ref worm_tse_firmwareUpdate_isBundledAvailable to check whether
        a bundled firmware update is available before calling this method to prevent
        accidentally installing a firmware update (which might take multiple minutes)
        without notifying the end user beforehand.

        Note that the latest available firmware for a specific TSE is not always
        the latest bundled firmware since an incremental update might be
        necessary. E.g. if a TSE has firmware v1.0.0 a direct update to v3.0.0 might
        not be possible because the TSE must be updated to v2.0.0 first.
        In that case this method must be called multiple times as long as
        @ref worm_tse_firmwareUpdate_isBundledAvailable returns an available
        firmware update.

        This method internally uses @ref worm_tse_firmwareUpdate_apply, so please
        refer to this method for further details about the update procedure and
        things to consider before and after the update.

        @note This command requires the user *Admin* to be logged in (see @ref
        userauth).
        @param[in] context Library context
        */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_firmwareUpdate_applyBundled(IntPtr context);

        /* Applies a firmware update.

        The firmware will be checked for authenticity and integrity before being
        applied.

        The new firmware must have been transferred to the TSE with
        @ref worm_tse_firmwareUpdate_transfer before calling this method.

        On a successful firmware update, the TSE will reboot itself internally.
        Depending on the host, this might interrupt the communication and the library
        context must be freed and recreated from scratch.
        In that case, the error code of this command can not be trusted.

        In any case, it is recommended to check the current
        @ref worm_info_softwareVersion "TSE Software Version"
        before executing this command and compare it with the value after running this
        command to detect if the version has changed and the update has thus been
        applied successfully.

        Please note that this is a long running operation, which usually takes up to
        60 seconds to complete. It must be ensured that there is no power loss while
        applying the firmware update, as this might brick the device and make it
        unusable. Therefore it is recommended to export all data before applying a
        firmware update.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).
        @param[in] context Library context
        @param[in] fwSize Size of the firmware update package (in bytes) that has been
                    sent with @ref worm_tse_firmwareUpdate_transfer.
        @see @ref worm_info_softwareVersion
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_firmwareUpdate_apply(WormContext* context, uint32_t fwSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_firmwareUpdate_apply(IntPtr context, UInt32 fwSize);

        /* Enables Data Export if the CSP test fails.

        The TSE allows to determine the behavior of the TSE with
        respect to the export of data if the CSP test fails during the self test.

        By default, no data can be exported anymore if the CSP test fails due
        to a broken security module.
        To allow data to be exportable if the CSP test fails, this command can be used.
        Please note that this command will only allow to do a complete export (see
        @ref worm_export_tar) of the tar archive; the filtered export commands
        will still be disabled. Also, all commands of the
        TSE that require a successful self test for their execution will still be
        inaccessible.

        This command can only be used while the CSP self test is still passing.
        As soon as the test fails, it is too late to change this setting and
        this command will fail.

        This setting is persisted across power cycles.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).
        @note This command requires an active CTSS interface.
        @param[in] context Library context
        @see @ref worm_tse_disableExportIfCspTestFails
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_enableExportIfCspTestFails(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_enableExportIfCspTestFails(IntPtr context);

        /* Disables Data Export if the CSP test fails.

        The TSE allows to determine the behavior of the TSE with
        respect to the export of data if the CSP test fails during the self test.

        To disable the functionality of export and prevent any data from being exported
        if the CSP test fails, use this command.
        Please note that in case of a broken CSP, the data on the TSE is then
        lost and can not be recovered.
        This is the factory default behavior.

        This setting can only be changed while the CSP test is still passing.
        As soon as the test fails, it is too late to change this setting and
        this command will fail.

        This setting is persisted across power cycles.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).
        @note This command requires an active CTSS interface.
        @param[in] context Library context
        @see @ref worm_tse_enableExportIfCspTestFails
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_disableExportIfCspTestFails(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_disableExportIfCspTestFails(IntPtr context);

        /* Runs the self test of the TSE.

        After each power cycle, the TSE runs a self test to ensure proper operation of
        its internal modules. The self test requires to check that the TSE is plugged
        into an authorized system.

        The self test can be repeated whenever it is desired by the ERS, but it
        must be run at least once every 25 hours. Otherwise, the TSE will set the
        state *selfTestRun* to inactive, which makes all future commands
        fail until the self test is run successfully again.
        The time until the *selfTestRun* state will be made inactive can be
        obtained with @ref worm_info_timeUntilNextSelfTest.

        The self test is a potentially long running operation that might take up to 60
        seconds to complete.

        @note This command must be sent as first command after the TSE boots. If it
                    fails, only methods regarding user authentication, registering new
                    clients, or re-running the self test are allowed.

        @note It is strongly recommended that after running the self test, any methods
                    that might be called (e.g. @ref worm_user_login) are executed as soon as
                    possible afterwards. Especially, it should be ensured that
                    @ref worm_tse_updateTime is called quickly after running the self test to
                    prevent the TSE from entering a low power state. While having a valid time
                    set, the TSE will not enter the low power state again.

        @param[in] context Library context
        @param[in] clientId Serial number of the system where the TSE is physically
                    plugged in. Must have been registered previously with
                    @ref worm_tse_registerClient, otherwise the self test will fail.
        @see @ref worm_tse_registerClient
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_runSelfTest(WormContext* context, const char* clientId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_runSelfTest(IntPtr context, IntPtr clientId);

        /* Registers a client (i.e. an ERS) as a valid system for self tests and
            transactions.

        A client is identified by its ID, which shall be a unique string (e.g. its
        serial number). If the same client is already registered, the command will
        be successful, but the client will not be registered twice.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).

        @param[in] context Library context
        @param[in] clientId Client to register. Maximum length is 30 bytes.
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_registerClient(WormContext* context, const char* clientId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_registerClient(IntPtr context, IntPtr clientId);

        /* Removes a client from the list of authorized clients.

        In case the client is not registered, this command will fail.

        Before a client can be deregistered, all transactions that have been started by
        this client must be finished first, otherwise the command will fail.

        @note This command requires the user *Admin* to be logged in
                    (see @ref userauth).

        @param[in] context Library context
        @param[in] clientId Client to deregister.
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_deregisterClient(WormContext* context, const char* clientId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_deregisterClient(IntPtr context, IntPtr clientId);

        /** Data structure to hold the output of @ref worm_tse_listRegisteredClients. */
        //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        //public struct WormRegisteredClients
        //{
        //    /** Amount of client ids stored in clientIds. */
        //    internal int amount;
        //    /** List of NULL terminated client ids. */
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16 * 31)]
        //    internal char[] clientIds;
        //}

        /* Lists all registered clients.

        The maximum number of clients that can be retrieved with one call is 16.
        If further clients should be retrieved, this method must be called
        with an increasing @p toSkip value, e.g. a @p toSkip value
        of 16 will return the registered clients #16 to #31.

        @note This command requires the user *Admin* to be logged in
            (see @ref userauth).

        @param[in] context Library context
        @param[in] toSkip Number of clients to skip. Use 0 to not skip
            any clients, but return the first (maximum 16) registered clients.
        @param[out] clients Registered clients. Must be allocated by the caller.
        @see @ref worm_info_registeredClients
        @see @ref worm_info_maxRegisteredClients
        */
        //WORMAPI WormError WORMAPI_CALL worm_tse_listRegisteredClients(WormContext* context, int toSkip, WormRegisteredClients* clients);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_tse_listRegisteredClients(IntPtr context, int toSkip, IntPtr clients);


        /** Authenticates users on the TSE and enforces their permissions.

        Multiple users can be logged in at the same time. If multiple users with
        different privileges are logged in, the effective privileges are the union of
        all logged in users (e.g. if *Admin* and *TimeAdmin* are logged in, the time can
        be set and administrative commands can be sent).

        @note Before any user can be logged in, the initial PUK must have been changed
                    with @ref worm_user_change_puk.

        @note The initial PIN is random for each TSE and can be calculated with
                    @ref worm_user_deriveInitialCredentials.

        @param[in] context Library context
        @param[in] id User to log in
        @param[in] pin PIN of the user matching @p id
        @param[in] pinLength Length of @p pin
        @param[out] remainingRetries If this reaches 0, the user will be blocked and
                    must be @ref worm_user_unblock "unblocked". This value will only be set if
                    the authentication failed, otherwise it will be unchanged. Might be NULL.
        @see @ref worm_user_deriveInitialCredentials
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_login(WormContext* context,
        //                                           WormUserId id,
        //                                           const unsigned char* pin,
        //                                           int pinLength,
        //                                           int* remainingRetries);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_login(IntPtr context, WormUserId id, IntPtr pin, int pinLength, IntPtr remainingRetries);

        /** Logs the given user out of the TSE.

        If the specified user is not logged in, this method will fail.

        @param[in] context Library context
        @param[in] id User to log out
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_logout(WormContext* context, WormUserId id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_logout(IntPtr context, WormUserId id);

        /* Unblocks a user in case they were previously blocked, or simply changes
            the PIN in case they were not blocked.

        @note If a user’s initial PIN has not been changed, yet, this method will fail.
        @param[in] context Library context
        @param[in] id User to unblock
        @param[in] puk Current PUK.
        @param[in] pukLength Length of @p puk.
        @param[in] newPin New PIN. Must be different from the previous PIN.
        @param[in] newPinLength Length of @p newPin. Must be 5.
        @param[out] remainingRetries If this reaches 0, the PUK will be blocked.
                    This value will only be set if the authentication failed, otherwise it will
                    be unchanged. Might be NULL.
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_unblock(WormContext* context, WormUserId id, const unsigned char* puk,
        //              int pukLength, const unsigned char* newPin, int newPinLength,
        //              int* remainingRetries);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_unblock(IntPtr context, WormUserId id,
            IntPtr puk, int pukLength, IntPtr newPin, int newPinLength, IntPtr remainingRetries);

        /* Changes the *Admin* PUK.

        @note The PUK can be changed even if the user *Admin* is not logged in.
        @note The initial PUK is random for each TSE and can be calculated with
                    @ref worm_user_deriveInitialCredentials.

        @param[in] context Library context
        @param[in] puk Current PUK.
        @param[in] pukLength Length of @p puk.
        @param[in] newPuk New PUK. Must be different from the previous PUK.
        @param[in] newPukLength Length of @p newPuk. Must be 6.
        @param[out] remainingRetries If this reaches 0, the PUK will be blocked.
                    This value will only be set if the authentication failed, otherwise it will
                    be unchanged. Might be NULL.
        @see @ref worm_user_deriveInitialCredentials
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_change_puk(
        //WormContext* context, const unsigned char* puk, int pukLength,
        //const unsigned char* newPuk, int newPukLength, int* remainingRetries);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_change_puk(IntPtr context,
            IntPtr puk, int pukLength, IntPtr newPuk, int newPukLength, IntPtr remainingRetries);

        /* Changes the PIN of the given user.

        The user must have been logged in before with @ref worm_user_login.

        @param[in] context Library context
        @param[in] id User to change the PIN for
        @param[in] pin Current PIN
        @param[in] pinLength Length of @p pin
        @param[in] newPin New PIN. Must be different from the previous PIN.
        @param[in] newPinLength Length of @p newPin. Must be 5.
        @param[out] remainingRetries If this reaches 0, the user will be blocked and
                    must be @ref worm_user_unblock "unblocked". This value will only be set if
                    the authentication failed, otherwise it will be unchanged. Might be NULL.
        @see @ref worm_user_deriveInitialCredentials
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_change_pin(
        //WormContext* context, WormUserId id, const unsigned char* pin,
        //int pinLength, const unsigned char* newPin, int newPinLength,
        //int* remainingRetries);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_change_pin(IntPtr context, WormUserId id,
            IntPtr pin, int pinLength, IntPtr newPin, int newPinLength, IntPtr remainingRetries);

        /* Derives the initial credentials needed to set up the TSE.

        The initial credentials will only consist of ASCII digits between '0' and '9'
        and will be random for each individual TSE.

        @param[in] context Library context
        @param[in] seed Seed used to calculate the credential. This is the secret
                    that is used to derive the initial credentials during TSE production.
                    For Swissbit samples, this is "SwissbitSwissbit" (without quotes).
        @param[in] seedLength Length of @p seed. Must be at most 32 bytes.
        @param[out] initialAdminPuk Initial Admin PUK. Must be allocated by the caller.
        @param[in] initialAdminPukLength Length of @p initialAdminPuk. Must be 6.
        @param[out] initialAdminPin Initial Admin PIN. Must be allocated by the caller.
        @param[in] initialAdminPinLength Length of @p initialAdminPin. Must be 5.
        @param[out] initialTimeAdminPin Initial TimeAdmin PIN. Must be allocated by the
                    caller.
        @param[in] initialTimeAdminPinLength Length of @p initialTimeAdminPin.
                    Must be 5.
        @see @ref worm_user_change_pin
        @see @ref worm_user_change_puk
        */
        //WORMAPI WormError WORMAPI_CALL worm_user_deriveInitialCredentials(
        //WormContext* context, const unsigned char* seed, int seedLength,
        //unsigned char* initialAdminPuk, int initialAdminPukLength,
        //unsigned char* initialAdminPin, int initialAdminPinLength,
        //unsigned char* initialTimeAdminPin, int initialTimeAdminPinLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_user_deriveInitialCredentials(IntPtr context, IntPtr seed, int seedLength,
            IntPtr initialAdminPuk, int initialAdminPukLength,
            IntPtr initialAdminPin, int initialAdminPinLength,
            IntPtr initialTimeAdminPin, int initialTimeAdminPinLength);



        /* Starts a new transaction.

        Multiple transaction can be started at the same time.

        @note This command requires an active CTSS interface and a valid time on the
                TSE.

        @param[in] context Library context
        @param[in] clientId The id belonging to the client that issues the transaction.
                The client must have been previously registered with
                @ref worm_tse_registerClient before, otherwise the transaction will be
                rejected.
        @param[in] processData Data of the transaction
        @param[in] processDataLength Length of @p processData
        @param[in] processType Type of the transaction
        @param[out] response The response of the transaction
        */
        //WORMAPI WormError WORMAPI_CALL worm_transaction_start(
        //    WormContext* context, const char* clientId,
        //    const unsigned char* processData, worm_uint processDataLength,
        //    const char* processType, WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_start(IntPtr context, IntPtr clientId,
            IntPtr processData, UInt64 processDataLength,
            IntPtr processType, IntPtr response);

        /* Updates a transaction.

        Currently, only signed updates are supported, so each update will create a new
        signature.

        @note This command requires an active CTSS interface and a valid time on the
                    TSE.

        @param[in] context Library context
        @param[in] clientId The id belonging to the client that issues the transaction.
                    The client must have been previously registered with
                    @ref worm_tse_registerClient before, otherwise the transaction will be
                    rejected. The same client must have opened the transaction belonging to
                    @p transactionNumber, otherwise the update will be rejected.
        @param[in] transactionNumber A transaction number from a previous response of
                    @ref worm_transaction_start
        @param[in] processData Data of the transaction
        @param[in] processDataLength Length of @p processData
        @param[in] processType Type of the transaction
        @param[out] response The response of the transaction
        */
        //WORMAPI WormError WORMAPI_CALL worm_transaction_update(
        //WormContext* context, const char* clientId, worm_uint transactionNumber,
        //const unsigned char* processData, worm_uint processDataLength,
        //const char* processType, WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_update(IntPtr context, IntPtr clientId, UInt64 transactionNumber,
            IntPtr processData, UInt64 processDataLength,
            IntPtr processType, IntPtr response);

        /* Finishes a transaction.

        Afterwards, the given transaction number is not allowed to be used anymore.

        @note This command requires an active CTSS interface and a valid time on the
                    TSE.

        @param[in] context Library context
        @param[in] clientId The id belonging to the client that issues the transaction.
                    The client must have been previously registered with
                    @ref worm_tse_registerClient before, otherwise the transaction will be
                    rejected. The same client must have opened the transaction belonging to
                    @p transactionNumber, otherwise the finish will be rejected.
        @param[in] transactionNumber A transaction number from a previous response of
                    @ref worm_transaction_start
        @param[in] processData Data of the transaction
        @param[in] processDataLength Length of @p processData
        @param[in] processType Type of the transaction
        @param[out] response The response of the transaction
        */
        //WORMAPI WormError WORMAPI_CALL worm_transaction_finish(
        //WormContext* context, const char* clientId, worm_uint transactionNumber,
        //const unsigned char* processData, worm_uint processDataLength,
        //const char* processType, WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_finish(IntPtr context, IntPtr clientId, UInt64 transactionNumber,
            IntPtr processData, UInt64 processDataLength,
            IntPtr processType, IntPtr response);

        /* Fetches the last transaction's response.

        Optionally, the response belonging to a transaction created by a
        specific client can be queried.

        @note This command requires an active CTSS interface.

        @param[in] context Library context
        @param[in] clientId Instead of returning the newest transaction response,
                        returns the latest transaction response that was created by this client.
                        Use NULL or a zero length string to ignore this parameter and return the
                        newest transaction response, no matter which client created it.
        @param[out] response The response of the transaction
        */
        //WORMAPI WormError WORMAPI_CALL worm_transaction_lastResponse(WormContext* context, const char* clientId, WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_lastResponse(IntPtr context, IntPtr clientId, IntPtr response);


        /* Lists the currently started transactions.

        The maximum amount of transaction numbers that can be retrieved with one call
        is 62. If further transactions should be retrieved, this method must be called
        with an increasing @p toSkip value, e.g. a @p toSkip value
        of 62 will return the started transactions #63 to #124.

        @note This command requires an active CTSS interface.

        @param[in] context Library context
        @param[in] clientId
                    Only open transactions belonging to this client will be returned.
                    Use NULL or a zero length string to return all open transactions without
                    further information to which client the transactions belong.
        @param[in] toSkip Number of transactions to skip. Use 0 to not skip
                    any transactions, but return the first (maximum 62) started transaction
                    numbers.
        @param[out] transactionNumbers An array of transaction numbers that will be
                    filled by this method. The array must be allocated and freed by the caller.
        @param[in] transactionNumbersLength Length of @p transactionNumbers in elements
        @param[out] storedTransactionNumbers The amount of elements that were stored
                    in @p transactionNumbers. Might be less than @p transactionNumbersLength
                    if there are not enough started transactions.
        */
        //WORMAPI WormError WORMAPI_CALL worm_transaction_listStartedTransactions(
        //WormContext* context, const char* clientId, int toSkip,
        //worm_uint* transactionNumbers, int transactionNumbersLength,
        //int* storedTransactionNumbers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_transaction_listStartedTransactions(IntPtr context, IntPtr clientId, int toSkip,
            IntPtr transactionNumbers, int transactionNumbersLength, IntPtr storedTransactionNumbers);

        /* Allocates a new transaction response.

        The returned object will not contain any data until it gets filled by
        @ref worm_transaction_start, @ref worm_transaction_update, or
        @ref worm_transaction_finish.

        @param[in] context Library context
        @returns an allocated object, or `NULL` on errors
        @see @ref worm_transaction_response_free
        */
        //WORMAPI WormTransactionResponse * WORMAPI_CALL worm_transaction_response_new(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_transaction_response_new(IntPtr context);


        /** Frees a previously allocated WormTransactionResponse.

        The object MUST NOT be used anymore after this method returned.

        @param[in] response Response reference. Might be `NULL`.
        @see @ref worm_transaction_response_new
        */
        //WORMAPI void WORMAPI_CALL worm_transaction_response_free(WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_transaction_response_free(IntPtr response);

        /** Retrieves the log time of a transaction.

        Each transaction will result in a log message that will be stored on the TSE.
        This method returns the timestamp of this log message.

        @param[in] response Response reference
        @returns the timestamp in Unix Time (number of seconds since Unix Epoch)
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_transaction_response_logTime(const WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_transaction_response_logTime(IntPtr response);

        /* Retrieves the serial number of the device that recorded a transaction.

        This is a hash over the public key that was used to generate the signature.

        @param[in] response Response reference
        @param[out] serialNumber The serial number.
                    The buffer is NOT `NULL` terminated, but exactly
                    @p serialNumberLength bytes long. Also, the buffer MUST NOT be freed by the
                    caller and will be valid until the @ref WormTransactionResponse parent gets
                    freed.
        @param[out] serialNumberLength Length of @p serialNumber.
        */
        //WORMAPI void WORMAPI_CALL worm_transaction_response_serialNumber(const WormTransactionResponse* response, const unsigned char** serialNumber,worm_uint* serialNumberLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_transaction_response_serialNumber(IntPtr response, ref IntPtr serialNumber, IntPtr serialNumberLength);

        /* Returns the signature counter that was assigned to a transaction.

        Each transaction generates a new signature in the TSE.
        For each signature, the signature counter will be increased.

        @param[in] response Response reference
        @returns the signature counter
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_transaction_response_signatureCounter(const WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_transaction_response_signatureCounter(IntPtr response);

        /* Retrieves the signature of a transaction.

        @param[in] response Response reference
        @param[out] signature The signature.
                    The returned string is NOT `NULL` terminated, but exactly
                    @p signatureLength bytes long. Also, the string MUST NOT be freed by the
                    caller and will be valid until the @ref WormTransactionResponse parent gets
                    freed.
        @param[out] signatureLength Length of @p signature.
        */
        //WORMAPI void WORMAPI_CALL worm_transaction_response_signature(const WormTransactionResponse* response, const unsigned char** signature,
        //worm_uint* signatureLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_transaction_response_signature(IntPtr response, ref IntPtr signature, IntPtr signatureLength);

        /* Returns the transaction number.

        If the response belongs to a transaction that was just started, this will be
        a newly assigned transaction number. For updated or finished transactions, this
        will be the same transaction number that was provided in the update or finish
        transaction call.

        @param[in] response Response reference
        @returns the transaction number
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_transaction_response_transactionNumber(const WormTransactionResponse* response);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_transaction_response_transactionNumber(IntPtr response);


        /* Allocates a new WormEntry.

        The returned object will not contain any data until
        @ref worm_entry_iterate_first or @ref worm_entry_iterate_last has been called.

        @param[in] context Library context
        @returns an allocated object, or `NULL` on errors
        @see @ref worm_entry_free
        */
        //WORMAPI WormEntry * WORMAPI_CALL worm_entry_new(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr worm_entry_new(IntPtr context);

        /* Frees a previously allocated WormEntry.

        The object MUST NOT be used anymore after this method returned.

        @param[in] entry Entry reference. Might be `NULL`.
        @see @ref worm_entry_new
        */
        //WORMAPI void WORMAPI_CALL worm_entry_free(WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void worm_entry_free(IntPtr entry);

        /* Reads the first entry from the TSE Store.

        The passed entry will be populated in place with the data of the first entry.
        \n
        In case the TSE Store is empty, the entry will be
        @ref worm_entry_isValid "invalid" afterwards.

        @note This method requires an active CTSS interface. Otherwise, the returned
                    entry will be @ref worm_entry_isValid "invalid".

        @param[in] entry Entry to populate
        @see @ref worm_entry_iterate_last
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_iterate_first(WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_iterate_first(IntPtr entry);

        /* Reads the last entry from the TSE Store.

        The passed entry will be populated in place with the data of the last entry.
        \n
        In case the TSE Store is empty, the entry will be
        @ref worm_entry_isValid "invalid" afterwards.

        @note This method requires an active CTSS interface. Otherwise, the returned
                    entry will be @ref worm_entry_isValid "invalid".

        @param[in] entry Entry to populate
        @see @ref worm_entry_iterate_first
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_iterate_last(WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_iterate_last(IntPtr entry);

        /* Reads an entry with an id previously obtained from @ref worm_entry_id.

        The passed entry will be populated in place with the data of the read entry.
        \n
        If the given id does not belong to a valid entry, the behavior of this method
        is undefined.

        @note This method requires an active CTSS interface. Otherwise, the returned
                    entry will be @ref worm_entry_isValid "invalid".

        @param[in,out] entry Entry to populate
        @param[in] id Id of entry to read
        @see @ref worm_entry_id
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_iterate_id(WormEntry* entry, uint32_t id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_iterate_id(IntPtr entry, UInt32 id);

        /* Reads the next entry from the TSE Store.

        The passed entry will be populated in place with the data of the next entry.
        \n
        In case there is no next entry, the entry will be
        @ref worm_entry_isValid "invalid" afterwards.

        @param[in,out] entry Entry to read the next entry for.
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_iterate_next(WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_iterate_next(IntPtr entry);

        /* Returns whether an entry is valid.

        An entry might be invalid, because it does not exist, i.e. it was
        read from before the beginning of the TSE Store or after its end.

        @param[in] entry Entry reference
        @returns `1` if it is valid, `0` otherwise
        */
        //WORMAPI int WORMAPI_CALL worm_entry_isValid(const WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int worm_entry_isValid(IntPtr entry);

        /* Returns the id of an entry.

        The id is unique across all entries in the TSE Store and can be used to
        identify a specific TSE entry for future use, e.g. in
        @ref worm_entry_iterate_id.
        The id of an entry is stable across the life-time of the TSE Store, i.e. it
        will never change and can be used across power cycles, even if new entries have
        been added to the TSE Store.

        Calling @ref worm_export_deleteStoredData invalidates all previous entries and
        their ids. These ids must NOT be used anymore.

        @param[in] entry Entry reference
        @returns the id
        @see @ref worm_entry_iterate_id
        */
        //WORMAPI uint32_t WORMAPI_CALL worm_entry_id(const WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt32 worm_entry_id(IntPtr entry);

        /* Returns the type of an entry.

        @param[in] entry Entry reference
        @returns the type of the entry
        */
        //WORMAPI WormEntryType WORMAPI_CALL worm_entry_type(const WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormEntryType worm_entry_type(IntPtr entry);

        /* Returns the length of an entry's complete log message.

        This is the length of the buffer that will be required to store the entire
        log message.

        @param[in] entry Entry reference
        @returns the size of the log message in bytes
        @see @ref worm_entry_readLogMessage
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_entry_logMessageLength(const WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_entry_logMessageLength(IntPtr entry);

        /* Reads the entire log message of an entry.

        @param[in] entry Entry reference
        @param[out] msg The log message.
                    The buffer is NOT `NULL` terminated, but exactly @p msgLength bytes
                    long. The buffer must be allocated and freed by the caller.
        @param[in] msgLength Length of @p msg. Must be equal to
                    @ref worm_entry_logMessageLength.
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_readLogMessage(const WormEntry* entry, unsigned char* msg, worm_uint msgLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_readLogMessage(IntPtr entry, IntPtr msg, UInt64 msgLength);

        /* Returns the length of an entry's process data.

        @param[in] entry Entry reference
        @returns the size of the entry in bytes
        */
        //WORMAPI worm_uint WORMAPI_CALL worm_entry_processDataLength(const WormEntry* entry);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate UInt64 worm_entry_processDataLength(IntPtr entry);

        /* Reads the process data of an entry.

        @param[in] entry Entry reference
        @param[in] offset Offset in bytes from where to read.
                    A value of `0` reads from the beginning of the data.
        @param[out] data Output buffer. Must be allocated by the caller and
                    be able to hold at least @p dataLength bytes.
        @param[in] dataLength Number of bytes to read. If this exceeds the available
                    data – i.e. `offset + dataLength > worm_entry_processDataLength()` –
                    the method will fail.
        @see @ref worm_entry_processDataLength
        */
        //WORMAPI WormError WORMAPI_CALL worm_entry_readProcessData(const WormEntry* entry, worm_uint offset,
        //                       unsigned char* data, worm_uint dataLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_entry_readProcessData(IntPtr entry, UInt64 offset, IntPtr data, UInt64 dataLength);

        /* Returns the certificate that can be used to verify the signatures
        of all Log Messages created by the TSE.

        The returned data is a single PEM file, which contains multiple
        certificates, since the TSE's certificate is signed by other
        certificates.
        To verify the signature, only the leaf certificate (the first one
        in the PEM file) is required.
        To make sure the leaf certificate is genuine, the next certificate
        in the file can be used to verify the previous certificate until
        the last certificate is about to be checked, which will be the root
        certificate that must be trusted by the system.

        Since the whole chain might be quite large, it is advised to supply
        a big @p certificate buffer (i.e. several kilobytes). If the buffer is too
        small, the method will fail.

        @note This command requires an active CTSS interface.

        @param[in] context Library context
        @param[out] certificate Output buffer. Must be allocated by the caller.
                    If this is NULL, only the required length for the buffer will be stored
                    in @p certificateLength.
        @param[in,out] certificateLength Length of the @p certificate buffer.
                    After a successful execution, this will contain the number of
                    bytes that were written to the output buffer.
        */
        //WORMAPI WormError WORMAPI_CALL worm_getLogMessageCertificate(WormContext* context, unsigned char* certificate,
        //                          uint32_t* certificateLength);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_getLogMessageCertificate(IntPtr context, IntPtr certificate, IntPtr certificateLength);

        /*
            * Callback which will be invoked for each chunk of data that is exported
            * by @ref worm_export_tar.
            * Returning non-zero will cause the whole export to be aborted with an error.
            *
            * @param[in] chunk Current chunk of data that is exported.
            * @param[in] chunkLength Length of @p chunk.
            * @param[in] callbackData Data provided as @p callbackData when calling
            *     @ref worm_export_tar.
            * @returns 0 on success, non-zero on failure
            */
        //typedef int (WORMAPI_CALL* WormExportTarCallback) (const unsigned char* chunk,
        //                                               unsigned int chunkLength,
        //                                        void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError WormExportTarCallback(IntPtr chunk, uint chunkLength, IntPtr callbackData);



        /* Exports all stored data as a TR-03153 compliant TAR archive.

        Since the whole archive might be very large, a callback is registered that
        will be called repeatedly with small chunks of data.
        Consumers of the data can thus store it to file or transmit it over the network
        in small chunks.
        Arbitrary data (e.g. file handles or network sockets) can be registered as
        @p callbackData that will then be supplied in the callback.

        @note This command requires an active CTSS interface.

        @note If you want to get progress information about the on-going export, use
                    @ref worm_export_tar_incremental instead, which can also be used to create
                    a full export.

        @param[in] context Library context
        @param[in] callback Callback to invoke for each chunk of exported data.
        @param[in] callbackData Arbitrary user data that will be supplied to the
                    callback.
        */
        //WORMAPI WormError WORMAPI_CALL worm_export_tar(WormContext* context,
        //                                       WormExportTarCallback callback,
        //                                       void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_export_tar(IntPtr context, IntPtr callback, IntPtr callbackData);

        /*
            * Callback which will be invoked for each chunk of data that is exported
            * by @ref worm_export_tar_incremental.
            * Returning non-zero will cause the whole export to be aborted with an error.
            *
            * With the help of the parameters @p processedBlocks and @p totalBlocks,
            * a progress indication can be given to the user.
            *
            * @param[in] chunk Current chunk of data that is exported.
            * @param[in] chunkLength Length of @p chunk.
            * @param[in] processedBlocks Amount of 512 byte blocks that were successfully
            *     processed so far.
            * @param[in] totalBlocks Total amount of 512 byte blocks that make up the
            *     exported TAR archive.
            * @param[in] callbackData Data provided as @p callbackData when calling
            *     @ref worm_export_tar.
            * @returns 0 on success, non-zero on failure
            */
        //typedef int (WORMAPI_CALL* WormExportTarIncrementalCallback) (
        //const unsigned char* chunk, unsigned int chunkLength,
        //uint32_t processedBlocks, uint32_t totalBlocks, void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError WormExportTarIncrementalCallback(IntPtr chunk, uint chunkLength, UInt32 processedBlocks, UInt32 totalBlocks, IntPtr callbackData);


        /* Performs an incremental export of all stored data as a TR-03153 compliant
            TAR archive.

        In order to facilitate an incremental export, the method will store auxilary
        data in the provided @p newState output variable which must be passed again to
        this method on the next call as @p lastState. Thus, the content of @p newState
        must be stored persistently between calls to this method.
        The same state buffer must not be used for multiple TSEs, but must be stored
        separately for each TSE.

        For the initial export, pass NULL as @p lastState to indicate that there is
        no previous state available. This will then perform a full export.

        A successful export will always include the info.csv and cert.pem files that
        are part of a regular full export.
        In case no new Log Messages have been stored on the TSE since the last
        incremental export, the method will fail with
        @ref WORM_ERROR_INCREMENTAL_EXPORT_NO_DATA.

        Please note that calling @ref worm_export_deleteStoredData will invalidate the
        content of any previously stored state that has been returned as @p newState.
        Thus, after using @ref worm_export_deleteStoredData, @p lastState must be set
        to NULL again one time to do a fresh full export (which should be small, since
        the data was recently deleted) and to generate a new valid state.

        Since the whole archive might be very large, a callback is registered that
        will be called repeatedly with small chunks of data.
        Consumers of the data can thus store it to file or transmit it over the network
        in small chunks.
        Arbitrary data (e.g. file handles or network sockets) can be registered as
        @p callbackData that will then be supplied in the callback.

        As it is impossible to calculate the total size of the exported data before
        starting the incremental export, the callback provides progress information of
        the operation.

        @note This command requires an active CTSS interface.

        Example initial Export:
        \code
        unsigned char newState[WORM_EXPORT_TAR_INCREMENTAL_STATE_SIZE];
        worm_export_tar_incremental(context, NULL, 0, newState, sizeof(newState),
                                    NULL, NULL, callback, callbackData);
        // Persist newState (e.g. to file)
        // ...
        \endcode

        Example incremental export:
        \code
        unsigned char oldState[WORM_EXPORT_TAR_INCREMENTAL_STATE_SIZE];
        // Restore oldState (e.g. from file)
        // ...
        unsigned char newState[WORM_EXPORT_TAR_INCREMENTAL_STATE_SIZE];
        worm_export_tar_incremental(context, oldState, sizeof(oldState), newState,
                                    NULL, NULL, sizeof(newState), callback,
                                    callbackData);
        // Persist newState (e.g. to file)
        // ...
        \endcode

        @param[in] context Library context
        @param[in] lastState Output value @p newState from a previous call to this
            method. Pass NULL to perform a full export.
        @param[in] lastStateSize Size of @p lastState in bytes.
            Must be @ref WORM_EXPORT_TAR_INCREMENTAL_STATE_SIZE or 0 in case
            @p lastState is NULL.
        @param[out] newState Output state of the export for future use as @p lastState.
        @param[in] newStateSize Size of @p newState in bytes.
            Must be @ref WORM_EXPORT_TAR_INCREMENTAL_STATE_SIZE.
        @param[out] firstSignatureCounter Signature counter of the first Log Message
            that is part of this export. Use NULL to ignore this parameter.
        @param[out] lastSignatureCounter Signature counter of the last Log Message
            that is part of this export. Use NULL to ignore this parameter.
        @param[in] callback Callback to invoke for each chunk of exported data.
        @param[in] callbackData Arbitrary user data that will be supplied to the
            callback.
        */
        //WORMAPI WormError WORMAPI_CALL worm_export_tar_incremental(
        //WormContext* context, const unsigned char* lastState, int lastStateSize,
        //unsigned char* newState, int newStateSize, uint64_t* firstSignatureCounter,
        //uint64_t* lastSignatureCounter, WormExportTarIncrementalCallback callback,
        //void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_export_tar_incremental(IntPtr context,
            IntPtr lastState, int lastStateSize, IntPtr newState, int newStateSize,
            IntPtr firstSignatureCounter, IntPtr lastSignatureCounter, IntPtr callback, IntPtr callbackData);

        /* Exports data between a given time interval and with a given client ID as
            a TR-03153 compliant TAR archive.

@note This command requires an active CTSS interface.
@note The filtered export is very slow when the stored data on the TSE
            increases. Thus, it is recommended to use @ref worm_export_tar_incremental
            instead.

@param[in] context Library context
@param[in] startDate Date as Unix Time (number of seconds since Unix Epoch).
            If 0, it will be treated as the beginning of time.
            The date is treated inclusively.
@param[in] endDate Date as  Unix Time (number of seconds since Unix Epoch).
            If -1, it will be treated as infinity.
            The date is treated inclusively.
@param[in] clientId Client ID as NULL-terminated string. Use NULL or an empty
            string to include transactions of all clients.
@param[in] callback Callback to invoke for each chunk of exported data.
@param[in] callbackData Arbitrary user data that will be supplied to the
            callback.
@see @ref worm_export_tar
*/
        //WORMAPI WormError WORMAPI_CALL worm_export_tar_filtered_time(
        //WormContext* context, worm_uint startDate, worm_uint endDate,
        //const char* clientId, WormExportTarCallback callback, void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_export_tar_filtered_time(IntPtr context, UInt64 startDate, UInt64 endDate, IntPtr clientId,
                IntPtr callback, IntPtr callbackData);

        /* Exports data belonging to a given transaction interval and client ID.

        @note This command requires an active CTSS interface.
        @note The filtered export is very slow when the stored data on the TSE
                    increases. Thus, it is recommended to use @ref worm_export_tar_incremental
                    instead.

        @param[in] context Library context
        @param[in] transactionNumberStart Start transaction number (inclusive).
        @param[in] transactionNumberEnd End transaction number (inclusive).
                    If this is equal to @p transactionNumberStart, only transaction data
                    belonging to this single transaction will be exported.
        @param[in] clientId NULL-terminated Client ID string. Use NULL or an empty
                    string to include transactions of all clients.
        @param[in] callback Callback to invoke for each chunk of exported data.
        @param[in] callbackData Arbitrary user data that will be supplied to the
                    callback.
        @see @ref worm_export_tar
        */
        //WORMAPI WormError WORMAPI_CALL worm_export_tar_filtered_transaction(
        //WormContext* context, worm_uint transactionNumberStart,
        //worm_uint transactionNumberEnd, const char* clientId,
        //WormExportTarCallback callback, void* callbackData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_export_tar_filtered_transaction(IntPtr context, UInt64 transactionNumberStart, UInt64 transactionNumberEnd, IntPtr clientId,
                IntPtr callback, IntPtr callbackData);

        /* Deletes all stored data from the TSE.

        For successful execution, a full or incremental export (but not a filtered
        one) must have been performed previously and no new data must have been
        generated since then.

        @note This command requires an active CTSS interface and the user @e Admin to be
                    logged in (see @ref userauth).
        @param[in] context Library context
        @see @ref worm_export_tar
        */
        //WORMAPI WormError WORMAPI_CALL worm_export_deleteStoredData(WormContext* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate WormError worm_export_deleteStoredData(IntPtr context);

        /* Firmware updates bundled with this library. */
        public enum WormTseFirmwareUpdate
        {
            /** Update to software version 1.1.0 for USB TSEs. */
            WORM_FW_1_1_0_USB,
            /** No firmware update available. */
            WORM_FW_NONE
        }

        /* Possible types of entries in the TSE Store.
        The type of a @ref WormEntry defines the structure of its payload.
        */
        public enum WormEntryType
        {
            /* A transaction. */
            WORM_ENTRY_TYPE_TRANSACTION = 0,

            /* A system log message. */
            WORM_ENTRY_TYPE_SYSTEM_LOG_MESSAGE = 1,

            /* A SE Audit log message. */
            WORM_ENTRY_TYPE_SE_AUDIT_LOG_MESSAGE = 2
        }

        /** Supported User IDs and their permissions. */
        public enum WormUserId
        {
            /* Allowed to execute all methods except:
              - @ref worm_tse_ctss_enable
              - @ref worm_tse_ctss_disable
              - @ref worm_tse_initialize
              - @ref worm_tse_decommission
              - @ref worm_tse_updateTime
              - @ref worm_tse_firmwareUpdate_transfer
              - @ref worm_tse_firmwareUpdate_apply
              - @ref worm_tse_enableExportIfCspTestFails
              - @ref worm_tse_disableExportIfCspTestFails
              - @ref worm_tse_registerClient
              - @ref worm_tse_deregisterClient
              - @ref worm_tse_listRegisteredClients
              - @ref worm_export_deleteStoredData
            */
            WORM_USER_UNAUTHENTICATED = 0,

            /* Allowed to execute all methods. */
            WORM_USER_ADMIN = 1,

            /* Same as @ref WORM_USER_UNAUTHENTICATED, but is allowed to call
                @ref worm_tse_updateTime. */
            WORM_USER_TIME_ADMIN = 2
        }

        /* Possible initialization states of the TSE. */
        public enum WormInitializationState
        {
            /* Not initialized.
            This is the default factory state.
            */
            WORM_INIT_UNINITIALIZED = 0,

            /* Initialized.
            @see worm_tse_initialize
            */
            WORM_INIT_INITIALIZED = 1,

            /* Decommissioned.
            @see worm_tse_decommission
            */
            WORM_INIT_DECOMMISSIONED = 2
        }

        /*
        Error codes.

        All methods that can fail return a @ref WormError to indicate if they
        completed successfully. A return value of @ref WORM_ERROR_NOERROR means
        the method succeeded without an error. All other values indicate an
        error condition, which usually stems from a usage error and can not
        be handled by the caller in a meaningful way.
        */
        public enum WormError
        {
            /* No error. */
            WORM_ERROR_NOERROR = 0,

            /* Invalid input parameter. */
            WORM_ERROR_INVALID_PARAMETER = 1,

            /* No TSE was found at the provided path. */
            WORM_ERROR_NO_WORM_CARD = 2,

            /* IO Error. */
            WORM_ERROR_IO = 3,

            /* Operation timed out. */
            WORM_ERROR_TIMEOUT = 4,

            /* Out of memory. */
            WORM_ERROR_OUTOFMEM = 5,

            /* Invalid Response from TSE. */
            WORM_ERROR_INVALID_RESPONSE = 6,

            /* The TSE Store is full. */
            WORM_ERROR_STORE_FULL_INTERNAL = 7,

            /* A command was not acknowledged, i.e. it was ignored by the card.
              This happens if two commands are sent at the same time or if a command is
              not allowed in the current state.
            */
            WORM_ERROR_RESPONSE_MISSING = 8,

            /* TSE not initialized. */
            WORM_ERROR_EXPORT_NOT_INITIALIZED = 9,

            /* Export Failed. */
            WORM_ERROR_EXPORT_FAILED = 10,

            /* Incremental Export: invalid state. */
            WORM_ERROR_INCREMENTAL_EXPORT_INVALID_STATE = 11,

            /* Incremental Export: no new data. */
            WORM_ERROR_INCREMENTAL_EXPORT_NO_DATA = 12,

            /* A power cycle occurred during command execution. */
            WORM_ERROR_POWER_CYCLE_DETECTED = 13,

            /* The firmware update was not properly applied. */
            WORM_ERROR_FIRMWARE_UPDATE_NOT_APPLIED = 14,

            /* Failed to start the background thread for keeping the TSE awake. */
            WORM_ERROR_THREAD_START_FAILED = 15,

            /** Firmware Update: No update is available for the TSE. */
            WORM_ERROR_FWU_NOT_AVAILABLE = 26,

            /* Lowest error code that might be raised from the TSE. */
            WORM_ERROR_FROM_CARD_FIRST = 0x1000,

            /* Unspecified, internal processing error. */
            WORM_ERROR_UNKNOWN = 0x1001,

            /* Time not set. */
            WORM_ERROR_NO_TIME_SET = 0x1002,

            /* No transaction in progress. */
            WORM_ERROR_NO_TRANSACTION_IN_PROGRESS = 0x1004,

            /* Wrong command length. */
            WORM_ERROR_INVALID_CMD_SYNTAX = 0x1005,

            /* @deprecated Use @ref WORM_ERROR_INVALID_CMD_SYNTAX instead. */
            WORM_ERROR_WRONG_LENGTH = WORM_ERROR_INVALID_CMD_SYNTAX,

            /* Not enough data written during transaction. */
            WORM_ERROR_NOT_ENOUGH_DATA_WRITTEN = 0x1006,

            /* Invalid Parameter. */
            WORM_ERROR_TSE_INVALID_PARAMETER = 0x1007,

            /* Given transaction is not started. */
            WORM_ERROR_TRANSACTION_NOT_STARTED = 0x1008,

            /* Maximum parallel transactions reached. */
            WORM_ERROR_MAX_PARALLEL_TRANSACTIONS = 0x1009,

            /* Certificate expired. */
            WORM_ERROR_CERTIFICATE_EXPIRED = 0x100a,

            /* No last transaction to fetch. */
            WORM_ERROR_NO_LAST_TRANSACTION = 0x100c,

            /* Command not allowed in current state. */
            WORM_ERROR_CMD_NOT_ALLOWED = 0x100d,

            /* Signatures exceeded. */
            WORM_ERROR_TRANSACTION_SIGNATURES_EXCEEDED = 0x100e,

            /* Not authorized. */
            WORM_ERROR_NOT_AUTHORIZED = 0x100f,

            /* Maximum registered clients reached. */
            WORM_ERROR_MAX_REGISTERED_CLIENTS_REACHED = 0x1010,

            /* Client not registered. */
            WORM_ERROR_CLIENT_NOT_REGISTERED = 0x1011,

            /* Failed to delete, data not completely exported. */
            WORM_ERROR_EXPORT_UNACKNOWLEDGED_DATA = 0x1012,

            /* Failed to deregister, client has unfinished transactions. */
            WORM_ERROR_CLIENT_HAS_UNFINISHED_TRANSACTIONS = 0x1013,

            /* Failed to decommission, TSE has unfinished transactions. */
            WORM_ERROR_TSE_HAS_UNFINISHED_TRANSACTIONS = 0x1014,

            /* Wrong state, there is no response to fetch. */
            WORM_ERROR_TSE_NO_RESPONSE_TO_FETCH = 0x1015,

            /* Wrong state, ongoing Filtered Export must be finished before this command
               is allowed. */
            WORM_ERROR_NOT_ALLOWED_EXPORT_IN_PROGRESS = 0x1016,

            /* Operation failed, not enough remaining capacity in TSE Store. */
            WORM_ERROR_STORE_FULL = 0x1017,

            /* Wrong state, changed PUK required. */
            WORM_ERROR_WRONG_STATE_NEEDS_PUK_CHANGE = 0x1050,

            /* Wrong state, changed PIN required. */
            WORM_ERROR_WRONG_STATE_NEEDS_PIN_CHANGE = 0x1051,

            /* Wrong state, active CTSS interface required. */
            WORM_ERROR_WRONG_STATE_NEEDS_ACTIVE_CTSS = 0x1053,

            /* @deprecated Use @ref WORM_ERROR_WRONG_STATE_NEEDS_ACTIVE_CTSS instead. */
            WORM_ERROR_WRONG_STATE_NEEDS_ACTIVE_ERS =
                WORM_ERROR_WRONG_STATE_NEEDS_ACTIVE_CTSS,

            /* Wrong state, self test must be run first. */
            WORM_ERROR_WRONG_STATE_NEEDS_SELF_TEST = 0x1054,

            /* Wrong state, passed self test required. */
            WORM_ERROR_WRONG_STATE_NEEDS_SELF_TEST_PASSED = 0x1055,

            /* Firmware Update: Integrity check failed. */
            WORM_ERROR_FWU_INTEGRITY_FAILURE = 0x1061,

            /* Firmware Update: Decryption failed. */
            WORM_ERROR_FWU_DECRYPTION_FAILURE = 0x1062,

            /* Firmware Update: Wrong format. */
            WORM_ERROR_FWU_WRONG_FORMAT = 0x1064,

            /* Firmware Update: Internal error. */
            WORM_ERROR_FWU_INTERNAL_ERROR = 0x1065,

            /* Firmware Update: downgrade prohibited. */
            WORM_ERROR_FWU_DOWNGRADE_PROHIBITED = 0x1067,

            /* TSE already initialized. */
            WORM_ERROR_TSE_ALREADY_INITIALIZED = 0x10FD,

            /* TSE decommissioned. */
            WORM_ERROR_TSE_DECOMMISSIONED = 0x10FE,

            /* TSE not initialized. */
            WORM_ERROR_TSE_NOT_INITIALIZED = 0x10FF,

            /* Authentication failed. */
            WORM_ERROR_AUTHENTICATION_FAILED = 0x1100,

            /* PIN is blocked. */
            WORM_ERROR_AUTHENTICATION_PIN_BLOCKED = 0x1201,

            /* Given user is not authenticated. */
            WORM_ERROR_AUTHENTICATION_USER_NOT_LOGGED_IN = 0x1202,

            /* Self test of FW failed. */
            WORM_ERROR_SELF_TEST_FAILED_FW = 0x1300,

            /* Self test of CSP failed. */
            WORM_ERROR_SELF_TEST_FAILED_CSP = 0x1310,

            /* Self test of RNG failed. */
            WORM_ERROR_SELF_TEST_FAILED_RNG = 0x1320,

            /* Firmware Update: Base FW update error. */
            WORM_ERROR_FWU_BASE_FW_ERROR = 0x1400,

            /* Firmware Update: FW Extension update error. */
            WORM_ERROR_FWU_FWEXT_ERROR = 0x1500,

            /* Firmware Update: CSP update error. */
            WORM_ERROR_FWU_CSP_ERROR = 0x1600,

            /* Filtered Export: no export in progress. */
            WORM_ERROR_EXPORT_NONE_IN_PROGRESS = 0x2001,

            /* Filtered Export: no new data, keep polling. */
            WORM_ERROR_EXPORT_RETRY = 0x2002,

            /* Filtered Export: no matching entries, export would be empty. */
            WORM_ERROR_EXPORT_NO_DATA_AVAILABLE = 0x2003,

            /* Command not found. */
            WORM_ERROR_CMD_NOT_FOUND = 0xf000,

            /* Signature creation error. */
            WORM_ERROR_SIG_ERROR = 0xff00,

            /* Highest error code that might be raised from the TSE. */
            WORM_ERROR_FROM_CARD_LAST = 0xFFFF
        }

        public worm_getVersion func_worm_getVersion { get; set; }
        public worm_signatureAlgorithm func_worm_signatureAlgorithm { get; set; }
        public worm_logTimeFormat func_worm_logTimeFormat { get; set; }
        public worm_init func_worm_init { get; set; }
        public worm_cleanup func_worm_cleanup { get; set; }
        public worm_info_new func_worm_info_new { get; set; }
        public worm_info_free func_worm_info_free { get; set; }
        public worm_info_read func_worm_info_read { get; set; }
        public worm_info_customizationIdentifier func_worm_info_customizationIdentifier { get; set; }
        public worm_info_isDevelopmentFirmware func_worm_info_isDevelopmentFirmware { get; set; }
        public worm_info_capacity func_worm_info_capacity { get; set; }
        public worm_info_size func_worm_info_size { get; set; }
        public worm_info_hasValidTime func_worm_info_hasValidTime { get; set; }
        public worm_info_hasPassedSelfTest func_worm_info_hasPassedSelfTest { get; set; }
        public worm_info_isCtssInterfaceActive func_worm_info_isCtssInterfaceActive { get; set; }
        public worm_info_isExportEnabledIfCspTestFails func_worm_info_isExportEnabledIfCspTestFails { get; set; }
        public worm_info_initializationState func_worm_info_initializationState { get; set; }
        public worm_info_isDataImportInProgress func_worm_info_isDataImportInProgress { get; set; }
        public worm_info_hasChangedPuk func_worm_info_hasChangedPuk { get; set; }
        public worm_info_hasChangedAdminPin func_worm_info_hasChangedAdminPin { get; set; }
        public worm_info_hasChangedTimeAdminPin func_worm_info_hasChangedTimeAdminPin { get; set; }
        public worm_info_timeUntilNextSelfTest func_worm_info_timeUntilNextSelfTest { get; set; }
        public worm_info_startedTransactions func_worm_info_startedTransactions { get; set; }
        public worm_info_maxStartedTransactions func_worm_info_maxStartedTransactions { get; set; }
        public worm_info_createdSignatures func_worm_info_createdSignatures { get; set; }
        public worm_info_maxSignatures func_worm_info_maxSignatures { get; set; }
        public worm_info_remainingSignatures func_worm_info_remainingSignatures { get; set; }
        public worm_info_maxTimeSynchronizationDelay func_worm_info_maxTimeSynchronizationDelay { get; set; }
        public worm_info_maxUpdateDelay func_worm_info_maxUpdateDelay { get; set; }
        public worm_info_tsePublicKey func_worm_info_tsePublicKey { get; set; }
        public worm_info_tseSerialNumber func_worm_info_tseSerialNumber { get; set; }
        public worm_info_tseDescription func_worm_info_tseDescription { get; set; }
        public worm_info_registeredClients func_worm_info_registeredClients { get; set; }
        public worm_info_maxRegisteredClients func_worm_info_maxRegisteredClients { get; set; }
        public worm_info_certificateExpirationDate func_worm_info_certificateExpirationDate { get; set; }
        public worm_info_tarExportSizeInSectors func_worm_info_tarExportSizeInSectors { get; set; }
        public worm_info_tarExportSize func_worm_info_tarExportSize { get; set; }
        public worm_info_hardwareVersion func_worm_info_hardwareVersion { get; set; }
        public worm_info_softwareVersion func_worm_info_softwareVersion { get; set; }
        public worm_info_formFactor func_worm_info_formFactor { get; set; }
        public worm_flash_health_summary func_worm_flash_health_summary { get; set; }
        public worm_flash_health_needs_replacement func_worm_flash_health_needs_replacement { get; set; }
        public worm_tse_factoryReset func_worm_tse_factoryReset { get; set; }
        public worm_tse_setup func_worm_tse_setup { get; set; }
        public worm_tse_ctss_enable func_worm_tse_ctss_enable { get; set; }
        public worm_tse_ctss_disable func_worm_tse_ctss_disable { get; set; }
        public worm_tse_initialize func_worm_tse_initialize { get; set; }
        public worm_tse_decommission func_worm_tse_decommission { get; set; }
        public worm_tse_updateTime func_worm_tse_updateTime { get; set; }
        public worm_transaction_openStore func_worm_transaction_openStore { get; set; }
        public worm_tse_firmwareUpdate_transfer func_worm_tse_firmwareUpdate_transfer { get; set; }
        public worm_tse_firmwareUpdate_isBundledAvailable func_worm_tse_firmwareUpdate_isBundledAvailable { get; set; }
        public worm_tse_firmwareUpdate_applyBundled func_worm_tse_firmwareUpdate_applyBundled { get; set; }
        public worm_tse_firmwareUpdate_apply func_worm_tse_firmwareUpdate_apply { get; set; }
        public worm_tse_enableExportIfCspTestFails func_worm_tse_enableExportIfCspTestFails { get; set; }
        public worm_tse_disableExportIfCspTestFails func_worm_tse_disableExportIfCspTestFails { get; set; }
        public worm_tse_runSelfTest func_worm_tse_runSelfTest { get; set; }
        public worm_tse_registerClient func_worm_tse_registerClient { get; set; }
        public worm_tse_deregisterClient func_worm_tse_deregisterClient { get; set; }
        public worm_tse_listRegisteredClients func_worm_tse_listRegisteredClients { get; set; }
        public worm_user_login func_worm_user_login { get; set; }
        public worm_user_logout func_worm_user_logout { get; set; }
        public worm_user_unblock func_worm_user_unblock { get; set; }
        public worm_user_change_puk func_worm_user_change_puk { get; set; }
        public worm_user_change_pin func_worm_user_change_pin { get; set; }
        public worm_user_deriveInitialCredentials func_worm_user_deriveInitialCredentials { get; set; }
        public worm_transaction_start func_worm_transaction_start { get; set; }
        public worm_transaction_update func_worm_transaction_update { get; set; }
        public worm_transaction_finish func_worm_transaction_finish { get; set; }
        public worm_transaction_lastResponse func_worm_transaction_lastResponse { get; set; }
        public worm_transaction_listStartedTransactions func_worm_transaction_listStartedTransactions { get; set; }
        public worm_transaction_response_new func_worm_transaction_response_new { get; set; }
        public worm_transaction_response_free func_worm_transaction_response_free { get; set; }
        public worm_transaction_response_logTime func_worm_transaction_response_logTime { get; set; }
        public worm_transaction_response_serialNumber func_worm_transaction_response_serialNumber { get; set; }
        public worm_transaction_response_signatureCounter func_worm_transaction_response_signatureCounter { get; set; }
        public worm_transaction_response_signature func_worm_transaction_response_signature { get; set; }
        public worm_transaction_response_transactionNumber func_worm_transaction_response_transactionNumber { get; set; }
        public worm_entry_new func_worm_entry_new { get; set; }
        public worm_entry_free func_worm_entry_free { get; set; }
        public worm_entry_iterate_first func_worm_entry_iterate_first { get; set; }
        public worm_entry_iterate_last func_worm_entry_iterate_last { get; set; }
        public worm_entry_iterate_id func_worm_entry_iterate_id { get; set; }
        public worm_entry_iterate_next func_worm_entry_iterate_next { get; set; }
        public worm_entry_isValid func_worm_entry_isValid { get; set; }
        public worm_entry_id func_worm_entry_id { get; set; }
        public worm_entry_type func_worm_entry_type { get; set; }
        public worm_entry_logMessageLength func_worm_entry_logMessageLength { get; set; }
        public worm_entry_readLogMessage func_worm_entry_readLogMessage { get; set; }
        public worm_entry_processDataLength func_worm_entry_processDataLength { get; set; }
        public worm_entry_readProcessData func_worm_entry_readProcessData { get; set; }
        public worm_getLogMessageCertificate func_worm_getLogMessageCertificate { get; set; }
        public worm_export_tar func_worm_export_tar { get; set; }
        public worm_export_tar_incremental func_worm_export_tar_incremental { get; set; }
        public worm_export_tar_filtered_time func_worm_export_tar_filtered_time { get; set; }
        public worm_export_tar_filtered_transaction func_worm_export_tar_filtered_transaction { get; set; }
        public worm_export_deleteStoredData func_worm_export_deleteStoredData { get; set; }
    }
}
