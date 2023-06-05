using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.IT.Epson.Extensions
{
    public static class StringExtension
    {
        public static string RemoveListObjectsForEpsonXml(this string xml)
        {
            return xml.Replace("<NotExistingOnEpsonItemMsg>\r\n", "")
                .Replace("</NotExistingOnEpsonItemMsg>\r\n", "")
                .Replace("<NotExistingOnEpsonAdjMsg>\r\n", "")
                .Replace("</NotExistingOnEpsonAdjMsg>\r\n", "")
                .Replace("<NotExistingOnEpsonTotalMsg>\r\n", "")
                .Replace("</NotExistingOnEpsonTotalMsg>\r\n", "");
        }
    }
}
