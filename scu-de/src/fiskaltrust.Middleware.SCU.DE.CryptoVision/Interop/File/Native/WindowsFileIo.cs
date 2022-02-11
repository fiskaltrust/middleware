using System;
using System.IO;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native
{
    public class WindowsFileIo : IOsFileIo
    {
        private FileStream fileStream;
        private readonly int size = 8 * 1024;

        public void CloseFile()
        {
            fileStream?.Close();
            fileStream?.Dispose();
            fileStream = null;
        }

        public void OpenFile(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            //  (FileOptions) 0x20000000 is kernel32 specific for FILE_FLAG_NO_BUFFERING
            fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 512, FileOptions.WriteThrough | (FileOptions) 0x20000000);

            if (!fileStream.CanRead || !fileStream.CanWrite || !fileStream.CanSeek)
            {
                // fileStream doesn't support required operations, maybe failed to open file in no-buffering-mode
                throw new FileNotFoundException();
            }
            var writeBytes = Enumerable.Repeat((byte) 0x0, size).ToArray();
            fileStream.Write(writeBytes, 0, writeBytes.Length);
            fileStream.Position = 0;
        }

        public bool IsOpen => fileStream != null && fileStream.CanRead && fileStream.CanWrite && fileStream.CanSeek;

        public byte[] Read(int numberOfBlocks, int blockSize)
        {
            var buffer = new byte[numberOfBlocks * blockSize];
            if (fileStream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new IOException();
            }
            return buffer;
        }

        public void SeekBegin()
        {
            if (fileStream.Seek(0, SeekOrigin.Begin) != 0)
            {
                throw new IOException();
            }
        }

        public void Write(byte[] alignedData, int numberOfBlocks, int blockSize)
        {
            fileStream.Write(alignedData, 0, numberOfBlocks * blockSize);
        }

        #region IDisposeable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)

                    try
                    {
                        CloseFile();
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LinuxFileIo()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
