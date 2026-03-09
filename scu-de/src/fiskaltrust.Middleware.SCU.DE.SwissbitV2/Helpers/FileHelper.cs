using System.IO;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitV2.Helpers
{
    public static class FileHelper
    {
        public static bool IsFileWritable(string filePath)
        {
            try
            {
                using (var fs = File.Create(filePath, 1, FileOptions.DeleteOnClose))
                { }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
