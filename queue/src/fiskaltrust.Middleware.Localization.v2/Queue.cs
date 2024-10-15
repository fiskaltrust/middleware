using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Synchronizer;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2
{
    public class Queue
    {
        private readonly ISignProcessor _signProcessor;
        private readonly EchoProcessor _echoProcessor;

        public required Guid Id { get; set; }
        public required Dictionary<string, object> Configuration { get; set; }

        public Queue(ISignProcessor signProcessor, ILoggerFactory loggerFactory)
        {
            _signProcessor = new LocalQueueSynchronizationDecorator(signProcessor, loggerFactory.CreateLogger<LocalQueueSynchronizationDecorator>());
            _echoProcessor = new EchoProcessor();
        }

        public Func<string, Task<string>> RegisterForEcho()
        {
            return async (message) =>
            {
                var request = JsonSerializer.Deserialize<ifPOS.v1.EchoRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                var response = await _echoProcessor.ProcessAsync(request);
                return JsonSerializer.Serialize(response);
            };
        }

        public Func<string, Task<string>> RegisterForSign()
        {
            return async (message) =>
            {
                var request = JsonSerializer.Deserialize<ReceiptRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                var response = await _signProcessor.ProcessAsync(request);
                return JsonSerializer.Serialize(response);
            };
        }
    }
}
