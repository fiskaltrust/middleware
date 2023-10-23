using System.IO;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class FileSystemHelper
    {
        public static void CopyDirectoryRecursively(string sourceDirName, string destDirName)
        {
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            var dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDirName);
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }
            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDirName, subdir.Name);
                CopyDirectoryRecursively(subdir.FullName, tempPath);
            }
        }
    }
}
