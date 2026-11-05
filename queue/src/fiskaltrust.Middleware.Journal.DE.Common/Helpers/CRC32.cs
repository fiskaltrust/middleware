using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    // No idea what this is doing, but it seems to be working.
    // Got it from https://stackoverflow.com/a/51922169/9548836
    public class CRC32
    {
        private const uint SEED_GENERATOR = 0xEDB88320;
        private readonly uint[] _checksumTable;

        public CRC32()
        {
            _checksumTable = Enumerable.Range(0, 256).Select(i =>
            {
                var tableEntry = (uint) i;
                for (var j = 0; j < 8; ++j)
                {
                    tableEntry = ((tableEntry & 1) != 0)
                        ? (SEED_GENERATOR ^ (tableEntry >> 1))
                        : (tableEntry >> 1);
                }
                return tableEntry;
            }).ToArray();
        }

        public uint? ComputeHash(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            var arrayOfBytes = Encoding.UTF8.GetBytes(data);

            var crc32 = new CRC32();
            return crc32.Get(arrayOfBytes);
        }

        private uint? Get<T>(IEnumerable<T> byteStream)
        {
            try
            {
                return ~byteStream.Aggregate(0xFFFFFFFF, (checksumRegister, currentByte) =>
                          _checksumTable[(checksumRegister & 0xFF) ^ Convert.ToByte(currentByte)] ^ (checksumRegister >> 8));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
