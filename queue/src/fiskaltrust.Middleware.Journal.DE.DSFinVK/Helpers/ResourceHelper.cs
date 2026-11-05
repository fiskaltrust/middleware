using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Helpers
{
    public static class ResourceHelper
    {

        public static async Task WriteResourceToFileAsync(string resourceFolder, string resourceFileName, string targetPath, string eol)
        {
            using var stream = GetResourceStream(resourceFolder, resourceFileName);
            using (var sr = new StreamReader(stream))
            using (var sw = new StreamWriter(targetPath) { NewLine = eol })
            {
                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    sw.WriteLine(line);
                }
            }
        }

        private static Stream GetResourceStream(string folder, string fileName)
        {
            var assembly = typeof(ResourceHelper).GetTypeInfo().Assembly;
            return assembly.GetManifestResourceStream($"fiskaltrust.Middleware.Journal.DE.DSFinVK.{folder}.{fileName}");
        }
    }
}
