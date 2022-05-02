using System;
using System.Text;
using fiskaltrust.ifPOS.v1.me;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class SignatureFactoryME
    {
        public SignatureFactoryME()
        {
        }

        public string ICCConcatenate(string issuerTIN, DateTime issuesDateTime, string invOrdNum, string businUnitCode, string enuCode, string softwareCode, decimal totPrice)
        {
            return ConvertToUTF8(string.Concat(issuerTIN,'|', issuesDateTime.ToString("u"), '|', invOrdNum.ToString(), '|', businUnitCode, '|', enuCode, '|', softwareCode, '|', totPrice));
        }

        private string ConvertToUTF8(string input) {
            var bytes = Encoding.Default.GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
