using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    public class ErrorCodeFactory
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

        public string GetErrorInfo(string code)
        {
            return _epsonErrorCodes.ContainsKey(code) ? _epsonErrorCodes[code] : string.Empty;
        }
    }
}
