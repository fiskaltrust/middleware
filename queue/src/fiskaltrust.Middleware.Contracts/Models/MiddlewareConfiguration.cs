using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Contracts.Models
{
    public class MiddlewareConfiguration
    {
        public Guid QueueId { get; set; }
        public Guid CashBoxId { get; set; }
        public int ReceiptRequestMode { get; set; }
        public int TarFileChunkSize { get; set; } = 1024 * 1024; // 1 MB
        public int JournalChunkSize { get; set; } = 1024 * 1024; // 1 MB
        public bool AllowUnsafeScuSwitch { get; set; }
        public bool IsSandbox { get; set; }
        public string ServiceFolder { get; set; }
// Looks like this can cause troubles on android
//#if !ANDROID
//        public Action<string> OnMessage { get; set; }
//#endif
        public string ProcessingVersion { get; set; }
        public string AssemblyName { get; set; }
        public Version AssemblyVersion { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public Dictionary<string, bool> PreviewFeatures { get; set; }
        public string LauncherEnvironment { get; set; }
    }

    public static class LauncherEnvironments
    {
        public const string Local = "local";
        public const string Debug = "debug";
        public const string Cloud = "cloud";
    }
}