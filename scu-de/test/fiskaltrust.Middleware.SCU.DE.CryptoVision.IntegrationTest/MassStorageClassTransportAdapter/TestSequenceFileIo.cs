using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class TestSequenceFileIo : IOsFileIo
    {
        private int sequenceNumber = -1;

        private readonly IEnumerable<TestSequenceItem> sequenceData;

        public bool IsOpen => true;

        public TestSequenceFileIo(IEnumerable<TestSequenceItem> sequenceData) => this.sequenceData = sequenceData;

        public void CloseFile() { }

        public void OpenFile(string fileName) { }

        public byte[] Read(int numberOfBlocks, int blockSize)
        {
            var item = sequenceData.First(i => i.Number == sequenceNumber);
            item.Direction.Should().Be(TestSequenceItemDirection.Read);
            return Convert.FromBase64String(item.DataBase64);
        }

        public void SeekBegin() => sequenceNumber++;

        public void Write(byte[] alignedData, int numberOfBlocks, int blockSize)
        {
            var item = sequenceData.First(i => i.Number == sequenceNumber);
            item.Direction.Should().Be(TestSequenceItemDirection.Write);
            Convert.ToBase64String(alignedData.Take(numberOfBlocks * blockSize).ToArray())
                .Should().Be(item.DataBase64);
        }

        public void Dispose() { }
    }
}
