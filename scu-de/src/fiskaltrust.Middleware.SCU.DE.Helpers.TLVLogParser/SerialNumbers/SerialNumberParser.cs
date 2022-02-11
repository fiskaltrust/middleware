using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.SerialNumbers.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.SerialNumbers
{
    public static class SerialNumberParser
    {
        public static List<SerialNumberRecord> GetSerialNumbers(byte[] bytes)
        {
            var tlvElements = TlvParser.ParseTlv(bytes);
            var serialNumbers = new List<SerialNumberRecord>();
            foreach (var serialNumber in tlvElements.First().Children)
            {
                var serialNumberRecord = new SerialNumberRecord
                {
                    //TODO check if this can be returned by byte[], Base64-string, Octet-string
                    //SerialNumber = BitConverter.ToString(serialNumber.Children.Single(x => x.Tag == 0x4).Value)
                    SerialNumber = serialNumber.Children.Single(x => x.Tag == 0x4).Value
                };

                foreach (var entry in serialNumber.Children.Single(x => x.Tag == 0x30).Children)
                {
                    if (BitConverter.ToBoolean(entry.Value, 0))
                    {
                        switch (entry.Tag)
                        {
                            case 0x80:
                                serialNumberRecord.IsUsedForTransactionLogs = true;
                                break;

                            case 0x81:
                                serialNumberRecord.IsUsedForSystemLogs = true;
                                break;

                            case 0x82:
                                serialNumberRecord.IsUsedForAuditLogs = true;
                                break;
                        }
                    }
                }
                serialNumbers.Add(serialNumberRecord);
            }
            return serialNumbers;
        }
    }
}