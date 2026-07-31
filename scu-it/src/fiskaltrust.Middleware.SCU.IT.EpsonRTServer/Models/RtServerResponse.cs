using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models
{
    /// <summary>
    /// Represents the XML response returned by the Epson RT Server SOAP endpoints:
    /// <![CDATA[ <response success="true" code="0" status="OK"><addInfo>...</addInfo></response> ]]>
    /// </summary>
    public class RtServerResponse
    {
        public bool Success { get; set; }

        /// <summary>The response code. 0 (or empty for fpmate.cgi) means success; negative codes are errors.</summary>
        public string Code { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        /// <summary>Flattened addInfo child elements (element name -> inner text).</summary>
        public Dictionary<string, string> AddInfo { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The raw XML body for diagnostics.</summary>
        public string RawResponse { get; set; } = string.Empty;

        public int CodeAsInt => int.TryParse(Code, out var value) ? value : 0;

        public string? GetAddInfo(string key) => AddInfo.TryGetValue(key, out var value) ? value : null;

        public static RtServerResponse Parse(string xml)
        {
            var response = new RtServerResponse { RawResponse = xml };
            var document = XDocument.Parse(xml);
            var responseElement = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "response")
                ?? throw new FormatException($"The RT Server response does not contain a 'response' element. Raw: {xml}");

            response.Success = string.Equals(responseElement.Attribute("success")?.Value, "true", StringComparison.OrdinalIgnoreCase);
            response.Code = responseElement.Attribute("code")?.Value ?? string.Empty;
            response.Status = responseElement.Attribute("status")?.Value ?? string.Empty;

            var addInfo = responseElement.Elements().FirstOrDefault(x => x.Name.LocalName == "addInfo");
            if (addInfo != null)
            {
                foreach (var element in addInfo.Elements())
                {
                    // Store the first-level text of each addInfo child (e.g. fingerPrint, zRepNumber, token, publicKey).
                    var text = element.Nodes().OfType<XText>().Select(x => x.Value).FirstOrDefault()?.Trim()
                        ?? element.Value?.Trim()
                        ?? string.Empty;
                    response.AddInfo[element.Name.LocalName] = text;
                }
            }

            return response;
        }
    }
}
