using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    public class ErrorCodeFactory
    {
        private readonly Dictionary<string, string> _epsonErrorCodes = new();
        public ErrorCodeFactory() => init();

        private void init()
        {
            _epsonErrorCodes.Add("NO_DATA", "Empty HTTP POST SOAP request");
            _epsonErrorCodes.Add("NO_RAM", "Printer out of RAM memory");
            _epsonErrorCodes.Add("PARSER_ERROR", "Malformed XML file");
            _epsonErrorCodes.Add("LAN_ERROR", "FpMate CGI service fiscal firmware intercommunication timeout during initial communication phase (before XML line processing)");
            _epsonErrorCodes.Add("LAN_TIME_OUT", "FpMate CGI service fiscal firmware intercommunication timeout during XML line processing");
            _epsonErrorCodes.Add("FP_NO_ANSWER", "Fiscal firmware did not respond to FpMate CGI service command within timeout");
            _epsonErrorCodes.Add("TM_NO_ANSWER/OFF_LINE", "TM Connection reset by peer error or timeout");
            _epsonErrorCodes.Add("CONFIGURATION_FILE_ERROR", "Empty or non-existent secondary devices XML configuration file");
            _epsonErrorCodes.Add("INCOMPLETE FILE", "XML file does not contain the minimum elements necessary (missing endFiscalReceipt for example)");
            _epsonErrorCodes.Add("Non valid XML command", "Unknown or misplaced subelement (endNonFiscal in an invoice for example)");
            _epsonErrorCodes.Add("EPTR_REC_EMPTY", "Problem due to cover opeprinter offline or no paper roll loaded");
            _epsonErrorCodes.Add("PRINTER ERROR", "Problem due to fiscal printer at the native communication level.");
            _epsonErrorCodes.Add("EFT_POS_ERROR", "Electronic payment error");
            _epsonErrorCodes.Add("FP_NO_ANSWER_NETWORK", "JavaScript library ontimeout\r\nevent or HTTP error.\r\nFor example:\r\n HTTP 404 – No page found or \r\nnetwork error");
        }
     
        public string GetErrorInfo(string code)
        {
            if(_epsonErrorCodes.ContainsKey(code))
            {  
                return _epsonErrorCodes[code]; 
            }
            return "";
        }
    }
}
