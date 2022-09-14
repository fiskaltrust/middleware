using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace fiskaltrust.Middleware.Localization.QueueDE.Helpers
{
    public class PathHelpers
    {
        public const int MAX_PATH = 255;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName([MarshalAs(UnmanagedType.LPTStr)] string path,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder longPath, int longPathLength);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string path,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder shortPath, int shortPathLength);

        public static string GetShortPath(string source)
        {
            if (source.Length > MAX_PATH)
            {
                return source;
            }
            else
            {
                var shortPath = new StringBuilder(255);
                GetShortPathName(source, shortPath, shortPath.Capacity);
                return shortPath.ToString();
            }
        }
    }
}
