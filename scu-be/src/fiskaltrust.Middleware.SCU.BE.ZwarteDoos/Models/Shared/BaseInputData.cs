using System;
using System.Reflection;
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Converters;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Enums;
using fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Sale;

namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos.Models.Shared;

public class BaseInputData
{
    /// <summary>
    /// The language the POS system is operating in. This language will be used in messages received from the fiscal data module. It is recommended to also set the HTTP Language header to an appropriate value in case an error is returned before the language field is parsed and evaluated.
    /// </summary>
    [JsonPropertyName("language")]
    public required Language Language { get; set; } = Language.NL;

    /// <summary>
    /// The Belgian VAT identification number of the business. It is composed of 12 characters and starts with the country code "BE" followed by 10 digits. The first digit after the country code is always either a 0 or 1. The last two digits form a module 97 checksum on the first eight digits of the VAT registration number.
    /// </summary>
    [JsonPropertyName("vatNo")]
    public required string VatNo { get; set; }

    /// <summary>
    /// A business identified by its VAT registration number can operate in multiple locations. These locations are registered with an establishment unit number. The establishment unit number is 10 digits long and the first digit is a number in the range 2 through 8. The last two digits form a module 97 checksum on the first eight digits.
    /// 
    /// NL
    /// vestigingseenheidsnummer
    /// FR
    /// unité d'établissement
    /// DE
    ///  Nummer der Niederlassungseinheit
    /// </summary>
    [JsonPropertyName("estNo")]
    public required string EstNo { get; set; }

    /// <summary>
    /// The identification of the point of service system that is registering the transaction.
    ///
    /// Format
    ///     CXXXNNNPPPPPPP(14 characters)
    /// C
    /// The type prefix "C". There is no longer a distinction made between an ECR(type "A") and a PC based POS system(type "B").
    /// 
    /// XXX
    /// Identification of the manufacturer(characters in the range A-Z, assigned by FPS Finance).
    /// 
    /// NNN
    /// Model number of the POS system(digits in the range 0-9, assigned by FPS Finance).
    /// 
    /// PPPPPPP
    /// Production number, determined by the manufacturer(characters in the range A-Z and/or digits in the range 0-9).
    /// </summary>
    [JsonPropertyName("posId")]
    public required string PosId { get; set; }

    /// <summary>
    /// Ticket number assigned to the transaction by the POS system. The number is taken from an uninterrupted sequence from 1 to 999999999. This sequence can be generated across all event types and all terminals, or generated per event type, or per terminal, or per event type and terminal combination. When the maximum value is reached, the POS restarts with number 1.
    /// </summary>
    [JsonPropertyName("posFiscalTicketNo")]
    public required long PosFiscalTicketNo { get; set; }

    /// <summary>
    /// The date and time, in local time, of the point of service at the time of sending the transaction to the fiscal data module (the local time is affected by daylights savings and should be reported appropriately). When the POS system supports it, the time portion should include milliseconds.
    /// ISO 8601 format (RFC3339): YYYY-MM-DDTHH:MM:SS+HH:SS (i.e. 2025-11-03T15:35:54+01:00)
    /// </summary>
    [JsonPropertyName("posDateTime")]
    [JsonConverter(typeof(Iso8601DateTimeConverter))]
    public required DateTime PosDateTime { get; set; }

    /// <summary>
    /// The current software version of the POS system. A maximum of 36 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field. For example "1.8.3".
    /// </summary>
    [JsonPropertyName("posSwVersion")]
    public required string PosSwVersion { get; set; }

    /// <summary>
    /// When the POS system has multiple points of service each point of service has to be assigned a unique identifier. Systems that are not mutlti-terminal can provide any non-white space string value with a length of at least one character, but they have to provide that same value with each request as not to conflict with multi-terminal setups. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field. For example "BAR", "KIOSK-01", "002", "TERM005", etc.
    /// </summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; set; }

    /// <summary>
    /// The hardware identification of the physical device on which the transaction was registered. This can be a tablet, smartphone, kiosk, standalone POS system, etc. The identification can be the MAC-address, a manufacturer's serial number, a label attached to the device, etc. A maximum of 600 characters is allowed with a minimum of 1 character. Leading or trailing whitespace characters are not allowed in the field.
    /// </summary>
    [JsonPropertyName("deviceId")]
    public required string DeviceId { get; set; }

    /// <summary>
    /// A GUID, in lowercase and with dashes, identifying the period of operation of the POS system within the booking date. A booking date can have one or more periods. A period could be the lunch shift, the dinner shift, etc. Example: dffcd829-a0e5-41ca-a0ae-9eb887f95637.
    /// </summary>
    [JsonPropertyName("bookingPeriodId")]
    public required Guid BookingPeriodId { get; set; }

    /// <summary>
    /// Accounting date on which the transaction is recorded. The accounting date and the calendar date is not necessarily the same. An accounting date can start at 4 AM and end at 4 AM the next calendar date for example.
    ///  ISO 8601 format: YYYY-MM-DD (i.e. 2025-11-03)
    /// </summary>
    [JsonPropertyName("bookingDate")]
    public required DateOnly BookingDate { get; set; }

    /// <summary>
    /// Specifies the method of ticket delivery used by the POS system for the transaction.
    /// </summary>
    [JsonPropertyName("ticketMedium")]
    public required TicketMedium TicketMedium { get; set; }

    /// <summary>
    /// The social security number of the POS operator recording the transaction. This number contains 11 digits in the range 0 through 9. The last two digits are a module 97 checksum on the preceding digits for persons born before the year 2000, or on the sum of 2000000000 and the preceding digits for persons born later.
    ///
    /// This number MUST not be printed on the tickets or any customer-facing displays as it is not only subject to GDPR but also more stringent Belgian privacy laws. The POS system needs to contain a table or list that translates the social security number to the operator identifier (i.e. "OPER-001", "Emma", etc.) used on the receipts and displays.
    ///
    /// An operator foreign to the business (i.e. a technician of the POS supplier) who registers transactions (i.e. for testing or troubleshooting purposes) must be identified with number 00000000097. Online orders, kiosk orders, and all future possible order input systems that do not require a physical operator but are booking transactions under the responsibility of the business, have to be identified with "Robot-User" 00000000029.
    /// </summary>
    [JsonPropertyName("employeeId")]
    public required string EmployeeId { get; set; }
}
