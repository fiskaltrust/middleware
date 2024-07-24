using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Models
{
    public class MigrationState
    {
        [JsonProperty]
        public long ActionJournalCount { get; set; }

        [JsonProperty]
        public long JournalDECount { get; set; }

        [JsonProperty]
        public long QueueItemCount { get; set; }

        [JsonProperty]
        public long ReceiptJournalCount { get; set; }

        [JsonProperty]
        public long QueueRow { get; set; }
    }
}