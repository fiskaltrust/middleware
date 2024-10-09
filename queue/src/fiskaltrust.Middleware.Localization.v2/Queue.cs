using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Synchronizer;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2
{
    public class Queue
    {
        private readonly ISignProcessor _signProcessor;

        public required Guid Id { get; set; }
        public required Dictionary<string, object> Configuration { get; set; }

        public Queue(ISignProcessor signProcessor, ILoggerFactory loggerFactory)
        {
            _signProcessor = new LocalQueueSynchronizationDecorator(signProcessor, loggerFactory.CreateLogger<LocalQueueSynchronizationDecorator>());
        }

        private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

        public Func<string, Task<string>> RegisterForSign()
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                CashBoxId = GetQueueCashbox(Id, Configuration),
                QueueId = Id,
                IsSandbox = Configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
                ServiceFolder = Configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
                Configuration = Configuration
            };

            return async (message) =>
            {
                var request = System.Text.Json.JsonSerializer.Deserialize<ReceiptRequest>(message) ?? throw new ArgumentException($"Invalid message format. The body for the message {message} could not be serialized.");
                var response = await _signProcessor.ProcessAsync(request);
                return System.Text.Json.JsonSerializer.Serialize(response);
            };
        }

        private static Guid GetQueueCashbox(Guid queueId, Dictionary<string, object> configuration)
        {
            var key = "init_ftQueue";
            if (configuration.ContainsKey(key))
            {
                var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key]!.ToString()!);
                return queues.Where(q => q.ftQueueId == queueId).First().ftCashBoxId;
            }
            else
            {
                throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
            }
        }
    }
}
