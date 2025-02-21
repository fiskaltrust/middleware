#if WCF
using Newtonsoft.Json;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.Interface.Tests.Helpers.Wcf.Formatting
{
    public class JsonDispatchMessageFormatter : IDispatchMessageFormatter
    {
        private static DateFormat _dateFormat = DateFormat.Undefined;

        private readonly IDispatchMessageFormatter _basicDispatchMessageFormatter;
        private readonly OperationDescription _operation;

        public JsonDispatchMessageFormatter(OperationDescription operation, IDispatchMessageFormatter inner)
        {
            _operation = operation;
            _basicDispatchMessageFormatter = inner;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            var messageType = _operation.Messages.FirstOrDefault()?.Body.Parts.FirstOrDefault()?.Type;

            if (messageType != default && messageType != typeof(string) && IsJsonContentTypeRequest(message) && !message.IsEmpty && _dateFormat != DateFormat.WCF)
            {
                var messageBuffer = message.CreateBufferedCopy(int.MaxValue);

                var bodyReader = messageBuffer.CreateMessage().GetReaderAtBodyContents();
                var element = XElement.Load(bodyReader);
                if (_dateFormat != DateFormat.ISO && RequestContainsWcfDates(element))
                {
                    _dateFormat = DateFormat.WCF;
                    _basicDispatchMessageFormatter.DeserializeRequest(messageBuffer.CreateMessage(), parameters);
                }
                else
                {
                    _dateFormat = DateFormat.ISO;

                    element.TransformToBeJsonCompliant();
                    var fromXml = JsonConvert.SerializeXNode(element, Newtonsoft.Json.Formatting.None, true);
                    var jsonObj = JsonConvert.DeserializeObject(fromXml, messageType);
                    parameters[0] = jsonObj;
                }
            }
            else
            {
                _basicDispatchMessageFormatter.DeserializeRequest(message, parameters);
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var replyMessage = _basicDispatchMessageFormatter.SerializeReply(messageVersion, parameters, result);
            if (IsJsonContentTypeResponse(replyMessage) && _dateFormat != DateFormat.WCF)
            {
                string json = JsonConvert.SerializeObject(result);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                var message = Message.CreateMessage(messageVersion, _operation.Messages[1].Action, new RawDataWriter(bytes));
                message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
                var responseMessageProperty = new HttpResponseMessageProperty();
                responseMessageProperty.Headers.Add("Content-Type", "application/json");
                message.Properties.Add(HttpResponseMessageProperty.Name, responseMessageProperty);

                return message;
            }

            return replyMessage;
        }

        private bool RequestContainsWcfDates(XElement element)
        {
            return element.Descendants()
                .Where(desc => !desc.Elements().Any())
                .Any(x => x.Value.StartsWith("/Date"));
        }

        private bool IsJsonContentTypeResponse(Message message)
        {
            return message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out var value)
                && value is WebBodyFormatMessageProperty property
                && property.Format == WebContentFormat.Json;
        }

        private bool IsJsonContentTypeRequest(Message message)
        {
            if (message.Properties.TryGetValue("httpRequest", out object header) && header is HttpRequestMessageProperty)
            {
                var contentTypeVals = ((HttpRequestMessageProperty)header).Headers.GetValues("Content-Type");
                return contentTypeVals != null && contentTypeVals.Any(v => v.Contains("application/json"));
            }
            return false;
        }
    }
}
#endif