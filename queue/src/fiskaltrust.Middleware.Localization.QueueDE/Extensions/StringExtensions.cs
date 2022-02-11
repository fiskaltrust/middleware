using System.IO;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveInvalidFilenameChars(this string filename) => string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }
}
