using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Models
{
    public class ErrorCode
    {
        private readonly Dictionary<string, string> epsonErrorCodes = new Dictionary<string, string>();
        public ErrorCode() { init(); }

        private void init()
        {
            epsonErrorCodes.Add("NO_DATA", "Empty HTTP POST SOAP request");
            epsonErrorCodes.Add("NO_RAM", "Printer out of RAM memory");
            epsonErrorCodes.Add("PARSER_ERROR", "Malformed XML file");
            epsonErrorCodes.Add("LAN_ERROR", "FpMate CGI service fiscal firmware intercommunication timeout during initial communication phase (before XML line processing)");
            epsonErrorCodes.Add("LAN_TIME_OUT", "FpMate CGI service fiscal firmware intercommunication timeout during XML line processing");
            epsonErrorCodes.Add("FP_NO_ANSWER", "Fiscal firmware did not respond to FpMate CGI service command within timeout");
            epsonErrorCodes.Add("TM_NO_ANSWER/OFF_LINE", "TM Connection reset by peer error or timeout");
            epsonErrorCodes.Add("CONFIGURATION_FILE_ERROR", "Empty or non-existent secondary devices XML configuration file");
            epsonErrorCodes.Add("INCOMPLETE FILE", "XML file does not contain the minimum elements necessary (missing endFiscalReceipt for example)");
            epsonErrorCodes.Add("Non valid XML command", "Unknown or misplaced subelement (endNonFiscal in an invoice for example)");
            epsonErrorCodes.Add("EPTR_REC_EMPTY", "Problem due to cover opeprinter offline or no paper roll loaded");
            epsonErrorCodes.Add("PRINTER ERROR", "Problem due to fiscal printer at the native communication level. For example, discount amount = 0. For a complete list of possible errors, please refer to the Communication Protocol document and the A.PDU Errors chapter");
            epsonErrorCodes.Add("EFT_POS_ERROR", "Electronic payment error");
            epsonErrorCodes.Add("FP_NO_ANSWER_NETWORK", "JavaScript library ontimeout\r\nevent or HTTP error.\r\nFor example:\r\n HTTP 404 – No page found or \r\nnetwork error");
            epsonErrorCodes.Add("PARSER_ERROR", "Malformed XML file");
        }
     
        public string GetErrorInfo(string code)
        {
            return epsonErrorCodes[code];
        }
    }
}
