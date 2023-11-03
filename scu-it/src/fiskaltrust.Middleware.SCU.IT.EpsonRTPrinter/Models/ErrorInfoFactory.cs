using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models
{
    public class ErrorInfoFactory
    {
        private readonly Dictionary<string, string> _epsonErrorCodes = new()
            {
                { "NO_DATA", "Empty HTTP POST SOAP request" },
                { "NO_RAM", "Printer out of RAM memory" },
                { "PARSER_ERROR", "Malformed XML file" },
                { "LAN_ERROR", "FpMate CGI service fiscal firmware intercommunication timeout during initial communication phase (before XML line processing)" },
                { "LAN_TIME_OUT", "FpMate CGI service fiscal firmware intercommunication timeout during XML line processing" },
                { "FP_NO_ANSWER", "Fiscal firmware did not respond to FpMate CGI service command within timeout" },
                { "TM_NO_ANSWER/OFF_LINE", "TM Connection reset by peer error or timeout" },
                { "CONFIGURATION_FILE_ERROR", "Empty or non-existent secondary devices XML configuration file" },
                { "INCOMPLETE FILE", "XML file does not contain the minimum elements necessary (missing endFiscalReceipt for example)" },
                { "Non valid XML command", "Unknown or misplaced subelement (endNonFiscal in an invoice for example)" },
                { "EPTR_REC_EMPTY", "Problem due to cover opeprinter offline or no paper roll loaded" },
                { "PRINTER ERROR", "Problem due to fiscal printer at the native communication level." },
                { "EFT_POS_ERROR", "Electronic payment error" },
                { "FP_NO_ANSWER_NETWORK", "JavaScript library ontimeout\r\nevent or HTTP error.\r\nFor example:\r\n HTTP 404 – No page found or \r\nnetwork error" }
            };

        private readonly Dictionary<int, string> _epsonErrorStatus = new()
            {
            { 2, "CARTA SCONTRINO - It is a warning rather than an error.Flag SET 14/11 activates or deactivates the warning.If " +
                 "activated and the paper is low, the printer generates this warning in response to theb payment or closure commands." +
                 "The UPOS fiscal driver automatically deactivates this warning; pap" +
                "er status is determined through the regular printer status requests." },
            { 3, "OFFLINE - The printer has gone offline(ERROR LED ON). Either the paper has finished, or the cover has been opened." },
            { 7, "SLIP KO Only valid if an external Slip printer is connected to the fiscal printer via the serial port. Indicates a slip printer problem." },
            { 8, "TASTO ERRATO - Invalid key press." },
            { 9, "DATA INFERIORE - Invalid past date entered. The date cannot be earlier than the date of the last fiscal closure report." },
            { 10, "DATA ERRATA - Bad date format.E.g. 33022021." },
            { 11, "SEQUENZA ERRATA - Command sequence not allowed. The command cannot be used at this point in the sequence. See examples below." },
            { 12, "DATI INESISTENTI - Inexistent data. For example, attempting to use a PLU that has not been programmed or barcode received does not match any PLU in the database." },
            { 13, "VALORE ERRATO - Generic error. One or more fields contains an erroneous value." },
            { 14, "PROG MATRICOLA - No fiscal serial number has been programmed." },
            { 15, "GIA ESISTENTE - An attempt has been made to perform an operation that has already been carried out. For example, trying to program a PLU " +
                  "with a barcode that has already been set on another PLU."},
            { 16, "NON PREVISTO - Generic error. An invalid index parameter or an inexistent H1 H2 command pair has been received." },
            { 17, "IMPOSSIBILE - ORA Generic error. It is not possible to carry out the operation at this time." },
            { 18, "NON POSSIBILE - Generic error. It is not possible to carry out the operation." },
            { 19, "SCRITTA INVALIDA - Obsolete." },
            { 20, "SUPERA VALORE - The amount is greater than the maximum allowed." },
            { 21, "SUPERA LIMITE - A parameter value is outside the permitted range or maximum daily total reached." },
            { 22, "NON PROGRAMMATO - The printer has received a command that requires prior programming." },
            { 23, "CHIUDI SCONTRINO - The maximum number of operations has been reached and the document must be closed with a single payment or cancelled. The current limit is around 1000 operations." },
            { 24, "CHIUDI PAGAMENTO - The maximum number of operations has been reached whilst partial payments are being printed.The document must be closed with a single final payment or cancelled.The same " +
                  "1000 operations limit applies." },
            { 25, "MANCA OPERATORE - Only valid if operator mode has been enabled. No operator has been selected." },
            { 26, "CASSA INFERIORE - An attempt has been made to perform a cash out operation or document change of an amount greater than the current cash drawer total." },
            { 27, "OLTRE PROGRAMMAZIONE - The sale price (unit price x quantity) is greater than the programmed department limit." },
            { 28, "P.C.NON CONNESSO - No PC or server connection or bad sequence termination.Server includes SMTP mail server." },
            { 29, "MANCA MODULO - Only valid if an external Slip printer is connected to the fiscal printer via the serial port. Indicates that no form has been inserted." },
            { 30, "CHECKSUM ERRATO - Partita IVA (business tax code), codice fiscale (personal tax code) or lottery code checksum error." },
            { 34, "MANCA ATTIVAZIONE - Missing activation. For example, attempt to open an invoice when invoice printing has been deactivated." },
            { 35, "SLIP:CONNESSIONE ? - Only valid if an external Slip printer is connected to the fiscal printer via the serial port. Indicates a slip printer connection problem." },
            { 37, "RIMUOVERE MODULO - Only valid if an external Slip printer is connected to the fiscal printer via the serial port. Indicates form removal. More of an instruction than an error." },
            { 38, "EFT-POS in ERRORE - EFT-POS error" },
            { 39, "DOC già ANNULLATO - Commercial document already voided." },
            { 40, "DOC già RESO - Commercial document already refunded." },
            { 41, "TIPO NON VALIDO (DOC di ANNULLO) - Reference document cannot be a void document. It must a commercial document." },
            { 42, "TIPO NON VALIDO (DOC di RESO) - Reference document cannot be a refund document. It must a commercial document." },
        };

        public string GetCodeInfo(string code) => _epsonErrorCodes.ContainsKey(code) ? _epsonErrorCodes[code] : string.Empty;

        public string GetStatusInfo(int status) => _epsonErrorStatus.ContainsKey(status) ? _epsonErrorStatus[status] : string.Empty;
    }
}
