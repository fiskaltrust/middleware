using System.ComponentModel;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models
{
    public enum ProtocolErrors
    {
        [Description("An unknown error has been generated. Contact the customer support.")]
        ERROR_UNKNOWN = 0,
        [Description("File or archive not present.")]
        NOT_PRESENT = 1,
        [Description("There was an error in reading the fiscal memory data. Technical service is required.")]
        FISCAL_MEMORY_RX_ERROR = 2,
        [Description("The data input is not correct, delete it and retry.")]
        INVALID_VALUE = 3,
        [Description("If the requested operation is not executed, retry. If the problem persists, contact the technical service. In the case of device with battery, it can occurs when the battery is low. Connect the device to the network and recharge it.")]
        ERROR_OF_INTERNAL_CODE = 4,
        [Description("The operation executed is not correct, close the documents in progress a retry.")]
        OPERATION_NOT_POSSIBLE = 5,
        [Description("It is not possible to write the fiscal logotype on fiscal memory.")]
        ERROR_WRITING_LOGOTYPE_IN_FISCAL_MEMORY = 6,
        [Description("It has been sent the wording total in a sale.")]
        ERROR_ON_TOTAL_DESCRIPTION = 7,
        [Description("It is impossible to execute it again.")]
        ALREADY_DONE = 8,
        [Description("The amount exceed the maximum value on receipt total (€ 9999999,99).")]
        DOCUMENT_TOTAL_OVERFLOW = 9,
        [Description("The amount exceed the maximum value on daily total (€ 9999999,99).")]
        DAILY_TOTAL_OVERFLOW = 10,
        [Description("The price/amount is not correct.")]
        ERROR_IN_AMOUNT = 11,
        [Description("A date has been entered older than the one stored in the fiscal memory, Correct it and re-enter.")]
        FISCAL_MEMORY_DATE_MORE_RECENT = 12,
        [Description("A not valid date/time has been inserted. Correct it and re-set it.")]
        INVALID_DATE_TIME = 13,
        [Description("A data different from the one stored has been inserted. Correct it and re-set it.")]
        DIFFERENT_DATE = 14,
        [Description("Control box/fiscal memory damaged.")]
        FATAL_ERROR_FROM_CB_FM = 15,
        [Description("Error because of paper end. Replace paper.")]
        PAPER_END = 16,
        [Description("Error in slip printer.")]
        SLIP_PRINTER = 17,
        [Description("Document heading is missing.")]
        BUSINESS_NAME_MISSING = 18,
        [Description("It was not possible to write the fiscal zeroset in fiscal memory.")]
        FISCAL_MEMORY_ZEROSET_NOT_POSSIBLE = 19,
        [Description("A remote terminal is sending a document. Wait for the end of current operation.")]
        OPEN_DOCUMENT = 21,
        [Description("In the request has been inserted a not suitable date.")]
        WRONG_DATES = 22,
        [Description("The total of the receipt is negative, add amounts or cancel.")]
        NEGATIVE_TOTAL = 23,
        [Description("An incorrect or unsupported length command was sent.")]
        ERROR_IN_COMMAND_LENGTH = 24,
        [Description("A not completed payment was sent.")]
        PAYMENT_NOT_COMPLETED = 25,
        [Description("The procedure was interrupted by the user.")]
        PROCEDURE_ABORTED_BY_USER = 26,
        [Description("Generic database error (an internal subcode is displayed that defines the type of SQLite error). Contact technical service.")]
        DB_ENGINE_COD = 27,
        [Description("Set the VAT rate.")]
        NOT_PROGRAMMED_VAT = 28,
        [Description("A negative VAT rate has been set.")]
        NEGATIVE_VAT = 29,
        [Description("The electronic journal(EJ) is full. Replace it.")]
        EJ_FULL = 30,
        [Description("The electronic journal (EJ) is nearly full. Replace it as soon as possible.")]
        EJ_NEARLY_FULL = 31,
        [Description("The new EJ could not be initialized. Replace it with another and contact the technical service.")]
        EJ_INITIALIZATION_NOT_POSSIBLE = 32,
        [Description("The EJ is missing, insert it into the device.")]
        EJ_NOT_PRESENT = 33,
        [Description("Attempt to write to EJ failed.")]
        WRONG_EJ_DATA = 34,
        [Description("The fiscal memory has been disconnected. Contact the technical service.")]
        FISCAL_MEMORY_WAS_DISCONNECTED = 35,
        [Description("The EJ data are missing, replace and contact the technical service.")]
        EJ_DATA_MISSING = 36,
        [Description("The paper cover has been opened during the printing of a fiscal report.")]
        COVER_OPEN_ON_DUMP = 37,
        [Description("Electronic journal not approved. Contact the authorized dealer.")]
        SD_MMC_NOT_IDENTIFIED = 38,
        [Description("SD/MMC inserted is not usable because protected with password. Use one not protected.")]
        SD_MMC_WITH_PASSWORD = 39,
        [Description("The data searched on the electronic journal has not been found.")]
        EJ_DATA_NOT_FOUND = 40,
        [Description("It has been inserted an electronic journal not associated with the device. Insert the correct electronic journal.")]
        WRONG_EJ = 41,
        [Description("Reset the device. If the problem persists, replace the electronic journal with a new one.")]
        EJ_DATA_NOT_SAVED = 42,
        [Description("The electronic journal has been copied/cloned.")]
        EJ_CLONED = 43,
        [Description("A firmware update is ready and needs confirmation.")]
        UPGRADE_REQUIRED = 44,
        [Description("It is not possible to give the change for the chosen form of payment.")]
        CHANGE_NOT_ALLOWED = 46,
        [Description("The device is not fiscalized. Perform a fiscalization procedure.")]
        NOT_FISCALIZED = 47,
        [Description("Busy control box error. Restart the control box.")]
        BUSY_CONTROL_BOX = 48,
        [Description("Invalid serial number for control box.")]
        WRONG_CONTROL_BOX_SERIAL_NUMBER = 49,
        [Description("The control box or the fiscal memory disconnection as been detected. Contact the technical service.")]
        CB_FM_NOT_FOUND = 50,
        [Description("The fiscal memory is full. Contact the technical service.")]
        FISCAL_MEMORY_FULL = 51,
        [Description("Hardware Init. jumper inserted. Contact the technical service.")]
        HWINIT_JUMPER_INSERTED = 52,
        [Description("Attempt to serialize a fiscal memory already serialized.")]
        DEVICE_ALREADY_SERIALIZED = 53,
        [Description("The fiscal closure (fiscal zeroset) is needed.")]
        FISCAL_CLOSURE_NECESSARY = 54,
        [Description("The training mode is enabled.")]
        TRAINING_MODE_ENABLED = 55,
        [Description("The customer display is not connected.")]
        DISPLAY_NOT_FOUND = 56,
        [Description("Date/time has not been inserted.")]
        DATE_TIME_NOT_SET = 57,
        [Description("Device not fiscalized/activated because already fiscalized or with problems to the fiscal memory.")]
        DEVICE_NOT_FISCALIZED_ACTIVATED = 59,
        [Description("The device has not been serialized. Contact the technical service.")]
        DEVICE_NOT_SERIALIZED = 60,
        [Description("24 hours have passed since the last fiscal closure. Perform a fiscal closure.")]
        TWENTY_FOUR_H_FROM_LAST_CLOSURE = 61,
        [Description("Remote data reception with open receipt; close the receipt from the keyboard. The receipt in memory will be printed when the current keyboard transaction is closed.")]
        RECEIVING_DATA_IN_PROGRESS = 62,
        [Description("Transaction failed on EFT-POS.")]
        TRANSACTION_FAILED = 63,
        [Description("Make sure the paper cover is closed.")]
        COVER_OPEN = 64,
        [Description("High voltage on print head. Contact the technical service.")]
        HEAD_POWER_ERROR = 65,
        [Description("High temperature on thermal head. Contact the technical service.")]
        HEAD_TEMPERATURE_ERROR = 66,
        [Description("Cutter error. Contact the technical service.")]
        CUTTER_ERROR = 67,
        [Description("Contact the technical service.")]
        HEAD_DISCONNECTED = 68,
        [Description("Card capacity (EJ) not supported. Replace with a card (EJ) of proper capacity.")]
        SD_MMC_WRONG_LENGTH = 69,
        [Description("More than 24 hours have passed since the first sale. Perform a fiscal closure.")]
        TWENTY_FOUR_H_EXCEEDED = 70,
        [Description("A second fiscal reset is not allowed. Perform at least one sale.")]
        SECOND_Z_REPORT_NOT_ALLOWED = 71,
        [Description("A hardware init is required. Perform a hardware initialization.")]
        HWINIT_REQUIRED = 72,
        [Description("Periodic verification expired, device blocked. The technical assistance is required. Contact the technical assistance.")]
        TECHNICAL_ASSISTANCE = 73,
        [Description("Time out in receiving task.")]
        RX_TIMEOUT = 74,
        [Description("There is a fiscal electronic journal on the non-fiscal device.")]
        FISCAL_EJ = 75,
        [Description("A non-fiscal electronic journal is present on the fiscal device.")]
        NOT_FISCAL_EJ = 76,
        [Description("The fiscal memory is blocked.")]
        FISCAL_MEMORY_CLOSED = 77,
        [Description("Unable to proceed without first purchasing the license.")]
        INVALID_FW_LICENSE = 78,
        [Description("The serial number is missing.")]
        NOT_SERIALIZED = 81,
        [Description("It is required an activation.")]
        NOT_ACTIVATED = 82,
        [Description("The device has not yet been registered.")]
        DEVICE_NOT_REGISTERED = 83,
        [Description("The device SSL certificate has expired.")]
        CERTIFICATE_EXPIRED = 84,
        [Description("It is necessary a fiscal zeroset after a hardware init.")]
        PERFORM_ZEROSET_AFTER_HWINIT = 85,
        [Description("File search failed.")]
        NO_ENTRY_FOUND = 86,
        [Description("A file is still open.")]
        FILE_OPENED = 87,
        [Description("No any file is open.")]
        NO_FILE_OPENED = 88,
        [Description("Generic file system error.")]
        GENERIC_ERROR = 89,
        [Description("The end of the file has been reached.")]
        END_OF_FILE = 90,
        [Description("The requested volume is full.")]
        DISK_FULL = 91,
        [Description("There is an error in the command.")]
        INVALID_PARAMETER = 92,
        [Description("The file is write only.")]
        WRITE_ONLY = 93,
        [Description("The file is read only.")]
        READ_ONLY = 94,
        [Description("There is an error in file reading.")]
        READ_ERROR = 95,
        [Description("There is an error in file writing.")]
        WRITE_ERROR = 96,
        PLEASE_WAIT = 97,
        [Description("Error available as an echo only from the CUSTOM protocol. The device is busy while processing a keyboard operation. End the operation and return to the standby status.")]
        BUSY = 98,
        [Description("Error available as an echo only from the CUSTOM protocol. Generic error of the management engine (refer to command 1015, see CUSTOM protocol manual cod. 77100000030300).")]
        ECR = 99,
        [Description("An unknown error has been generated. Contact the technical assistance.")]
        ERROR_UNKNOWN2 = 100,
        [Description("The memory available for storing transient operations is exhausted. It is necessary to carry out the relative zeroing (for example, clearing the settlements by financial clearing).")]
        MEMORY_FULL = 101,
        [Description("The price entered on the light is higher than the maximum price set for that department.")]
        MAX_AMOUNT_PASSED = 102,
        [Description("A key has been pressed that has no function.")]
        NOT_LINKED = 103,
        [Description("A keyboard has not been selected.")]
        KEYBOARD_NOT_SELECTED = 104,
        [Description("Check the network configuration parameters.")]
        WRONG_COMMUNICATION = 105,
        [Description("Leave the device on for 24 hours. If the problem persists, contact the technical support.")]
        LOW_BATTERY = 106,
        [Description("The specified file upgrade failed. Check that the files used are the correct ones.")]
        UPGRADE_FAILED = 107,
        [Description("The input price “on the go” is lower than the minimum. value set for the department.")]
        MINIMUM_AMOUNT_NOT_REACHED = 108,
        [Description("It was not possible to reset the corresponding counter. The reset is incomplete.")]
        COUNTERS_NOT_RESET = 109,
        [Description("Control box not configured. Configure the control box.")]
        CONTROL_BOX_NOT_SET = 110,
        [Description("The memory is full. Restart the control box.")]
        DRAM_MEMORY_FULL = 111,
        [Description("Communication problem with the fiscal portal. Check the connection parameters to the fiscal portal.")]
        USER_NOT_RECOGNIZED_CHECK_USER_NAME = 112,
        [Description("Communication problem with the fiscal portal. Check the connection parameters to the fiscal portal.")]
        WRONG_FTP_PASSWORD_CHECK_PASSWORD = 113,
        [Description("The file searched on the fiscal portal does not exist.")]
        FILE_UNAVAILABLE_CHECK_SERVER_DIR = 114,
        [Description("Communication error with the portal due to incorrect syntax of the XML file.")]
        XML_SYNTAX_ERROR_CHECK_FILE_XML = 115,
        [Description("The requested function is not allowed at this moment.")]
        FUNCTION_NOT_ALLOWED = 117,
        [Description("The entered quantity exceeds 65535,99 units.")]
        WRONG_SETTING = 118,
        [Description("The entered discount is bigger than 100%.")]
        WRONG_DISCOUNT = 121,
        [Description("The entered value is not included between the minimum and the maximum value available for that parameter.")]
        VALUE_NOT_ALLOWED = 122,
        [Description("The password was not entered correctly (incorrect password or different password).")]
        WRONG_PASSWORD = 123,
        [Description("The selected VAT rate is incorrect.")]
        WRONG_VAT_RATE = 124,
        [Description("Modem transmission error.")]
        WRONG_OBEX_COMMAND = 125,
        [Description("Error in the file size used to update the modem.")]
        INVALID_FILE_SIZ_CONTROL_JAD_JAR_FILE = 126,
        [Description("Modem communication error.")]
        WRONG_AT_COMMAND = 127,
        [Description("Modem communication error.")]
        MODEM_COMMUNICATION_ERROR = 128,
        [Description("Modem communication error.")]
        WRONG_PROTOCOL_CHECKSUM = 129,
        [Description("Modem communication error.")]
        WRONG_PROTOCOL_COMMAND = 130,
        [Description("Modem communication error.")]
        WRONG_PROTOCOL_PARAMETER = 131,
        [Description("The deferred invoice is required.")]
        DEFERRED_INVOICE_REQUIRED = 132,
        [Description("The ticked invoice is required.")]
        TICKET_INVOICE_REQUIRED = 133,
        [Description("Document closure is required.")]
        CLOSE_DOCUMENT = 134,
        [Description("It has been given credit to a customer who needs to be deleted. It will not be possible to delete it until the remaining credit is collected and cleared.")]
        COLLECT_CREDITS = 135,
        [Description("The amount of cash available is not enough for the requested operation.")]
        NOT_ENOUGH_CASH = 136,
        [Description("The selected payment type is not allowed to complete the current transaction.")]
        INVALID_PAYMENT = 137,
        [Description("An attempt was made to enter a Department / PLU without specifying the quantity. You need to do this consistently with what is programmed for that department or for the department to which that PLU is connected.")]
        QUANTITY_NOT_SPECIFIED = 138,
        [Description("The description is missing.")]
        MISSING_DESCRIPTION = 139,
        [Description("In single ticket mode it is not allowed to set the operator for more than one time in a document. In shift mode it is not allowed to set the operator for more than one time in a shift.")]
        OPERATOR_ALREADY_SET = 140,
        [Description("The amount entered is higher than the maximum amount programmed for that payment.")]
        PAYMENT_AMOUNT_TOO_BIG = 141,
        [Description("The change calculated exceeds the maximum value programmed for that payment.")]
        CHANGE_TOO_BIG = 142,
        [Description("The credit that you tried to grant is higher than the maximum amount set for that customer.")]
        CUSTOMER_CREDIT_TOO_BIG = 143,
        [Description("The specified payment is not present.")]
        PAYMENT_NOT_PRESENT = 144,
        [Description("The payment method is already present.")]
        PAYMENT_ALREADY_PRESENT = 145,
        [Description("The specified modifier is not present.")]
        MODIFIER_NOT_PRESENT = 146,
        [Description("The modifier is already present.")]
        MODIFIER_ALREADY_PRESENT = 147,
        [Description("The requested department is not available because it does not exist or it has been deleted.")]
        DEPARTMENT_NOT_PRESENT = 148,
        [Description("The department is already present.")]
        DEPARTMENT_ALREADY_PRESENT = 149,
        [Description("The requested PLU is not available because it does not exist or it has been deleted.")]
        PLU_NOT_PRESENT = 150,
        [Description("The PLU is already present.")]
        PLU_ALREADY_PRESENT = 151,
        [Description("The requested operator is not available because it does not exist or it has been deleted.")]
        OPERATOR_NOT_PRESENT = 152,
        [Description("The operator is already present.")]
        OPERATOR_ALREADY_PRESENT = 153,
        [Description("The requested customer is not available because it does not exist or it has been deleted.")]
        CUSTOMER_NOT_PRESENT = 154,
        [Description("The customer is already present.")]
        CUSTOMER_ALREADY_PRESENT = 155,
        [Description("No agreement related to the company selected.")]
        NO_CORPORATE_RATES = 156,
        [Description("The corporate rate is not present.")]
        CORPORATE_RATE_NOT_PRESENT = 157,
        [Description("The corporate rate is already present.")]
        CORPORATE_RATE_ALREADY_PRESENT = 158,
        [Description("The corporate is not present.")]
        CORPORATE_NOT_PRESENT = 159,
        [Description("The corporate is already present.")]
        CORPORATE_ALREADY_PRESENT = 160,
        [Description("The requested pre-bill is not related to any pre-bill stored.")]
        PREBILL_NOT_AVAILABLE = 161,
        [Description("The number of strokes input for the table has reached the max. value of strokes for each document (150). Close the table.")]
        TABLE_FULL = 164,
        [Description("No any stroke has been input for the requested table.")]
        TABLE_EMPTY = 165,
        [Description("Before executing the requested operation close the table.")]
        CLOSE_TABLE = 166,
        [Description("Error in GPRS connection: check access point parameters.")]
        CONNECTION_ERROR = 170,
        [Description("Error in FTP connection to server: check FTP parameters.")]
        FTP_SERVER_ERROR = 171,
        [Description("Error writing file to FTP server: check the FTP connection with the server.")]
        FILE_WRITING_ERROR = 172,
        [Description("SIM card error: check if the SIM card is present and if it is inserted correctly in the modem.")]
        SIM_CARD_ERROR = 173,
        [Description("FTP connection error: check transfer folder name on the server.")]
        FTP_CHDIR_ERROR_CHECK_ID_SERIAL = 174,
        [Description("Wrong dimension of Z file: check Z_XML file on lash-disk.")]
        WRONG_XML_FILE_SIZE = 175,
        [Description("The SIM card requires a PIN code.")]
        INSERT_PIN = 176,
        [Description("Wrong dimension of JAD file: check it on lash-disk")]
        WRONG_JAD_FILE_SIZE = 177,
        [Description("Error in the SIM card operator selection.")]
        ERROR_SELECTED_OPERATOR = 178,
        [Description("Error in FTP connection: the connection has been already opened.")]
        ERROR_FTP_OPEN_CONNECTION = 179,
        [Description("The ticket is not assigned to any valid event.")]
        INVALID_EVENT = 180,
        [Description("The maximum capacity has been reached.")]
        MAXIMUM_CAPACITY_REACHED = 181,
        [Description("Error in transferring file from FTP server to printer.")]
        ERROR_FTP_FILE_TRANSFER = 182,
        [Description("Busy modem: serial communication busy with another command.")]
        ERROR_MODEM_BUSY = 183,
        [Description("Error in the received ACK file.")]
        WRONG_ACK_CODE = 184,
        [Description("Error in sending Z report.")]
        ERROR_SENDING_Z_REPORT = 185,
        [Description("The serial number parameter is not correctly set in the modem database.")]
        EFD_SERIAL_NUMBER_NOT_SET = 186,
        [Description("Failure during the opening of the FTP connection.")]
        FTP_CONNECTION_NOT_OPEN = 187,
        [Description("Error deleting file on FTP server.")]
        ERROR_FTP_FILE_DELETE = 188,
        [Description("Declare the customer before closing the document.")]
        MANDATORY_CUSTOMER = 189,
        [Description("The paid out amount is too high.")]
        PAID_OUT_TOO_HIGH = 190,
        [Description("The function you are trying to use must be enabled from the appropriate menu item.")]
        FUNCTION_NOT_ACTIVATED = 191,
        [Description("The memory has reached its maximum capacity. Perform a financial zeroset.")]
        MEMORY_FULL_PERFORM_FINANCIAL_CLOSURE = 192,
        [Description("Error saving the last fiscal reset on the lash-disk")]
        ERROR_SAVING_Z_REPORT = 193,
        [Description("Repeat the synchronization operation.")]
        MISSING_CONFIRMATION = 194,
        [Description("Check the FTP connection parameters.")]
        FTP_I_O_ERROR = 195,
        [Description("Check the general configuration.")]
        WRONG_EXTENDED_COMMAND = 196,
        [Description("Enter the customer code in the registration parameters.")]
        CUSTOMER_CODE_MISSING = 197,
        [Description("Error in entering the business license number (VAT_Registration Number, VRN).")]
        INSERT_VRN = 198,
        [Description("An invalid VAT rate has been entered.")]
        INSERT_TAX_OFFICE = 199,
        [Description("An invalid registration date has been entered.")]
        INSERT_REG_DATE = 200,
        [Description("Check the customer code in the registration parameters.")]
        INVALID_CUSTOMER_CODE = 201,
        [Description("Check the serial number.")]
        INVALID_SERIAL_NUMBER = 202,
        [Description("Check if the device has already been registered before.")]
        LICENSE_ACTIVE = 203,
        [Description("Exhausted the number of licenses available for new devices.")]
        NO_LICENSE_AVAILABLE = 204,
        [Description("Contact the technical assistance.")]
        REGISTRATION_ERROR = 205,
        [Description("The device needs to be connected to the mains and it needs to be recharged.")]
        BATTERY_EXHAUST = 206,
        [Description("The entry of the technician identification is required (Technical Identification Number, TIN).")]
        INSERT_TIN = 207,
        [Description("The day’s data could not be sent to the Revenue Authority.")]
        ACK_FILE_NOT_RECEIVED = 208,
        [Description("Check the network configuration parameters or the configuration parameters of the Secure Ticket service")]
        ANSWER_HTTP_SERVER_ERROR = 209,
        [Description("Error in cryptography operation.")]
        CRYPTOGRAPHY_ERROR = 210,
        [Description("Check the Ethernet configuration parameters or the configuration parameters of the Secure Ticket service.")]
        MMC_DOWNLOAD_ERROR = 211,
        [Description("An error occurred during fiscal initialization.")]
        ERROR_DURING_FISCAL_INITIALIZATION = 212,
        [Description("The device is busy during the execution of telematic operations.")]
        DEVICE_BUSY_IN_TELEMATIC_OPERATIONS = 213,
        [Description("A daily fiscal closure must be performed.")]
        PERIOD_OF_INACTIVITY_PENDING = 214,
        [Description("The VAT code entered is incorrect.")]
        INCORRECT_VAT_CODE = 215,
        [Description("The device has been opened / manipulated. Contact the technical assistance.")]
        TAMPER_OPEN = 216,
        [Description("The device is busy during the export of the XML file.")]
        BUSY_DURING_XML_EXPORT = 217,
        [Description("The device is exporting the receipt lottery XML file.")]
        EXPORT_LOTTERY_FILE_IN_PROGRESS = 218,
        [Description("An attempt is made to send an XML file to the device but the communication protocol set is incorrect.")]
        WRONG_PROTOCOL = 219,
        [Description("An attempt is made to send an XML file to the device but it is not in the correct mode (FPU mode).")]
        WRONG_FPU_MODE = 220,
        [Description("The device is busy executing a report that provides for the search and extraction of a number of receipts from the lottery database.")]
        LOTTERY_SEARCH_IN_PROGRESS = 221,
        [Description("The device can not process the commands because it is in a wrong status.")]
        WRONG_ECR_STATUS = 222,
        [Description("It is not possible to access to the fiscal memory or to electronic journal contents since the operator does not own the permissions.")]
        NOT_POSSIBLE_TO_ACCESS_TO_FM_OR_EJ = 223,
        [Description("Critical section timeout error.")]
        SYSTEM_ERROR = 229,
        [Description("Scale not present, not connected or switched of")]
        SCALE_NOT_PRESENT = 232,
        [Description("The weight detected and communicated by the scale is zero.")]
        WEIGHT_IS_ZERO = 233,
        [Description("The Ethernet service did not start correctly.")]
        ETHERNET_SERVICE_ERROR = 235,
        [Description("The device migration was not authorized.")]
        UNAUTHORIZED_MIGRATION = 236,
        [Description("The device migration is failed.")]
        MIGRATION_ERROR = 237,
        [Description("The certificate is not present in the fiscal memory.")]
        MISSING_CERTIFICATE = 244,
        [Description("Invalid XML document.")]
        INVALID_XML = 245,
        [Description("The device has already been migrated.")]
        MIGRATION_ALREADY_DONE = 248,
        [Description("Error in response to the requested Ethernet service.")]
        ACK_ERROR_MESSAGE = 249,
        [Description("The device has already been activated.")]
        DEVICE_ALREADY_ACTIVATED = 250,
        [Description("Error loading private key or device certificate.")]
        LOADING_CRYPTOGRAPHY_ERROR = 251,
        [Description("Traffic light for sending busy XML data due to the automatic sending task.")]
        ETHERNET_COMMUNICATION_BUSY = 252,
        [Description("The private key is not present.")]
        MISSING_PRIVATE_KEY = 253,
        [Description("Error creating the signed certificate request (CSR) of the device.")]
        CSR_CREATION_ERROR = 254,
        [Description("Error creating XML response object.")]
        INVALID_XML_RESPONSE = 255,
        [Description("The Ethernet service is not enabled.")]
        ETHERNET_SERVICE_DISABLED = 256,
    }
}