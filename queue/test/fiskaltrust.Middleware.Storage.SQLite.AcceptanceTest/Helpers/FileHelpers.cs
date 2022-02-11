using System;
using System.IO;
using System.Threading;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers
{
    public static class FileHelpers
    {
        public static void WaitUntilFileIsAccessible(string path, int timeoutMs = 2000)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);
            while (DateTime.Now < startTime + timeout)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100))
                    {
                        break;
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
