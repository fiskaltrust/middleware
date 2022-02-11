using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class TestSequenceItem
    {
        public TestSequenceItem() { }

        public TestSequenceItem(int number, TestSequenceItemDirection direction, string dataBase64)
        {
            Number = number;
            Direction = direction;
            DataBase64 = dataBase64;
        }

        public int Number { get; set; }
        public string DataBase64 { get; set; }
        public TestSequenceItemDirection Direction { get; set; }
    }

    public enum TestSequenceItemDirection
    {
        Write,
        Read
    }
}
