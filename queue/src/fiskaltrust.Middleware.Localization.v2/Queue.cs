using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Synchronizer;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2
{
    public class Queue
    {
        private readonly ISignProcessor _signProcessor;
        private readonly JournalProcessor _journalProcessor;
        private readonly EchoProcessor _echoProcessor;

        public required Guid Id { get; set; }
        public required Dictionary<string, object> Configuration { get; set; }

        public Queue(ISignProcessor signProcessor, JournalProcessor journalProcessor, ILoggerFactory loggerFactory)
        {
            _signProcessor = new LocalQueueSynchronizationDecorator(signProcessor);
            _journalProcessor = journalProcessor;
            _echoProcessor = new EchoProcessor();
        }

        public Func<string, Task<string>> RegisterForEcho()
        {
            return async (message) =>
            {
                var request = JsonSerializer.Deserialize<ifPOS.v2.EchoRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                var response = await _echoProcessor.ProcessAsync(request);
                return JsonSerializer.Serialize(response);
            };
        }

        public Func<string, Task<string>> RegisterForSign()
        {
            return async (message) =>
            {
                var request = JsonSerializer.Deserialize<ifPOS.v2.ReceiptRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                var response = await _signProcessor.ProcessAsync(request);
                return JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
            };
        }

        public Func<string, Task<(ContentType, PipeReader)>> RegisterForJournal()
        {
            return async (message) =>
            {
                var request = JsonSerializer.Deserialize<ifPOS.v2.JournalRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                return await _journalProcessor.ProcessAsync(request);
            };
        }
    }
}
