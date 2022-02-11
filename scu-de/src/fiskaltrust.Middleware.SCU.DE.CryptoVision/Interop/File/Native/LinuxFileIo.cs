using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native
{

    public class LinuxFileIo : IOsFileIo
    {
        // TODO check if alligned memory is required
        // https://stackoverflow.com/questions/10512987/o-direct-flag-not-working
        // stdlib.h => void *aligned_alloc(size_t alignment, size_t size); 
        // or use intptr adjustment to get start-address related to block-size

        private int fileDescriptor;
        private readonly int size = 8 * 1024;

        public void CloseFile()
        {
            if (fileDescriptor > 0)
            {
                if (Syscall.close(fileDescriptor) != 0)
                {
                    throw new IOException();
                }
                fileDescriptor = 0;
            }
        }


        public void OpenFile(string fileName)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new FileNotFoundException($"Could not find '{fileName}'.");
            }

            //using Mono.Unix.Native; fileDescriptor = Syscall.open(fileName, OpenFlags.O_RDWR | OpenFlags.O_DIRECT | OpenFlags.O_SYNC);
            fileDescriptor = Syscall.open(fileName,  /* OpenFlags.O_RDWR*/ 0x2 | /* OpenFlags.O_DIRECT */ 0x4000 |  /* OpenFlags.O_SYNC */ 0x101000);
            if (fileDescriptor < 0)
            {
                throw new FileNotFoundException($"Could not open file '{fileName}'. The returned filedescriptor is invalid.");
            }
            var writeBytes = Enumerable.Repeat((byte) 0x0, size).ToArray();
            System.IO.File.WriteAllBytes(fileName, writeBytes);
            SeekBegin();
        }

        public bool IsOpen => fileDescriptor >= 0;

        public byte[] Read(int numberOfBlocks, int blockSize)
        {
            var bufferLength = numberOfBlocks * blockSize;
            var bufferPtr = Marshal.AllocHGlobal(bufferLength + blockSize);  // add one blocksize to be able to align pointer to blocksize
            try
            {
                var offset = blockSize - Convert.ToInt32(bufferPtr.ToInt64() % blockSize);
                var alignedBufferPtr = IntPtr.Add(bufferPtr, offset);  // set alignedBufferPtr to blocksize aligned address
                if (Syscall.read(fileDescriptor, alignedBufferPtr, (ulong) bufferLength) != bufferLength)
                {
                    // tse need to read always full buffer size
                    throw new IOException();
                }
                var buffer = new byte[bufferLength];
                Marshal.Copy(alignedBufferPtr, buffer, 0, bufferLength);
                return buffer;
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public void SeekBegin()
        {
            //using Mono.Unix.Native; if (Syscall.lseek(fileDescriptor,0, SeekFlags.SEEK_SET)!=0)
            if (Syscall.lseek(fileDescriptor, 0, /* SeekFlags.SEEK_SET */ 0) != 0)
            {
                throw new IOException();
            }
        }
        public void Write(byte[] alignedData, int numberOfBlocks, int blockSize)
        {
            var bufferLength = numberOfBlocks * blockSize;
            var bufferPtr = Marshal.AllocHGlobal(bufferLength + blockSize);  // add one blocksize to be able to align pointer to blocksize
            try
            {
                var offset = blockSize - Convert.ToInt32(bufferPtr.ToInt64() % blockSize);
                var alignedBufferPtr = IntPtr.Add(bufferPtr, offset);  // set alignedBufferPtr to blocksize aligned address
                Marshal.Copy(alignedData, 0, alignedBufferPtr, bufferLength);
                if (Syscall.write(fileDescriptor, alignedBufferPtr, (ulong) bufferLength) != bufferLength)
                {
                    throw new IOException();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
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
