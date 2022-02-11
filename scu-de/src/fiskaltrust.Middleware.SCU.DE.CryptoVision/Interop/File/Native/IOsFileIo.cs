using System;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native
{
    /* 
     * be aware, native access needs non-buffered-io to tse.
     * also alligned read/write along the hardware-sectors of 512byte is required by tse.
     * therefore different operating-system require different implementation of this file-io.
    */

    public interface IOsFileIo : IDisposable
    {
        bool IsOpen { get; }
        void OpenFile(string fileName);
        void CloseFile();
        void Write(byte[] alignedData, int numberOfBlocks, int blockSize);
        byte[] Read(int numberOfBlocks, int blockSize);
        void SeekBegin();
    }
}
