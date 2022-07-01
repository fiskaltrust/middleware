using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService.Helpers
{
    public class FormatBehaviour : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) => clientRuntime.ClientMessageInspectors.Add(new MeFormatter());

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }

        public class MeFormatter : IClientMessageInspector
        {
            public void AfterReceiveReply(ref Message reply, object correlationState) { }

            public object BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                var msgbuf = request.CreateBufferedCopy(int.MaxValue);

                var oldMessage = msgbuf.CreateMessage();
                var xdr = oldMessage.GetReaderAtBodyContents();

                var xmlRequest = XDocument.Load(xdr.ReadSubtree());
                xdr.Close();

                FormatElements(xmlRequest.Elements());

                var ms = new MemoryStream();
                var xw = XmlWriter.Create(ms);
                xmlRequest.WriteTo(xw);
                xw.Flush();
                xw.Close();
                ms.Position = 0;
                var xr = XmlReader.Create(ms);

                var message = Message.CreateMessage(request.Version, null, xr);
                message.Headers.CopyHeadersFrom(request);
                message.Properties.CopyProperties(request.Properties);

                request = message;
                return null;
            }
        }

        private static void FormatElements(IEnumerable<XElement> elements)
        {
            foreach (var element in elements)
            {
                FormatElements(element.Elements());
                FormatAttributes(element.Attributes());
            }
        }

        private static void FormatAttributes(IEnumerable<XAttribute> attributes)
        {
            var formats = new Dictionary<string, (bool, List<string>)>
            {
                { "MM/yyyy", (false, new List<string>
                    {
                        "TaxPeriod"
                    })
                },
                { "yyyy-MM-dd", (false, new List<string>
                    {
                        "PayDeadline",
                        "VD",
                        "Start",
                        "End",
                        "ValidFrom",
                        "ValidTo",
                        "D",
                    })
                },
                { @"yyyy-MM-dd\THH:mm:ss\Z", (true, new List<string>
                    {
                        "IssueDateTime",
                        "ChangeDateTime",
                        "SendDateTime",
                        "IssueDateTime",
                    })
                },
            };
            var formatdecs = new Dictionary<string, List<string>>
            {
                { "{0:0.00}",  new List<string>
                    {
                        "Amt",
                        "PA",
                        "PB",
                        "UPA",
                        "UPB",
                        "VA",
                        "VR",
                        "R",
                        "TotPriceWoVAT",
                        "GoodsExAmt",
                        "TaxFreeAmt",
                        "TotVATAmt",
                        "VATRate",
                        "PriceBefVAT",
                        "TotPrice",
                    }
                },
            };


            foreach (var attribute in attributes)
            {
                foreach ( var formatdec in formatdecs)
                {
                    if (formatdec.Value.Contains(attribute.Name.LocalName))
                    {
                        var decVal = decimal.Parse(attribute.Value, CultureInfo.GetCultureInfo("en-EN"));
                        decVal = Math.Round(decVal,2);
                        var parsed = string.Format(CultureInfo.GetCultureInfo("en-EN"), formatdec.Key, decVal);
                        attribute.SetValue(parsed);
                    }
                }
                foreach (var format in formats)
                {
                    if (format.Value.Item2.Contains(attribute.Name.LocalName))
                    {
                        var parsed = DateTime.Parse(attribute.Value, CultureInfo.InvariantCulture);
                        if (format.Value.Item1)
                        {
                            parsed = parsed.ToUniversalTime();
                        }
                        attribute.SetValue(parsed.ToString(format.Key));
                    }
                }
            }
        }
    }
}