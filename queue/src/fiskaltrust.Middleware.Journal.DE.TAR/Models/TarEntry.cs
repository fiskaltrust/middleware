using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Journal.DE.TAR.Models
{
    public class TarEntry
    {
        public TarEntry(string fileName, long size, byte[] fileContent)
        {
            FileName = fileName;
            Size = size;
            FileContent = fileContent.ToList();
        }
        public string FileName { get; }
        public long Size { get; }
        public List<byte> FileContent { get; }
    }
}
