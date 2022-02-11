using System;

namespace fiskaltrust.Middleware.Storage.EF
{
    // This is a terrible, terrible workaround and should be replaced by a MigrationsAssembly implementation in EF Core
    // https://stackoverflow.com/a/51730150/9548836
    internal static class MigrationQueueIdProvider
    {
        public static Guid QueueId { get; set; }
    }
}
